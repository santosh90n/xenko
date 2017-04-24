// Copyright (c) 2014-2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.
using System.Diagnostics;
using SiliconStudio.Core;
using SiliconStudio.Core.Storage;

namespace SiliconStudio.Xenko.Shaders
{
    /// <summary>
    /// Description of a constant buffer.
    /// </summary>
    [DataContract]
    [DebuggerDisplay("cbuffer {Name} : {Size} bytes")]
    public class EffectConstantBufferDescription
    {
        /// <summary>
        /// The name of this constant buffer.
        /// </summary>
        public string Name;

        /// <summary>
        /// The size in bytes.
        /// </summary>
        public int Size;

        /// <summary>
        /// The type of constant buffer.
        /// </summary>
        public ConstantBufferType Type;

        /// <summary>
        /// The members of this constant buffer.
        /// </summary>
        public EffectValueDescription[] Members;

        [DataMemberIgnore]
        public ObjectId Hash;

        /// <summary>
        /// Clone the current instance of the constant buffer description.
        /// </summary>
        /// <returns>A clone copy of the description</returns>
        public EffectConstantBufferDescription Clone()
        {
            return (EffectConstantBufferDescription)MemberwiseClone();
        }
    }
}
