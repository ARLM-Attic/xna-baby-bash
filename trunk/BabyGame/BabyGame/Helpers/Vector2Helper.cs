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

namespace MurrayGrant.BabyGame.Helpers
{
    public static class Vector2Helper
    {
        public static Vector2 GetProportionalSize(float percent, Viewport viewport)
        {
            return new Vector2(viewport.Height * percent);
        }

        public static Vector2 ResizeKeepingAspectRatio(Rectangle desiredSize, Vector2 originalSize)
        {
            return ResizeKeepingAspectRatio(new Vector2(desiredSize.Width, desiredSize.Height), originalSize);
        }

        public static Vector2 ResizeKeepingAspectRatio(Rectangle desiredSize, Rectangle originalSize)
        {
            return ResizeKeepingAspectRatio(new Vector2(desiredSize.Width, desiredSize.Height), new Vector2(originalSize.Width, originalSize.Height));
        }
        public static Vector2 ResizeKeepingAspectRatio(Vector2 desiredSize, Rectangle originalSize)
        {
            return ResizeKeepingAspectRatio(desiredSize, new Vector2(originalSize.Width, originalSize.Height));
        }
        public static Vector2 ResizeKeepingAspectRatio(Vector2 desiredSize, Vector2 originalSize)
        {
            float resizeRatio;

            // Try X axis.
            resizeRatio = desiredSize.X / originalSize.X;
            if ((originalSize.Y * resizeRatio) <= (desiredSize.Y + 0.01))
                return new Vector2(originalSize.X * resizeRatio, originalSize.Y * resizeRatio);

            // Try Y axis.
            resizeRatio = desiredSize.Y / originalSize.Y;
            if ((originalSize.X * resizeRatio) <= (desiredSize.X + 0.01))
                return new Vector2(originalSize.X * resizeRatio, originalSize.Y * resizeRatio);

            throw new ApplicationException("Unable to resize.");
        }

    }
}
