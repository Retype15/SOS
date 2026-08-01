// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using Barotrauma;

namespace SOS
{
    public interface ISOSPrefabAuto : ISOSPrefab, IAutoRegister;

    public sealed class ItemPrefabProvider : ISOSPrefabAuto
    {
        public string Id => "ItemPrefab";
        public double Order => 1;
        public Type PrefabType => typeof(ItemPrefab);
        public string Header => Texts.Get("sos.list.header.itemprefab", "Item Prefab").Value;

        private static readonly Dictionary<Identifier, string> _itemSlotCache = [];

        public static string GetItemSlotsCached(ItemPrefab prefab)
        {
            if (_itemSlotCache.TryGetValue(prefab.Identifier, out var cached)) return cached;

            if (prefab.ConfigElement == null) return _itemSlotCache[prefab.Identifier] = "";

            var slots = new List<string>();
            foreach (var element in prefab.ConfigElement.Descendants())
            {
                string n = element.Name.ToString().ToLowerInvariant();
                if (n == "wearable" || n == "holdable")
                {
                    string s = element.GetAttributeString("slots", "");
                    if (!string.IsNullOrEmpty(s)) slots.Add(s.Replace("+", " "));
                }
            }

            return _itemSlotCache[prefab.Identifier] = string.Join(" ", slots).ToLowerInvariant();
        }

        public IEnumerable<Prefab> GetAll(ISOSPrefabFilter filter)
        {
            return ItemPrefab.Prefabs
                .Where(p => Matches(p, filter));
        }

        private static bool Matches(ItemPrefab p, ISOSPrefabFilter filter)
        {
            if (filter.Mod.Count > 0 && !filter.Mod.Any(m =>
                (p.ContentPackage?.Name ?? "Vanilla").Contains(m, StringComparison.OrdinalIgnoreCase)))
                return false;

            if (filter.Category.Count > 0 && !filter.Category.Any(c =>
                p.Category.ToString().Contains(c, StringComparison.OrdinalIgnoreCase)))
                return false;

            if (filter.ID.Count > 0 && !filter.ID.Any(id =>
                p.Identifier.Value.Contains(id, StringComparison.OrdinalIgnoreCase)))
                return false;

            if (filter.Slot.Count > 0 && !filter.Slot.Any(s =>
                GetItemSlotsCached(p).Contains(s, StringComparison.OrdinalIgnoreCase)))
                return false;

            foreach (var t in filter.Tag)
                if (!p.Tags.Any(pt => pt.Value.Contains(t, StringComparison.OrdinalIgnoreCase)))
                    return false;

            return MatchesGeneral(p, filter);
        }

        private static bool MatchesGeneral(ItemPrefab p, ISOSPrefabFilter filter)
        {
            if (filter.General.Count == 0) return true;

            string name = p.Name.Value;
            string id = p.Identifier.Value;
            string modName = p.ContentPackage?.Name ?? "Vanilla";

            foreach (var term in filter.General)
            {
                bool found = name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                             id.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                             modName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                             p.Category.ToString().Contains(term, StringComparison.OrdinalIgnoreCase) ||
                             GetItemSlotsCached(p).Contains(term, StringComparison.OrdinalIgnoreCase) ||
                             p.Tags.Any(t => t.Value.Contains(term, StringComparison.OrdinalIgnoreCase));
                if (!found) return false;
            }

            return true;
        }

        public static void Destroy()
        {
            _itemSlotCache.Clear();
        }
    }

    public sealed class AfflictionPrefabProvider : ISOSPrefabAuto
    {
        public string Id => "AfflictionPrefab";
        public double Order => 2;
        public Type PrefabType => typeof(AfflictionPrefab);
        public string Header => Texts.Get("sos.list.header.afflictionprefab", "Affliction Prefab").Value;

        public IEnumerable<Prefab> GetAll(ISOSPrefabFilter filter)
        {
            return AfflictionPrefab.List
                .Where(a => Matches(a, filter))
                .OrderBy(a => a is AfflictionPrefabHusk ? 1 : 0);
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

    public sealed class AfflictionPrefabHuskProvider : ISOSPrefabAuto
    {
        public string Id => "AfflictionPrefabHusk";
        public double Order => 2.5;
        public Type PrefabType => typeof(AfflictionPrefabHusk);
        public string Header => Texts.Get("sos.list.header.afflictionprefabhusk", "Affliction Prefab Husk").Value;
        public IEnumerable<Prefab> GetAll(ISOSPrefabFilter filter) => [];
    }
}
