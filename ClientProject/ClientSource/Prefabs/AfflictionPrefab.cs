// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using Barotrauma;
using Barotrauma.Extensions;
using Microsoft.Xna.Framework;
using SOS.Extensions;

namespace SOS.Prefabs.Affliction
{
    [AutoRegister("SOS.Prefab.AfflictionPrefab", 2)]
    public sealed class AfflictionPrefabProvider : ISOSPrefab
    {
        public IEnumerable<Prefab> GetPrefabs(IPrefabFilter filter)
        {
            if (!filter.AllowsType(nameof(AfflictionPrefab))) return [];

            return AfflictionPrefab.List
                .Where(a => Matches(a, filter))
                .OrderBy(a => a is AfflictionPrefabHusk ? 1 : 0).ThenBy(p => p.GetNameOrFallback());
        }

        private static bool Matches(AfflictionPrefab a, IPrefabFilter filter)
        {
            if (filter.Slot.Count > 0 || filter.Tag.Count > 0) return false;

            if (filter.Mod.Count > 0 && !filter.Mod.Any(m =>
                (a.ContentPackage?.Name ?? "Vanilla").Contains(m, StringComparison.OrdinalIgnoreCase)))
                return false;

            if (filter.Category.Count > 0 && !filter.Category.Any(c =>
                a.AfflictionType.Contains(c)))
                return false;

            if (filter.ID.Count > 0 && !filter.ID.Any(id =>
                a.Identifier.Value.Contains(id, StringComparison.OrdinalIgnoreCase)))
                return false;

            return MatchesGeneral(a, filter);
        }

        private static bool MatchesGeneral(AfflictionPrefab a, IPrefabFilter filter)
        {
            if (filter.General.Count == 0) return true;

            string name = a.Name.Value;
            string id = a.Identifier.Value;
            string modName = a.ContentPackage?.Name ?? "Vanilla";

            foreach (var term in filter.General)
            {
                bool found = name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                             id.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                             modName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                             a.AfflictionType.Contains(term);
                if (!found) return false;
            }

            return true;
        }
    }

    [AutoRegister("SOS.Prefab.AfflictionPrefab.Info", 2)]
    public sealed class AfflictionPrefabInfo : ISOSPrefabInfo
    {
        public bool CanHandle(Prefab prefab)
            => prefab is AfflictionPrefab;

        public LocalizedString? GetName(Prefab prefab)
            => prefab is AfflictionPrefab aff ? aff.Name : null;

        public Sprite? GetIcon(Prefab prefab)
            => prefab is AfflictionPrefab aff ? aff.Icon : null;

        public Color? GetIconColor(Prefab prefab)
            => prefab is AfflictionPrefab aff ? aff.IconColors?.FirstOrNull() : null;

        public ContentXElement? GetConfigElement(Prefab prefab)
            => prefab is AfflictionPrefab aff ? aff.configElement : null;

        public LocalizedString? GetDescription(Prefab prefab)
        {
            if (prefab is not AfflictionPrefab affliction) return null;
            return string.Join("\n\n", affliction.Descriptions?.Select(static d => $"({d.MinStrength.ToString().SetColor(Color.Orange)}-{d.MaxStrength.ToString().SetColor(Color.OrangeRed)}) {d.Target.ToString().SetColor(Color.BlueViolet)}: {d.Text}") ?? []);
        }

        public LocalizedString? GetTooltip(Prefab prefab)
        {
            if (prefab is not AfflictionPrefab affliction) return null;

            LocalizedString tooltip = affliction.Name.SetColor(GUIStyle.TextColorBright);
            var description = affliction.GetDescription(0f, AfflictionPrefab.Description.TargetType.Any);
            if (!description.IsNullOrEmpty())
                tooltip += (LocalizedString)$"\n{description}";
            return tooltip;
        }
    }
}