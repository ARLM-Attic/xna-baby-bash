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
    public static class ExceptionHelper
    {
        /// <summary>
        /// Gets a string which includes all inner exceptions.
        /// </summary>
        /// <returns></returns>
        public static String ToFullString(this Exception ex)
        {
            var e = ex;
            var result = e.ToString();

            while (e.InnerException != null)
                e = e.InnerException;

            return result;
        }
    }
}
