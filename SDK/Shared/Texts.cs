// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using Barotrauma;

namespace SOS
{
    public static class Texts
    {
        private static readonly Dictionary<string, Dictionary<Identifier, string>> prefixCache = [];
        public static LocalizedString Get(string key, string fallback = "", bool forceFallback = false)
        {
            var text = TextManager.Get(key);

            if (!string.IsNullOrEmpty(fallback))
            {
                if (forceFallback) return fallback;
#if DEBUG
                return text.Fallback("[NT]" + fallback); // NT=NOT-TRANSLATED
#else
                return text.Fallback(fallback);
#endif
            }
            return text;
        }

        public static Dictionary<Identifier, string> GetTranslationsByPrefix(string prefix)
        {
            if (prefixCache.TryGetValue(prefix, out var cached)) return cached;

            var allTranslations = TextManager.GetAllTagTextPairs();
            var filtered = allTranslations
                .Where(kvp => kvp.Key.StartsWith(prefix))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            prefixCache[prefix] = filtered;
            return filtered;
        }
    }
}