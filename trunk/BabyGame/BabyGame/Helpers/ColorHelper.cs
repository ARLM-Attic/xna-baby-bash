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
    public static class ColorExtensions
    {
        public static Microsoft.Xna.Framework.Color ToXnaColour(this System.Drawing.Color c)
        {
            return new Microsoft.Xna.Framework.Color(c.R, c.G, c.B, c.A);
        }
    }

    public static class ColorHelper
    {
        public static Microsoft.Xna.Framework.Color FromArgbHexString(String s)
        {
            if (String.IsNullOrWhiteSpace(s))
                throw new ArgumentNullException("s");
            if (s.Length != 8)
                throw new ArgumentOutOfRangeException("s", "String must be 8 characters long.");

            var a = Int32.Parse(s.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            var r = Int32.Parse(s.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            var g = Int32.Parse(s.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            var b = Int32.Parse(s.Substring(5, 2), System.Globalization.NumberStyles.HexNumber);
            
            return new Microsoft.Xna.Framework.Color(r, g, b, a);
        }

        public static Microsoft.Xna.Framework.Color Parse(String s)
        {
            if (String.IsNullOrWhiteSpace(s))
                throw new ArgumentNullException("s");

            // Try parsing as a known colour.
            // As there is no way to know if FromName() parsed correctly. Try with two initial values.
            var c = System.Drawing.Color.FromName(s).ToXnaColour();
            if (c.PackedValue == 0)
                c = Microsoft.Xna.Framework.Color.White;
            c = System.Drawing.Color.FromName(s).ToXnaColour();

            if (c.PackedValue == 0)
                // Still can't parse, try as a hex string.
                c = ColorHelper.FromArgbHexString(s);
            if (c.PackedValue == 0)
                // Still can't parse!! Give up.
                throw new BabyPackageLoadException();
            
            return c;
        }
    }
}
