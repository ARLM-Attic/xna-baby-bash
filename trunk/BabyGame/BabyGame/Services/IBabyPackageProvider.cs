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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;

namespace MurrayGrant.BabyGame.Services
{
    /// <summary>
    /// The interface to loading a baby package and using that
    /// baby package to map key presses, mouse movements, etc to 
    /// letters, words, sounds and pictures displayed on screen.
    /// </summary>
    public interface IBabyPackageProvider
        : IDisposable
    {
        void Load(string packagePathAndFile, CancellationToken cancelMarker);
        void Load(System.IO.FileInfo packageFile, CancellationToken cancelMarker);

        bool Loaded { get; }
        
        String Author { get; set; }
        String Title { get; set; }
        String Description { get; set; }
        Uri Website { get; set; }

        GameMain Game { get; set; }
        GameTime CurrentGameTime { get; set; }

        Texture2D GetBackground();
        Color GetBackgroundColour();

        ShapeAndSoundTuple Startup();

        ShapeAndSoundTuple MapKeyPress(Keys keyPress);
        
        ShapeAndSoundTuple DoMouseMovement(int currentX, int currentY, int lastX, int lastY, bool anyButtonPressed);
        ShapeAndSoundTuple DoMouseWheelMovement(int wheelState, int lastWheelState, bool anyButtonPressed);
        ShapeAndSoundTuple MapMouseButton(MouseButton buttonPress, int currentX, int currentY);

        ShapeAndSoundTuple MapControllerButton(ControllerButton buttonPress, PlayerIndex player);
        IEnumerable<ShapeAndSoundTuple> DoControllerAnalogue(ControllerAnalogue analogueInputs, PlayerIndex player);

        TimeSpan IdleTimeout { get; }
        ShapeAndSoundTuple DoIdleTimeout(TimeSpan idleTime);
        
        ShapeAndSoundTuple KeyBashingDetected();
    }

    public class InteractionEventArgs : EventArgs
    {
        public BabyShape Shape { get; set; }
        public InteractionEventArgs(BabyShape shape)
        {
            this.Shape = shape;
        }
    }

    public class ShapeAndSoundTuple
    {
        public ShapeAndSoundTuple(BabyShape shape, SoundEffect sound)
        {
            this.Shape = shape;
            this.Sound = sound;
        }
        public BabyShape Shape { get; set; }
        public SoundEffect Sound { get; set; }
    }
}

