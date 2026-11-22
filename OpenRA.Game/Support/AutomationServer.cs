#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace OpenRA
{
	/// <summary>
	/// Local-only TCP endpoint used by automation clients. It binds to 127.0.0.1 and is
	/// disabled unless Debug.AutomationEnabled is set in the game settings.
	/// Commands are queued and injected directly into the game input handler, bypassing
	/// the SDL/Windows focus requirements that apply to synthetic OS input.
	/// </summary>
	public static class AutomationServer
	{
		static readonly ConcurrentQueue<Action<IInputHandler>> PendingInput = new();
		static TcpListener listener;
		static Thread acceptThread;
		static volatile bool running;

		public static bool IsRunning => running;
		public static int Port { get; private set; }

		public static void Start(bool enabled, int port)
		{
			if (!enabled || running)
				return;

			try
			{
				listener = new TcpListener(IPAddress.Loopback, port);
				listener.Start();
				Port = port;
				running = true;

				acceptThread = new Thread(AcceptLoop) { IsBackground = true, Name = "OpenRA automation server" };
				acceptThread.Start();
				Log.Write("debug", $"Automation server listening on 127.0.0.1:{port}");
			}
			catch (Exception e)
			{
				running = false;
				Log.Write("debug", $"Failed to start automation server on 127.0.0.1:{port}: {e.Message}");
			}
		}

		/// <summary>Drains injected commands on the game thread.</summary>
		public static void ProcessInput(IInputHandler inputHandler)
		{
			while (PendingInput.TryDequeue(out var action))
				action(inputHandler);
		}

		static void AcceptLoop()
		{
			while (running)
			{
				try
				{
					var client = listener.AcceptTcpClient();
					ThreadPool.QueueUserWorkItem(_ => HandleClient(client));
				}
				catch (SocketException)
				{
					if (!running)
						return;
				}
				catch (ObjectDisposedException)
				{
					return;
				}
			}
		}

		static void HandleClient(TcpClient client)
		{
			using (client)
			using (var stream = client.GetStream())
			using (var reader = new StreamReader(stream, Encoding.UTF8))
			using (var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true })
			{
				try
				{
					writer.WriteLine("OpenRA automation ready");
					string line;
					while ((line = reader.ReadLine()) != null)
						writer.WriteLine(HandleCommand(line.Trim()));
				}
				catch (IOException)
				{
				}
			}
		}

		static string HandleCommand(string command)
		{
			if (command.Length == 0)
				return "error empty command";

			var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			switch (parts[0].ToLowerInvariant())
			{
				case "ping":
					return "pong";

				case "status":
					return $"listening 127.0.0.1:{Port}";

				case "click":
				case "move":
				{
					if (parts.Length != 3 || !int.TryParse(parts[1], out var x) || !int.TryParse(parts[2], out var y))
						return "error usage: click <x> <y>";

					var location = new int2(x, y);
					if (parts[0].Equals("click", StringComparison.OrdinalIgnoreCase))
					{
						PendingInput.Enqueue(handler =>
						{
							handler.OnMouseInput(new MouseInput(MouseInputEvent.Move, MouseButton.None, location, int2.Zero, Modifiers.None, 0));
							handler.OnMouseInput(new MouseInput(MouseInputEvent.Down, MouseButton.Left, location, int2.Zero, Modifiers.None, 0));
							handler.OnMouseInput(new MouseInput(MouseInputEvent.Up, MouseButton.Left, location, int2.Zero, Modifiers.None, 0));
						});
					}
					else
						PendingInput.Enqueue(handler => handler.OnMouseInput(new MouseInput(MouseInputEvent.Move, MouseButton.None, location, int2.Zero, Modifiers.None, 0)));

					return "ok";
				}

				case "key":
				{
					if (parts.Length != 2 || !Enum.TryParse<Keycode>(parts[1], true, out var key))
						return "error usage: key <keycode>";

					PendingInput.Enqueue(handler =>
					{
						handler.OnKeyInput(new KeyInput { Event = KeyInputEvent.Down, Key = key });
						handler.OnKeyInput(new KeyInput { Event = KeyInputEvent.Up, Key = key });
					});
					return "ok";
				}

				default:
					return $"error unknown command: {parts[0]}";
			}
		}
	}
}
