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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MurrayGrant.BabyGame.Helpers
{
    public static class StreamHelper
    {
        public static MemoryStream ToMemoryStream(this Stream s)
        {
            return ToMemoryStream(s, 0, 0);
        }

        public static MemoryStream ToMemoryStream(this Stream s, int startOffset)
        {
            return ToMemoryStream(s, startOffset, 0);
        }

        /// <summary>
        /// Makes a copy of a stream in memory.
        /// </summary>
        /// <param name="s">The stream to copy.</param>
        /// <param name="startOffset">A number of bytes to leave empty at the start of the resulting copy.</param>
        /// <param name="endOffset">A number of bytes to leave empty at the end of the resulting copy.</param>
        /// <returns></returns>
        public static MemoryStream ToMemoryStream(this Stream s, int startOffset, int endOffset)
        {
            var result = new MemoryStream(64 * 1024);
            byte[] buf = new byte[64 * 1024];

            // Leave bytes at the start of the copy.
            if (startOffset > 0)
            {
                result.SetLength(startOffset);
                result.Seek(startOffset, SeekOrigin.Begin);
            }

            var bytesRead = s.Read(buf, 0, buf.Length);
            while (bytesRead != 0)
            {
                result.Write(buf, 0, bytesRead);
                bytesRead = s.Read(buf, 0, buf.Length);
            }

            // Leave bytes at the end of the copy.
            if (endOffset > 0)
                result.SetLength(result.Length + endOffset);

            result.Seek(0, SeekOrigin.Begin);
            return result;        
        }
    }
}
