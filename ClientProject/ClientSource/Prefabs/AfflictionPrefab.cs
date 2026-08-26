// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using Barotrauma;

namespace SOS.Prefabs.Affliction
{
    [AutoRegister("AfflictionPrefab", 2)]
    public sealed class AfflictionPrefabProvider : ISOSPrefab
    {
        public Type PrefabType => typeof(AfflictionPrefab);
        public string Header => Texts.Get("sos.list.header.afflictionprefab", "Afflictions").Value;

        public IEnumerable<Prefab> GetAll(ISOSPrefabFilter filter)
        {
            return AfflictionPrefab.List
                .Where(a => Matches(a, filter))
                .OrderBy(a => a is AfflictionPrefabHusk ? 1 : 0).ThenBy(p => p.Name());
        }

        private static bool Matches(AfflictionPrefab a, ISOSPrefabFilter filter)
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

        private static bool MatchesGeneral(AfflictionPrefab a, ISOSPrefabFilter filter)
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

    [AutoRegister("AfflictionPrefabHusk", 2.5)]
    public sealed class AfflictionPrefabHuskProvider : ISOSPrefab
    {
        public Type PrefabType => typeof(AfflictionPrefabHusk);
        public string Header => Texts.Get("sos.list.header.afflictionprefabhusk", "Husk Afflictions").Value;
        public IEnumerable<Prefab> GetAll(ISOSPrefabFilter filter) => [];
    }
}