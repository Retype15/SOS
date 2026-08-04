// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using System.Diagnostics.CodeAnalysis;
using Barotrauma.LuaCs.Data;

namespace SOS.Configs
{
    public abstract class ConfigDirtySaver
    {

        protected readonly HashSet<ISettingBase> _dirtySettings = [];

        protected void MarkDirty(ISettingBase setting)
            => _dirtySettings.Add(setting);

        protected bool HasChanges => _dirtySettings.Count > 0;

        protected void SaveChanges()
        {
            if (!HasChanges) return;

            foreach (ISettingBase setting in _dirtySettings)
                Plugin.Instance.ConfigService.SaveConfigValue(setting);

            _dirtySettings.Clear();
        }

        protected bool TryInitConfig<T>(string name, out T setting) where T : ISettingBase
            => ConfigHelper.TryInitConfig<T>(name, out setting, MarkDirty);
    }

    public static class ConfigHelper
    {
        // ─── CSV Serialization Helpers ───

        internal static HashSet<string> CsvToHashSet(string? csv)
        {
            if (string.IsNullOrEmpty(csv)) return [];
            return [.. csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
        }

        internal static List<string> CsvToList(string? csv)
        {
            if (string.IsNullOrEmpty(csv)) return [];
            return [.. csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
        }

        // TryInitConfig

        internal static bool TryInitConfig<T>(string name, [NotNullWhen(true)] out T setting, Action<ISettingBase>? onValueChanged = null)
                where T : ISettingBase
        {
            if (!Plugin.Instance.ConfigService.TryGetConfig(Plugin.Instance.Package, name, out setting))
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