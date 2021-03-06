// Copyright (c) 2014-2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.

using System.Runtime.CompilerServices;

namespace SiliconStudio.Xenko.Engine
{
    /// <summary>
    /// Extensions for <see cref="RenderGroupMask"/>
    /// </summary>
    public static class RenderGroupMaskExtensions
    {
        /// <summary>
        /// Determines whether the group mask contains the specified group.
        /// </summary>
        /// <param name="mask">The mask.</param>
        /// <param name="group">The group.</param>
        /// <returns><c>true</c> if the group mask contains the specified group; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains(this RenderGroupMask mask, RenderGroup group)
        {
            return ((uint)mask & (1 << (int)group)) != 0;
        }
    }
}
