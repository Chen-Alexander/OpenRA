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
using System.Globalization;
using System.Linq;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class SystemInfoPromptLogic : ChromeLogic
	{
		// Increment the version number when adding new stats
		const int SystemInformationVersion = 6;

		[FluentReference]
		const string Row = "label-system-info-row";

		[FluentReference]
		const string IdLabel = "label-system-info-id";

		[FluentReference]
		const string PlatformLabel = "label-system-info-platform";

		[FluentReference]
		const string OsLabel = "label-system-info-os";

		[FluentReference]
		const string ArchitectureLabel = "label-system-info-architecture";

		[FluentReference]
		const string RuntimeLabel = "label-system-info-runtime";

		[FluentReference]
		const string OpenGLLabel = "label-system-info-opengl";

		[FluentReference]
		const string WindowSizeLabel = "label-system-info-window-size";

		[FluentReference]
		const string WindowScaleLabel = "label-system-info-window-scale";

		[FluentReference]
		const string UIScaleLabel = "label-system-info-ui-scale";

		[FluentReference]
		const string SystemLanguageLabel = "label-system-info-system-language";

		static Dictionary<string, (string LabelKey, string Value)> GetSystemInformation()
		{
			return new Dictionary<string, (string, string)>
			{
				{ "id", (IdLabel, Game.Settings.Debug.UUID) },
				{ "platform", (PlatformLabel, Platform.CurrentPlatform.ToString()) },
				{ "os", (OsLabel, Platform.OperatingSystem) },
				{ "arch", (ArchitectureLabel, Platform.CurrentArchitecture.ToString()) },
				{ "runtime", (RuntimeLabel, Platform.RuntimeVersion) },
				{ "gl", (OpenGLLabel, Game.Renderer.GLVersion) },
				{ "windowsize", (WindowSizeLabel, $"{Game.Renderer.NativeResolution.Width}x{Game.Renderer.NativeResolution.Height}") },
				{ "windowscale", (WindowScaleLabel, Game.Renderer.NativeWindowScale.ToString("F2", CultureInfo.InvariantCulture)) },
				{ "uiscale", (UIScaleLabel, Game.Settings.Graphics.UIScale.ToString("F2", CultureInfo.InvariantCulture)) },
				{ "lang", (SystemLanguageLabel, CultureInfo.InstalledUICulture.TwoLetterISOLanguageName) }
			};
		}

		public static bool ShouldShowPrompt()
		{
			return Game.Settings.Debug.SystemInformationVersionPrompt < SystemInformationVersion;
		}

		public static string CreateParameterString()
		{
			if (!Game.Settings.Debug.SendSystemInformation)
				return "";

			return $"&sysinfoversion={SystemInformationVersion}&"
				+ GetSystemInformation()
					.Select(kv => kv.Key + "=" + Uri.EscapeDataString(kv.Value.Value))
					.JoinWith("&");
		}

		[ObjectCreator.UseCtor]
		public SystemInfoPromptLogic(Widget widget, Action onComplete)
		{
			var sysInfoCheckbox = widget.Get<CheckboxWidget>("SYSINFO_CHECKBOX");
			sysInfoCheckbox.IsChecked = () => Game.Settings.Debug.SendSystemInformation;
			sysInfoCheckbox.OnClick = () => Game.Settings.Debug.SendSystemInformation ^= true;

			var sysInfoData = widget.Get<ScrollPanelWidget>("SYSINFO_DATA");
			var template = sysInfoData.Get<LabelWidget>("DATA_TEMPLATE");
			sysInfoData.RemoveChildren();

			foreach (var (labelKey, value) in GetSystemInformation().Values)
			{
				var label = template.Clone();
				var text = FluentProvider.GetMessage(Row,
					"label", FluentProvider.GetMessage(labelKey),
					"value", value);
				label.GetText = () => text;
				sysInfoData.AddChild(label);
			}

			widget.Get<ButtonWidget>("CONTINUE_BUTTON").OnClick = () =>
			{
				Game.Settings.Debug.SystemInformationVersionPrompt = SystemInformationVersion;
				Game.Settings.Save();
				Ui.CloseWindow();
				onComplete();
			};
		}
	}
}
