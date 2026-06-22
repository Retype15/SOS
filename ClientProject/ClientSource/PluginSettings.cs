// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using System.Diagnostics.CodeAnalysis;
using Barotrauma;
using Barotrauma.LuaCs;
using Barotrauma.LuaCs.Data;

namespace SOS
{

    public partial class Plugin
    {
        public class ClientConfig
        {
            private readonly ISettingBase<bool> _activedSOS = null!;
            public bool ActivedSOS { get => _activedSOS.Value; set => _activedSOS.TrySetValue(value); }

            private readonly ISettingBase<string> _nameSOS = null!;
            public string NameSOS { get => _nameSOS.Value; set => _nameSOS.TrySetValue(value); }

            public ClientConfig(IConfigService ConfigService, ContentPackage _package)
            {
                TryGetConfig("ActivedSOS", out _activedSOS);
                TryGetConfig("NameSOS", out _nameSOS);

                bool TryGetConfig<T>(string name, [NotNullWhen(true)] out T setting) where T : ISettingBase
                {
                    if (!ConfigService.TryGetConfig(_package, name, out setting))
                    {
                        RLogger.LogError($"Failed to find config named {name}!");
                        return false;
                    }

                    return true;
                }
            }
        }
    }
}