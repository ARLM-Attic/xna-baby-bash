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
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace MurrayGrant.BabyGame
{
    public class AnimatedSprite
        : Nuclex.Game.DrawableComponent
    {
        private TimeSpan _NextFrameTime = TimeSpan.MinValue;
        private int _CurrentFrame = -1;
        public Vector2 FrameSize { get; set; }
        public Texture2D Animation { get; set; }
        public Vector2 Position { get; set; }
        public float FramesPerSecond  { get; set; }
        public GameMain Game { get; private set; }
        public SpriteBatch SpriteBatch {get; set; } 
        private int TotalFrames
        {
            get
            {
                if (this.Animation != null && this.FrameSize.X != 0)
                    return (int)(this.Animation.Bounds.Width / this.FrameSize.X);
                else
                    return 0;
            }
        }

        public AnimatedSprite(GameMain game)
            : base()
        {
            this.Game = game;
            this.FramesPerSecond = 12;
            this.Position = Vector2.One;
            this.EnabledChanged += new EventHandler<EventArgs>(AnimatedSprite_EnabledChanged);

        }

        void AnimatedSprite_EnabledChanged(object sender, EventArgs e)
        {
            if (!this.Enabled)
            {
                this._CurrentFrame = -1;
                this._NextFrameTime = TimeSpan.MinValue;
            }
        }


        public override void Update(GameTime gameTime)
        {
            if (this.Enabled)
            {
                if (this._NextFrameTime < gameTime.TotalGameTime)
                {
                    this._NextFrameTime = gameTime.TotalGameTime.Add(TimeSpan.FromSeconds(1 / this.FramesPerSecond));   // Determine when the next frame should appear.
                    this._CurrentFrame += 1;                    // Update the frame we're up to.
                    this._CurrentFrame %= this.TotalFrames;     // Ensure we don't go past the end of the frames.
                }
            }
 	        base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            // Draw based on tiled frames in a single texture.
            // This assumes the frames are tiled across the texture (which will probably break if the texture is too wide).
            this.SpriteBatch.Draw(
                            this.Animation, 
                            new Rectangle((int)this.Position.X, (int)this.Position.Y, (int)this.FrameSize.X, (int)this.FrameSize.Y), 
                            new Rectangle(this._CurrentFrame * (int)this.FrameSize.X, 0, (int)this.FrameSize.X, (int)this.FrameSize.Y)
                            , Color.White
                        );

            base.Draw(gameTime);
        }
    }
}
