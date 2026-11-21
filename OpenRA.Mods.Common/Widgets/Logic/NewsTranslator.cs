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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	/// <summary>
	/// Translates the remote news feed at runtime using the public Google Translate
	/// endpoint, caching results so each string is only translated once.
	/// Failures fall back to the original English text.
	/// </summary>
	static class NewsTranslator
	{
		const string Endpoint = "https://translate.googleapis.com/translate_a/single?client=gtx&sl=en&tl=zh-CN&dt=t&q=";

		static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(20) };
		static readonly Dictionary<string, string> Cache = LoadCache();
		static readonly object Sync = new();
		static bool dirty;

		public static bool ShouldTranslate => Game.Settings.Game.Language.StartsWith("zh", StringComparison.OrdinalIgnoreCase);

		public static async Task<string> Translate(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
				return text;

			lock (Sync)
				if (Cache.TryGetValue(text, out var cached))
					return cached;

			var lines = text.Replace("\\n", "\n").Split('\n');
			var translated = new List<string>(lines.Length);
			foreach (var line in lines)
			{
				if (string.IsNullOrWhiteSpace(line))
				{
					translated.Add("");
					continue;
				}

				translated.Add(await TranslateLine(line));
			}

			var result = string.Join("\n", translated);
			lock (Sync)
			{
				Cache[text] = result;
				dirty = true;
			}

			return result;
		}

		static async Task<string> TranslateLine(string text)
		{
			try
			{
				var json = await Client.GetStringAsync(Endpoint + Uri.EscapeDataString(text));
				using var doc = JsonDocument.Parse(json);
				var sb = new StringBuilder();
				var first = doc.RootElement.EnumerateArray().First();
				foreach (var segment in first.EnumerateArray())
					sb.Append(segment.EnumerateArray().First().GetString());

				return sb.ToString();
			}
			catch (Exception e)
			{
				Log.Write("debug", $"News translation failed: {e.Message}");
				return text;
			}
		}

		public static void SaveCache()
		{
			lock (Sync)
			{
				if (!dirty)
					return;

				try
				{
					File.WriteAllText(Path.Combine(Platform.SupportDir, "news-zh.cache.json"),
						JsonSerializer.Serialize(Cache));
					dirty = false;
				}
				catch (Exception e)
				{
					Log.Write("debug", $"Failed to save news translation cache: {e.Message}");
				}
			}
		}

		static Dictionary<string, string> LoadCache()
		{
			try
			{
				var path = Path.Combine(Platform.SupportDir, "news-zh.cache.json");
				if (File.Exists(path))
					return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path)) ?? [];
			}
			catch (Exception e)
			{
				Log.Write("debug", $"Failed to load news translation cache: {e.Message}");
			}

			return [];
		}
	}
}
