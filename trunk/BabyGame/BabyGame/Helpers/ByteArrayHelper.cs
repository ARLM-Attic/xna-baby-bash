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

namespace MurrayGrant.BabyGame.Helpers
{
    public static class ByteArrayHelper
    {
        public static byte[] ToLittleEndian(this byte[] arr)
        {
            if (BitConverter.IsLittleEndian)
                return arr;

            byte temp;
            int highCtr = arr.Length - 1;

            for (int ctr = 0; ctr < arr.Length / 2; ctr++, highCtr -= 1)
            {
                temp = arr[ctr];
                arr[ctr] = arr[highCtr];
                arr[highCtr] = temp;
            }
            return arr;
        }
    }
}
