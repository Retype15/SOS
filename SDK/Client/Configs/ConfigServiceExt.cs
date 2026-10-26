// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using System.Xml;
using System.Xml.Linq;
using Barotrauma.LuaCs;
using Barotrauma.LuaCs.Data;
using Result = FluentResults.Result;

namespace SOS.Configs
{
    /// <summary>
    /// Writes a batch of dirty settings to disk using a single read/modify/write cycle per content package.
    /// </summary>
    /// <remarks>
    /// LuaCs' <c>SaveConfigValue</c> reloads, parses and rewrites the whole settings file for
    /// every individual setting, turning a window close into dozens of synchronous disk operations on the update thread.
    /// This saver groups the dirty settings by their owning package and performs one load plus one save per package,
    /// producing the exact same XML layout as the vanilla per-setting path.
    /// </remarks>
    public static class ConfigServiceExt
    {
        private static string SaveDataFileName => ConfigService.SaveDataFileName;
        /// <summary>Persists all dirty settings, batching file I/O per content package.</summary>
        /// <param name="configService">The configuration service that owns the settings.</param>
        /// <param name="settings">The settings to persist.</param>
        public static bool TrySaveBatch(this IConfigService configService, IReadOnlyCollection<ISettingBase> settings)
            => configService.SaveBatch(settings).IsSuccess;

        /// <summary>
        /// Persists a batch of settings, using the optimized per-package implementation when available.
        /// </summary>
        /// <param name="configService">The configuration service that owns the settings.</param>
        /// <param name="settings">The settings to persist.</param>
        /// <returns>A result describing success or the failures encountered while saving.</returns>
        public static Result SaveBatch(this IConfigService configService, IReadOnlyCollection<ISettingBase> settings)
        {
            if (configService is ConfigService service)
                return service.SaveBatch(settings);
            return configService.SaveBatchByOne(settings);
        }

        /// <summary>
        /// Persists a batch of settings grouped by content package, performing one read/modify/write cycle per package.
        /// </summary>
        /// <param name="configService">The concrete configuration service that owns the settings.</param>
        /// <param name="settings">The settings to persist.</param>
        /// <returns>A result describing success or the failures encountered while saving.</returns>
        public static Result SaveBatch(this ConfigService configService, IReadOnlyCollection<ISettingBase> settings)
        {
            if (settings.Count == 0) return Result.Ok();

            Result? resultLog = new();
            var groups = settings.GroupBy((setting) => setting.OwnerPackage);
            foreach (var settingsBatch in groups)
            {
                XDocument cpCfgValues;
                var package = settingsBatch.Key;

                if (configService._storageService.LoadLocalXml(package, SaveDataFileName) is not { } saveFileResult)
                    return Result.Fail($"{nameof(SaveBatch)}: Storage Service Failure while trying to load file for package [{package.Name}].");

                if (saveFileResult.IsSuccess)
                    cpCfgValues = saveFileResult.Value;
                else
                    cpCfgValues = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), new XElement("Configuration"));

                if (cpCfgValues.Root is null || cpCfgValues.Root.Name != "Configuration")
                    return Result.Fail($"{nameof(SaveBatch)}: Bad save file format for package: [{package.Name}]");

                var mainTarget = GetOrAddElement(cpCfgValues.Root, XmlConvert.EncodeLocalName(package.Name.Trim()), name => new XElement(name));

                foreach (var setting in settingsBatch)
                {
                    var currentTarget = GetOrAddElement(mainTarget, setting.InternalName, name => new XElement(name));

                    var ret = setting.GetSerializableValue().Match<bool>(str =>
                    {
                        var tgt = currentTarget.Attribute("Value");
                        if (tgt is null)
                        {
                            var attr = new XAttribute("Value", str);
                            currentTarget.Add(attr);
                        }
                        else
                        {
                            tgt.Value = str;
                        }

                        return true;
                    },
                    elem =>
                    {
                        currentTarget.ReplaceNodes(new XElement("Value", elem));
                        return true;
                    });

                    //resultLog.WithReasons(ret.Reasons);
                }

                resultLog.WithReasons(configService._storageService.SaveLocalXml(package, SaveDataFileName, cpCfgValues).Reasons);
            }

            return resultLog;
        }

        private static Result SaveBatchByOne(this IConfigService configService, IReadOnlyCollection<ISettingBase> settings)
        {
            if (settings.Count == 0) return Result.Ok();
            Result result = new();
            foreach (ISettingBase setting in settings)
                result.WithReasons(configService.SaveConfigValue(setting).Reasons);
            return result;
        }

        static XElement GetOrAddElement(XElement containerElement, string elementName, Func<string, XElement> factory)
        {
            var element = containerElement.Element(elementName);
            if (element is null)
            {
                element = factory(elementName);
                containerElement.Add(element);
            }
            return element;
        }
    }
}
