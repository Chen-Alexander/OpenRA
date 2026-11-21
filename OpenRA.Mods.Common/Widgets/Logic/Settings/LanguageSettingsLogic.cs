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
using OpenRA.Mods.Common.Widgets;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class LanguageSettingsLogic : ChromeLogic
	{
		[FluentReference]
		const string RestartTitle = "dialog-language-restart.title";

		[FluentReference]
		const string RestartPrompt = "dialog-language-restart.prompt";

		[FluentReference]
		const string RestartAccept = "button-restart-now";

		[FluentReference]
		const string RestartCancel = "button-got-it";

		[FluentReference]
		const string RestartFailedTitle = "dialog-language-restart-failed.title";

		[FluentReference]
		const string RestartFailedPrompt = "dialog-language-restart-failed.prompt";

		[FluentReference]
		const string Ok = "button-ok";

		// Language names are in native script so users can find their language
		// regardless of what the UI currently displays.
		static readonly Dictionary<string, string> LanguageNativeNames = new()
		{
			{ "en", "English" },
			{ "zh-Hans", "简体中文" },
		};

		readonly ModData modData;
		readonly GameSettings gameSettings;
		static string originalLanguage;

		[ObjectCreator.UseCtor]
		public LanguageSettingsLogic(ModData modData, SettingsLogic settingsLogic, string panelID, string label)
		{
			this.modData = modData;
			gameSettings = modData.GetSettings<GameSettings>();
			EnsureOriginalLanguage();

			settingsLogic.RegisterSettingsPanel(panelID, label, InitPanel, ResetPanel);
		}

		static void EnsureOriginalLanguage()
		{
			originalLanguage ??= Game.Settings.Game.Language;
		}

		public static string GetNativeName(string language)
		{
			return LanguageNativeNames.TryGetValue(language, out var name) ? name : language;
		}

		public static bool IsRestartRequired(GameSettings settings)
		{
			EnsureOriginalLanguage();
			return settings.Language != originalLanguage;
		}

		public static void ShowDropdown(DropDownButtonWidget dropdown, GameSettings settings, ModData modData)
		{
			EnsureOriginalLanguage();

			ScrollItemWidget SetupItem(string langCode, ScrollItemWidget itemTemplate)
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => settings.Language == langCode,
					() => SelectLanguage(langCode, settings, modData));

				var displayName = GetNativeName(langCode);
				item.Get<LabelWidget>("LABEL").GetText = () => displayName;
				return item;
			}

			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, Game.ModData.Languages, SetupItem);
		}

		static void SelectLanguage(string langCode, GameSettings settings, ModData modData)
		{
			if (settings.Language == langCode)
				return;

			settings.Language = langCode;

			// Choosing the language the game was started with does not need a restart.
			if (!IsRestartRequired(settings))
				return;

			ConfirmationDialogs.ButtonPrompt(modData,
				title: RestartTitle,
				text: RestartPrompt,
				confirmText: RestartAccept,
				onConfirm: () => RestartNow(modData),
				cancelText: RestartCancel,
				onCancel: () => { });
		}

		static void RestartNow(ModData modData)
		{
			Game.Settings.Save();

			if (Game.TryRestartApplication())
				return;

			ConfirmationDialogs.ButtonPrompt(modData,
				title: RestartFailedTitle,
				text: RestartFailedPrompt,
				confirmText: Ok,
				onConfirm: () => { });
		}

		Func<bool> InitPanel(Widget panel)
		{
			var scrollPanel = panel.Get<ScrollPanelWidget>("SETTINGS_SCROLLPANEL");

			var languageDropdown = panel.Get<DropDownButtonWidget>("LANGUAGE_DROPDOWN");
			languageDropdown.OnMouseDown = _ => ShowDropdown(languageDropdown, gameSettings, modData);
			languageDropdown.GetText = () => GetNativeName(gameSettings.Language);

			var restartDesc = panel.Get("LANGUAGE_RESTART_REQUIRED_DESC");
			restartDesc.IsVisible = () => IsRestartRequired(gameSettings);

			SettingsUtils.AdjustSettingsScrollPanelLayout(scrollPanel);

			return () => IsRestartRequired(gameSettings);
		}

		Action ResetPanel(Widget panel)
		{
			return () => gameSettings.Language = new GameSettings().Language;
		}
	}
}
