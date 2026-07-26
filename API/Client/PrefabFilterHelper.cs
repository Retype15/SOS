// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130

namespace SOS
{
    public static class PrefabFilterHelper
    {
        public static bool MatchesGeneral(ISOSPrefabFilter filter, string name, string identifier, string modName)
        {
            if (filter.General.Count == 0) return true;

            foreach (var term in filter.General)
            {
                if (name.Contains(term, StringComparison.OrdinalIgnoreCase)) continue;
                if (identifier.Contains(term, StringComparison.OrdinalIgnoreCase)) continue;
                if (modName.Contains(term, StringComparison.OrdinalIgnoreCase)) continue;
                return false;
            }

            return true;
        }
    }

    internal class SearchFilter : ISOSPrefabFilter
    {
        public List<string> General { get; } = [];
        public List<string> Mod { get; } = [];
        public List<string> Category { get; } = [];
        public List<string> Tag { get; } = [];
        public List<string> Slot { get; } = [];
        public List<string> ID { get; } = [];
        public List<string> PrefabType { get; } = [];

        public SearchFilter(string rawQuery)
        {
            if (string.IsNullOrWhiteSpace(rawQuery)) return;

            char currentType = ' ';
            int startIndex = 0;
            string query = rawQuery + " ";

            for (int i = 0; i < query.Length; i++)
            {
                char c = query[i];
                if (c == '@' || c == '#' || c == '$' || c == '&' || c == '!' || c == '%' || i == query.Length - 1)
                {
                    string content = query[startIndex..i].Trim();
                    if (!string.IsNullOrEmpty(content))
                    {
                        switch (currentType)
                        {
                            case ' ': General.Add(content); break;
                            case '@': Mod.Add(content); break;
                            case '#': Category.Add(content); break;
                            case '$': Tag.Add(content); break;
                            case '&': Slot.Add(content); break;
                            case '!': ID.Add(content); break;
                            case '%': PrefabType.Add(content); break;
                        }
                    }
                    currentType = c;
                    startIndex = i + 1;
                }
            }
        }

        internal bool AllowsType(string name)
            => PrefabType.Count == 0 || PrefabType.Any(t => name.Contains(t, StringComparison.OrdinalIgnoreCase));
    }
}
