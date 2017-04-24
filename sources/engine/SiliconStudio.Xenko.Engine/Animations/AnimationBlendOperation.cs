// Copyright (c) 2014-2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.

using SiliconStudio.Core;

namespace SiliconStudio.Xenko.Animations
{
    /// <summary>
    /// Describes the type of animation blend operation.
    /// </summary>
    [DataContract]
    public enum AnimationBlendOperation
    {
        /// <summary>
        /// Linear blend operation.
        /// </summary>
        [Display("Linear blend")]
        LinearBlend = 0,

        /// <summary>
        /// Add operation.
        /// </summary>
        [Display("Additive")]
        Add = 1,
    }

    /// <summary>
    /// Core animation operations support all operations exposed for blending as well as several required for the animation building itself
    /// </summary>
    public enum CoreAnimationOperation
    {
        Blend = 0,

        Add = 1,

        Subtract = 2,
    }
}
