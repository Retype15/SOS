// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using System.Diagnostics.CodeAnalysis;
using Barotrauma;
using Barotrauma.LuaCs;
using Barotrauma.LuaCs.Data;
using Microsoft.Xna.Framework;
using SOS.GUI;

namespace SOS.Configs
{
    public abstract class ConfigDirtySaver
    {

        protected readonly HashSet<ISettingBase> _dirtySettings = [];

        protected void MarkDirty(ISettingBase setting)
            => _dirtySettings.Add(setting);

        protected bool HasChanges => _dirtySettings.Count > 0;

        protected void SaveChanges(IConfigService configService)
        {
            if (!HasChanges) return;

            foreach (ISettingBase setting in _dirtySettings)
                configService.SaveConfigValue(setting);

            _dirtySettings.Clear();
        }

        protected bool TryInitConfig<T>(string name, out T setting, IConfigService configService, ContentPackage package) where T : ISettingBase
            => ConfigHelper.TryInitConfig<T>(name, out setting, configService, package, MarkDirty);
    }

    public static class ConfigHelper
    {
        // 

        public static void ButtonToResetSection(this GUILayoutBuilder l, ISOSConfig cfg, string? text = null, Action? onClick = null, string? tooltip = null, Color? color = null, string? style = null)
        {
            Action onClickFinal;
            if (onClick != null) onClickFinal = onClick;
            else onClickFinal = () =>
            {
                cfg.Reset();
                cfg.Save();
                Profiles.ProfileHelper.RefreshSettings(); //TODO: Revisar para hacer esta llamada más limpia.
            };
            l.Button(
                text: text ?? Texts.Get("sos.config.reset_section", "Reset Section Defaults").Value,
                onClick: onClickFinal,
                tooltip: tooltip,
                color: color ?? (Color.IndianRed * 0.8f),
                style: style ?? "GUIButtonSmall"
            );
        }

        // ─── CSV Serialization Helpers ───

        public static HashSet<string> CsvToHashSet(string? csv)
        {
            if (string.IsNullOrEmpty(csv)) return [];
            return [.. csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
        }

        public static List<string> CsvToList(string? csv)
        {
            if (string.IsNullOrEmpty(csv)) return [];
            return [.. csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
        }

        // TryInitConfig

        public static bool TryInitConfig<T>(string name, [NotNullWhen(true)] out T setting, IConfigService configService, ContentPackage package, Action<ISettingBase>? onValueChanged = null)
                where T : ISettingBase
        {
            if (!configService.TryGetConfig(package, name, out setting))
            {
                Logger.LogError($"Failed to find config named {name}!");
                return false;
            }
            if (onValueChanged != null) setting.OnValueChanged += onValueChanged;
#if DEBUG
            setting.OnValueChanged += setting => Logger.LogDebug($"Changed: {setting.InternalName} To: {setting.GetStringValue(),128}", level: LogLevel.Trace);
#endif
            return true;
        }
    }
}