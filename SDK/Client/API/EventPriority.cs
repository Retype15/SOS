// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

namespace SOS
{
    public static class EventPriority
    {
        public const double System = -2;
        public const double State = -1;
        public const double Default = 0;
        public const double UI = 1;
        public const double PostUI = 2;
    }
}
