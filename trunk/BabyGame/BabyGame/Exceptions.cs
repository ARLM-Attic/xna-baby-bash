// Copyright 2011 Murray Grant
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;

namespace MurrayGrant.BabyGame
{
    [Serializable]
    public class BabyPackageLoadException : Exception
    {
        public BabyPackageLoadException() { }
        public BabyPackageLoadException(string message) : base(message) { }
        public BabyPackageLoadException(string message, Exception inner) : base(message, inner) { }
        protected BabyPackageLoadException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }

    [Serializable]
    public class TestException : Exception
    {
        public TestException() { }
        public TestException(string message) : base(message) { }
        public TestException(string message, Exception inner) : base(message, inner) { }
        protected TestException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }

    [Serializable]
    public class UpdateException : Exception
    {
        public UpdateException() { }
        public UpdateException(string message) : base(message) { }
        public UpdateException(string message, Exception inner) : base(message, inner) { }
        protected UpdateException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }

    [Serializable]
    public class UpdateNotSupportedException : Exception
    {
        public UpdateNotSupportedException() { }
        public UpdateNotSupportedException(string message) : base(message) { }
        public UpdateNotSupportedException(string message, Exception inner) : base(message, inner) { }
        protected UpdateNotSupportedException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}