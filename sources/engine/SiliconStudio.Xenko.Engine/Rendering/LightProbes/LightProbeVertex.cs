// Copyright (c) 2014-2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.

using System.Runtime.InteropServices;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Xenko.Graphics;

namespace SiliconStudio.Xenko.Rendering.LightProbes
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct LightProbeVertex
    {
        /// <summary>
        /// Initializes a new <see cref="LightProbeVertex"/> instance.
        /// </summary>
        /// <param name="position">The position of this vertex.</param>
        /// <param name="lightprobeId">The lightprobe ID.</param>
        public LightProbeVertex(Vector3 position, uint lightprobeId)
            : this()
        {
            Position = position;
            LightProbeId = lightprobeId;
        }

        /// <summary>
        /// XYZ position.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// The lightprobe index.
        /// </summary>
        public uint LightProbeId;

        /// <summary>
        /// Defines structure byte size.
        /// </summary>
        public static readonly int Size = 16;

        /// <summary>
        /// The vertex layout of this struct.
        /// </summary>
        public static readonly VertexDeclaration Layout = new VertexDeclaration(VertexElement.Position<Vector3>(), new VertexElement("LIGHTPROBE_ID", PixelFormat.R32_UInt));
    }
}
