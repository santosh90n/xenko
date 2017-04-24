// Copyright (c) 2014-2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.

using SiliconStudio.Core;

namespace SiliconStudio.Xenko.Games.Testing.Requests
{
    [DataContract]
    internal class TestRegistrationRequest : TestRequestBase
    {
        public string Cmd;
        public string GameAssembly;
        public int Platform;
        public bool Tester;
    }
}
