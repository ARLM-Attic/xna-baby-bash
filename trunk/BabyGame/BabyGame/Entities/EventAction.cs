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
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;

namespace MurrayGrant.BabyGame.Entities
{
    public enum EventType : byte
    {
        None = 0,
        Start,
        KeyPress,
        KeyBashing,
        MouseMove,
        MouseButtonPress,
        MouseWheel,
        ControllerButtonPress,
        ControllerAnalogueMove,
        Idle,
    }

    public class EventAction
    {
        public BabyShape Template { get; set; }
        public List<Texture2D> TexturePool { get; set; }
        public List<SoundEffect> SoundPool { get; set; }
        public List<Color> ColourPool { get; set; }

        public EventAction() 
        {
            this.TexturePool = new List<Texture2D>();
            this.SoundPool = new List<SoundEffect>();
            this.ColourPool = new List<Color>();
        }
        public EventAction(BabyShape template, List<Texture2D> texturePool, List<SoundEffect> soundPool, List<Color> colourPool)
        {
            this.Template = template;
            this.TexturePool = texturePool;
            this.SoundPool = soundPool;
            this.ColourPool = colourPool;
        }
    }

    public struct EventKey
        : IEquatable<EventKey>, IEquatable<int>, IEquatable<ControllerButton>, IEquatable<MouseWheelDirection>, IEquatable<MouseButton>, IEquatable<Keys>
    {
        private int _Val;       // High 8 bits is KeyType, low 24 bits is the enum value.

        public static readonly EventKey Empty = default(EventKey);

        public EventType KeyType { get { return (EventType)(this._Val >> 24); } }
        public int Value { get { return this._Val; } }
        public ControllerButton ValueAsControllerButton { get { return (ControllerButton)(this._Val & 0x00ffffff); } }
        public MouseWheelDirection ValueAsMouseWheelDirection { get { return (MouseWheelDirection)(this._Val & 0x00ffffff); } }
        public MouseButton ValueAsMouseButton { get { return (MouseButton)(this._Val & 0x00ffffff); } }
        public Keys ValueAsKeyboard { get { return (Keys)(this._Val & 0x00ffffff); } }

        #region Contructor
        public EventKey(EventType t, ControllerButton val)
        {
            this._Val = ((int)t << 24) | ((int)val & 0x00ffffff);
        }

        public EventKey(EventType t, MouseWheelDirection val)
        {
            this._Val = ((int)t << 24) | ((int)val & 0x00ffffff);
        }

        public EventKey(EventType t, MouseButton val)
        {
            this._Val = ((int)t << 24) | ((int)val & 0x00ffffff);
        }

        public EventKey(EventType t, Keys val)
        {
            this._Val = ((int)t << 24) | ((int)val & 0x00ffffff);
        }

        public EventKey(EventType t, int val)
        {
            this._Val = ((int)t << 24) | (val & 0x00ffffff);
        }
        #endregion

        #region Equality
        public override int GetHashCode()
        {
            return this._Val.GetHashCode() ^ typeof(EventKey).GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (obj.GetType() == typeof(EventKey))
                return this.Equals((EventKey)obj);
            else if (obj.GetType() == typeof(int))
                return this.Equals((int)obj);
            else
                return false;
        }
        public bool Equals(int other)
        {
            return (this._Val == other);
        }
        public bool Equals(EventKey other)
        {
            return (this._Val == other._Val);
        }
        public bool Equals(ControllerButton other)
        {
            return (this.KeyType == EventType.ControllerButtonPress) && (this.ValueAsControllerButton == other);
        }
        public bool Equals(MouseWheelDirection other)
        {
            return (this.KeyType == EventType.MouseWheel) && (this.ValueAsMouseWheelDirection == other);
        }
        public bool Equals(MouseButton other)
        {
            return (this.KeyType == EventType.MouseButtonPress) && (this.ValueAsMouseButton == other);
        }
        public bool Equals(Keys other)
        {
            return (this.KeyType == EventType.KeyPress) && (this.ValueAsKeyboard == other);
        }
        #endregion
    }
}
