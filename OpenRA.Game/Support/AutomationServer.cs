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
	/// </summary>
	public static class AutomationServer
	{
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
					{
						var command = line.Trim();
						if (command.Length == 0)
							continue;

						if (command == "ping")
							writer.WriteLine("pong");
						else if (command == "status")
							writer.WriteLine($"listening 127.0.0.1:{Port}");
						else
							writer.WriteLine($"error unknown command: {command}");
					}
				}
				catch (IOException)
				{
				}
			}
		}
	}
}
