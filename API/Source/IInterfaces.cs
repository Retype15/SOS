// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using Barotrauma;

namespace SOS
{
    public interface IIdentifier
    {
        string Id => GetType().FullName ?? GetType().Name;
    }

    public interface IBaseStatSection
    {
        bool Analyze(Prefab item);
        void Draw(GUIListBox contentPanel, Action<Prefab> onPrimary, Action<Prefab> onSecondary);
    }

    public interface ISOSStatSection : IIdentifier, IBaseStatSection
    {
        int Order { get; }
    }
}

