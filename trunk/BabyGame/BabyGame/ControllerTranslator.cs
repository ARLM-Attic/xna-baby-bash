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
    [Flags()]
    public enum ControllerButton
    {
        None = 0x0000,
        A = 0x0001,
        B = 0x0002,
        X = 0x0004,
        Y = 0x0008,
        LeftShoulder = 0x0010,
        RightShoulder = 0x0020,
        LeftStick = 0x0040,
        RightStick = 0x0080,
        Back = 0x0100,
        Start = 0x0200,
        DUp = 0x0400,
        DDown = 0x0800,
        DLeft = 0x1000,
        DRight = 0x2000,
    }

    internal struct ControllerButtonAndPlayer
    {
        private int ButtonAndPlayer;    // Button is stored in the low 16bits, Player is stored in the high 16bits.

        internal ControllerButton Button { get { return (ControllerButton)(this.ButtonAndPlayer & 0x0000ffff); } }
        internal PlayerIndex Player { get { return (PlayerIndex)((this.ButtonAndPlayer & 0xffff0000) >> 16); } }

        internal ControllerButtonAndPlayer(ControllerButton button, PlayerIndex controllerNumber)
        {
            this.ButtonAndPlayer = (int)controllerNumber << 16 | (int)button;
        }
        public override int GetHashCode()
        {
            return this.ButtonAndPlayer ^ typeof(ControllerButtonAndPlayer).GetHashCode();
        }
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (obj.GetType() != typeof(ControllerButtonAndPlayer))
                return false;

            return this.Equals((ControllerButtonAndPlayer)obj);
        }
        public bool Equals(ControllerButtonAndPlayer other)
        {
            return (this.ButtonAndPlayer == other.ButtonAndPlayer);
        }
    }

    public static class ControllerTranslator
    {
        public static ControllerButton GetPressedButtons(this GamePadState state)
        {
            var result = ControllerButton.None;

            // Note that GamePagButtons.BigButton doesn't map to anything on the standard XBox360 controller.

            if (state.Buttons.A == ButtonState.Pressed)
                result |= ControllerButton.A;
            if (state.Buttons.B == ButtonState.Pressed)
                result |= ControllerButton.B;
            if (state.Buttons.Back == ButtonState.Pressed)
                result |= ControllerButton.Back;
            if (state.Buttons.LeftShoulder == ButtonState.Pressed)
                result |= ControllerButton.LeftShoulder;
            if (state.Buttons.LeftStick == ButtonState.Pressed)
                result |= ControllerButton.LeftStick;
            if (state.Buttons.RightShoulder == ButtonState.Pressed)
                result |= ControllerButton.RightShoulder;
            if (state.Buttons.RightStick == ButtonState.Pressed)
                result |= ControllerButton.RightStick;
            if (state.Buttons.Start == ButtonState.Pressed)
                result |= ControllerButton.Start;
            if (state.Buttons.X == ButtonState.Pressed)
                result |= ControllerButton.X;
            if (state.Buttons.Y == ButtonState.Pressed)
                result |= ControllerButton.Y;
            if (state.DPad.Down == ButtonState.Pressed)
                result |= ControllerButton.DDown;
            if (state.DPad.Up == ButtonState.Pressed)
                result |= ControllerButton.DUp;
            if (state.DPad.Left == ButtonState.Pressed)
                result |= ControllerButton.DLeft;
            if (state.DPad.Right == ButtonState.Pressed)
                result |= ControllerButton.DRight;

            return result;
        }
        
        public static ControllerAnalogue AnalogueState(this GamePadState state)
        {
            return new ControllerAnalogue(state);
        }

    }

    [Flags()]
    public enum ControllerReversal
    {
        None = 0,
        LeftThumb = 0x1,
        RightThumb = 0x2,
        BothThumbs = 0x3
    }

    public class ControllerAnalogue
    {
        public ControllerAnalogue(GamePadState state)
        {
            this.LeftThumbStick = state.ThumbSticks.Left;
            this.RightThumbStick = state.ThumbSticks.Right;
            this.LeftTrigger = state.Triggers.Left;
            this.RightTrigger = state.Triggers.Right;
        }
        public ControllerAnalogue(GamePadState state, ControllerReversal reversal)
        {
            if (!reversal.HasFlag(ControllerReversal.LeftThumb))
                this.LeftThumbStick = state.ThumbSticks.Left;
            else
                this.LeftThumbStick = new Vector2(state.ThumbSticks.Left.X, state.ThumbSticks.Left.Y * -1);

            if (!reversal.HasFlag(ControllerReversal.RightThumb))
                this.RightThumbStick = state.ThumbSticks.Right;
            else
                this.RightThumbStick = new Vector2(state.ThumbSticks.Right.X, state.ThumbSticks.Right.Y * -1);
            this.LeftTrigger = state.Triggers.Left;
            this.RightTrigger = state.Triggers.Right;
        }

        public Vector2 LeftThumbStick { get; set; }
        public Vector2 RightThumbStick { get; set; }
        public float LeftTrigger { get; set; }
        public float RightTrigger { get; set; }

        private static float Error = 0.001f;
        public bool IsZero
        {
            get
            {
                return
                    Math.Abs(this.LeftThumbStick.X) <= Error &&
                    Math.Abs(this.LeftThumbStick.Y) <= Error &&
                    Math.Abs(this.RightThumbStick.X) <= Error &&
                    Math.Abs(this.RightThumbStick.Y) <= Error &&
                    Math.Abs(this.LeftTrigger) <= Error &&
                    Math.Abs(this.RightTrigger) <= Error;
            }
        }
    }
}
