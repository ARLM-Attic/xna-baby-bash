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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;


namespace MurrayGrant.BabyGame
{
    /// <summary>
    /// This handles all key press events.
    /// </summary>
    public class StringShape : Nuclex.Game.DrawableComponent, ISharedSpriteBatchAndLifeCycle
    {
        protected enum DrawingState
        {
            PreShow,
            FadeIn,
            SpinIn,
            Solid,
            FadeOut,
            PostShow,
        }

        public Vector2 Location { get; set; }
        public SpriteBatch SpriteBatch { get; set; }
        public bool AtEndOfLife { get { return this.State == DrawingState.PostShow; } }
        private DrawingState State { get; set; }
        private TimeSpan RemainingTimeInCurrentState { get; set; }

        public TimeSpan FadeInTime { get; set; }
        public TimeSpan SpinTime { get; set; }
        public TimeSpan OnScreenTime { get; set; }
        public TimeSpan FadeOutTime { get; set; }

        public GameMain Game { get; set; }
        public string String { get; set; }
        public Color Colour { get; set; }
        public Color ShadowColour { get; set; }
        public float ShadowOffset { get; set; }
        public SpriteFont Font { get; set; }

        public StringShape(GameMain game) : base() 
        {
            this.Game = game;
            this.Location = Vector2.Zero;
            this.State = DrawingState.PreShow;
            this.RemainingTimeInCurrentState = TimeSpan.Zero;
            this.Colour = Color.Transparent;
            this.ShadowColour = Color.Transparent;
            this.ShadowOffset = 1f;
        }

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            if (this.FadeInTime > TimeSpan.Zero && this.SpinTime > TimeSpan.Zero)
                throw new InvalidOperationException("Unable to Fade In and Spin In at the same time. Set FadeInTime or SpinTime to TimeSpan.Zero.");

            // Update the state machine for this lifespan of this object.
            while (this.RemainingTimeInCurrentState <= TimeSpan.Zero && this.State != DrawingState.PostShow)
            {
                switch (this.State)
                {
                    case DrawingState.PreShow:
                        if (this.FadeInTime > TimeSpan.Zero)
                        {
                            this.State = DrawingState.FadeIn;
                            this.RemainingTimeInCurrentState = this.FadeInTime;
                        }
                        else if (this.SpinTime > TimeSpan.Zero)
                        {
                            this.State = DrawingState.SpinIn;
                            this.RemainingTimeInCurrentState = this.SpinTime;
                        }
                        else
                        {
                            this.State = DrawingState.Solid;
                            this.RemainingTimeInCurrentState = this.OnScreenTime;
                        }
                        break;
                    case DrawingState.FadeIn:
                        this.State = DrawingState.Solid;
                        this.RemainingTimeInCurrentState = this.OnScreenTime;
                        break;
                    case DrawingState.SpinIn:
                        this.State = DrawingState.Solid;
                        this.RemainingTimeInCurrentState = this.OnScreenTime;
                        break;
                    case DrawingState.Solid:
                        this.State = DrawingState.FadeOut;
                        this.RemainingTimeInCurrentState = this.FadeOutTime;
                        break;
                    case DrawingState.FadeOut:
                        this.State = DrawingState.PostShow;
                        break;
                    case DrawingState.PostShow:
                        
                        break;
                    default:
                        throw new ApplicationException("Forgot a DrawingState.");
                }
            }
            this.RemainingTimeInCurrentState = this.RemainingTimeInCurrentState.Subtract(gameTime.ElapsedGameTime);

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            switch (this.State)
            {
                case DrawingState.PreShow:
                    break;
                case DrawingState.FadeIn:
                    this.DrawFadeIn(gameTime);
                    break;
                case DrawingState.SpinIn:
                    this.DrawSpinIn(gameTime);
                    break;
                case DrawingState.Solid:
                    this.DrawSolid(gameTime);
                    break;
                case DrawingState.FadeOut:
                    this.DrawFadeOut(gameTime);
                    break;
                case DrawingState.PostShow:
                    break;
                default:
                    throw new ApplicationException("Forgot a DrawingState.");
            }
            base.Draw(gameTime);
        }



        private void DrawSpinIn(GameTime gameTime)
        {
            this.DrawSpinInInternal(gameTime, this.Location + new Vector2(this.ShadowOffset), this.ShadowColour);
            this.DrawSpinInInternal(gameTime, this.Location, this.Colour);
        }
        private void DrawSpinInInternal(GameTime gameTime, Vector2 location, Color colour)
        {
            var spinFactor = ((float)(this.RemainingTimeInCurrentState.TotalSeconds) / (float)this.SpinTime.TotalSeconds) * (MathHelper.Pi * 2f * 2f);     // Spin twice per second (4 radians).
            var scaleFactor = Math.Abs((float)(this.SpinTime.Subtract(this.RemainingTimeInCurrentState).TotalSeconds)) * (1f / (float)this.SpinTime.TotalSeconds);   // Scale from small to full size.
            var textSize = this.Font.MeasureString(this.String);
            var spriteCentre = textSize / 2;
            var movedLocation = location + new Vector2(spriteCentre.X, spriteCentre.Y);
            this.SpriteBatch.DrawString(this.Font, this.String, movedLocation, colour, spinFactor, spriteCentre, scaleFactor, SpriteEffects.None, 0);
        }

        private void DrawFadeIn(GameTime gameTime)
        {
            this.SpriteBatch.DrawString(this.Font, this.String, this.Location + new Vector2(this.ShadowOffset), this.ShadowColour * ((float)this.FadeInTime.Subtract(this.RemainingTimeInCurrentState).TotalSeconds / (float)this.FadeInTime.TotalSeconds));
            this.SpriteBatch.DrawString(this.Font, this.String, this.Location, this.Colour * ((float)this.FadeInTime.Subtract(this.RemainingTimeInCurrentState).TotalSeconds / (float)this.FadeInTime.TotalSeconds));
        }

        private void DrawSolid(GameTime gameTime)
        {
            this.SpriteBatch.DrawString(this.Font, this.String, this.Location + new Vector2(this.ShadowOffset), this.ShadowColour);
            this.SpriteBatch.DrawString(this.Font, this.String, this.Location, this.Colour);
        }
        private void DrawFadeOut(GameTime gameTime)
        {
            this.SpriteBatch.DrawString(this.Font, this.String, this.Location + new Vector2(this.ShadowOffset), this.ShadowColour * ((float)(this.RemainingTimeInCurrentState.TotalSeconds) / (float)this.FadeOutTime.TotalSeconds));
            this.SpriteBatch.DrawString(this.Font, this.String, this.Location, this.Colour * ((float)(this.RemainingTimeInCurrentState.TotalSeconds) / (float)this.FadeOutTime.TotalSeconds));
        }
    }
}
