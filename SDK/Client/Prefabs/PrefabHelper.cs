// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using System.Collections.Specialized;

namespace SOS.Prefabs
{
    public static class PrefabHelper
    {
        private static readonly HashSet<string> favorites = [];

        public static IReadOnlyCollection<string> Favorites => favorites;

        public static bool IsFavorite(string id) => favorites.Contains(id);

        public static bool AddFavorite(string id, bool emit = true)
        {
            var result = favorites.Add(id);
            if (emit) API.Emit(CommKeys.RefreshSearch);
            return result;
        }

        public static bool AddRangeFavorite(IEnumerable<string> ids, bool emit = true)
        {
            var result = false;
            foreach (var id in ids)
                result |= favorites.Add(id);
            if (emit) API.Emit(CommKeys.RefreshSearch);
            return result;
        }

        public static bool RemoveFavorite(string id, bool emit = true)
        {
            var result = favorites.Remove(id);
            if (emit) API.Emit(CommKeys.RefreshSearch);
            return result;
        }

        public static bool ToggleFavorite(string id, bool emit = true)
        {
            bool isFav = favorites.Contains(id);
            if (isFav) RemoveFavorite(id, emit); else AddFavorite(id, emit);
            return !isFav;
        }

        public static void ClearFavorites(bool emit = true)
        {
            favorites.Clear();
            if (emit) API.Emit(CommKeys.RefreshSearch);
        }
    }
}
