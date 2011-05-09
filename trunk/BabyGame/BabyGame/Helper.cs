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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace MurrayGrant.BabyGame
{
    public static class Helper
    {
        public const string BabyPackageExtension = ".babypackage";

        public static Version GetApplicationVesion()
        {
            return new Version(((System.Reflection.AssemblyFileVersionAttribute)typeof(AboutDialog).Assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyFileVersionAttribute), false).GetValue(0)).Version);
        }
        public static void AddRange<T>(this IList<T> list, IEnumerable<T> items)
        {
            foreach (var item in items)
                list.Add(item);
        }

        public static T GetService <T>(this GameServiceContainer services) where T: class
        {
            return (T)services.GetService(typeof(T));
        }


        /// <summary>
        /// Gets all combined items from an enum value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        /// <example>
        /// Displays ValueA and ValueB.
        /// <code>
        /// EnumExample dummy = EnumExample.Combi;
        /// foreach (var item in dummy.GetAllSelectedItems<EnumExample>())
        /// {
        ///    Console.WriteLine(item);
        /// }
        /// </code>
        /// </example>
        /// <remarks>
        /// Found here: http://stackoverflow.com/questions/105372/c-how-to-enumerate-an-enum/944352#944352
        /// </remarks>
        public static IEnumerable<T> GetAllSelectedItems<T>(this Enum value)
        {
            int valueAsInt = Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);

            foreach (object item in Enum.GetValues(typeof(T)))
            {
                int itemAsInt = Convert.ToInt32(item, System.Globalization.CultureInfo.InvariantCulture);

                if (itemAsInt == (valueAsInt & itemAsInt))
                {
                    yield return (T)item;
                }
            }
        }

    }
}
