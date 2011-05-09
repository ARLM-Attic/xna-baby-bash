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
using Microsoft.Xna.Framework.Input;

namespace MurrayGrant.BabyGame
{
    // PERFORMANCE: turn this into a flags field. And IEnumerable<MouseButton> into MouseButton.
    public enum MouseButton
    {
        Left,
        Right,
        Middle,
        X1,
        X2,
    }

    public enum MouseWheelDirection
    {
        Up,
        Down
    }

    public static class MouseHelper
    {
        public static bool AnyButtonPressed(this MouseState state)
        {
            return (
                       state.LeftButton == ButtonState.Pressed
                    || state.RightButton == ButtonState.Pressed
                    || state.MiddleButton == ButtonState.Pressed
                    || state.XButton1 == ButtonState.Pressed
                    || state.XButton2 == ButtonState.Pressed
                   );
        }

        public static IEnumerable<MouseButton> GetPressedButtons (this MouseState state)
        {
            if (!state.AnyButtonPressed())
                return new MouseButton[0];
            
            var result = new List<MouseButton>();

            if (state.LeftButton == ButtonState.Pressed)
                result.Add(MouseButton.Left);
            if (state.RightButton == ButtonState.Pressed)
                result.Add(MouseButton.Right);
            if (state.MiddleButton == ButtonState.Pressed)
                result.Add(MouseButton.Middle);
            if (state.XButton1 == ButtonState.Pressed)
                result.Add(MouseButton.X1);
            if (state.XButton2 == ButtonState.Pressed)
                result.Add(MouseButton.X2);

            return result;
        }
    }
}
