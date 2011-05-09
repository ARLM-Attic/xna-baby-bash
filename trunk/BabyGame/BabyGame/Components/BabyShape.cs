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
using MurrayGrant.BabyGame.Helpers;

namespace MurrayGrant.BabyGame
{
    /// <summary>
    /// This is a game component that implements IUpdateable.
    /// </summary>
    public class BabyShape : Nuclex.Game.DrawableComponent, ISharedSpriteBatchAndLifeCycle
    {
        public enum DrawingState
        {
            PreShow,
            FadeIn,
            SpinIn,
            Solid,
            FadeOut,
            PostShow,
        }

        public Vector2 Location { get; set; }       // This is the centre of the texture rather than the top left corner.
        public SpriteBatch SpriteBatch { get; set; }
        public bool AtEndOfLife { get { return this.State == DrawingState.PostShow; } }
        public DrawingState State { get; private set; }
        private TimeSpan RemainingTimeInCurrentState { get; set; }
        public float Rotation { get; set; }     // Rotation offset of the object in radians (ie: 1.0f = 180 degrees, -1.0f = -180 degrees).

        public TimeSpan FadeInTime { get; set; }
        public TimeSpan SpinTime { get; set; }
        public TimeSpan SolidTime { get; set; }
        public TimeSpan FadeOutTime { get; set; }

        public GameMain Game { get; set; }
        public Texture2D Texture { get; set; }
        public Vector2 ActualSize { get; private set; }
        private Vector2 _Size;
        public Vector2 Size
        {
            get
            {
                return this._Size;
            }
            set
            {
                this._Size = value;
                this.ActualSize = Vector2.Zero;
            }
        }
        public Color Colour { get; set; }

        public BabyShape(GameMain game) : base() 
        {
            this.Game = game;
            this.Location = Vector2.Zero;
            this.State = DrawingState.PreShow;
            this.RemainingTimeInCurrentState = TimeSpan.Zero;
            this.Colour = Color.White;
            this.Size = Vector2.Zero;
        }

        public void ResetTimeInCurrentState()
        {
            switch (this.State)
            {
                case DrawingState.PreShow:
                    break;
                case DrawingState.FadeIn:
                    this.RemainingTimeInCurrentState = this.FadeInTime;
                    break;
                case DrawingState.SpinIn:
                    this.RemainingTimeInCurrentState = this.SpinTime;
                    break;
                case DrawingState.Solid:
                    this.RemainingTimeInCurrentState = this.SolidTime;
                    break;
                case DrawingState.FadeOut:
                    this.RemainingTimeInCurrentState = this.FadeOutTime;
                    break;
                case DrawingState.PostShow:
                    break;
                default:
                    throw new ApplicationException("Forgot a DrawingState.");
            }
        }

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            if (this.FadeInTime > TimeSpan.Zero && this.SpinTime > TimeSpan.Zero)
                throw new InvalidOperationException("Unable to Fade In and Spin In at the same time. Set FadeInTime or SpinTime to TimeSpan.Zero.");

            if (this.ActualSize == Vector2.Zero)
                this.ActualSize = Vector2Helper.ResizeKeepingAspectRatio(this.Size, this.Texture.Bounds);

            if (this.Location == Vector2.Zero)
            {
                // Choose a location for the shape to appear.
                this.Location = new Vector2(
                        (float)this.Game.RandomGenerator.Next(this.Game.GraphicsDevice.Viewport.X + (int)(this.ActualSize.X * 0.2f), this.Game.GraphicsDevice.Viewport.Width - (int)(this.ActualSize.X * 0.2f))
                        , (float)this.Game.RandomGenerator.Next(this.Game.GraphicsDevice.Viewport.Y + (int)(this.ActualSize.Y * 0.2f), this.Game.GraphicsDevice.Viewport.Height - (int)(this.ActualSize.Y * 0.2f))
                    );

            }

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
                            this.RemainingTimeInCurrentState = this.SolidTime;
                        }
                        break;
                    case DrawingState.FadeIn:
                        this.State = DrawingState.Solid;
                        this.RemainingTimeInCurrentState = this.SolidTime;
                        break;
                    case DrawingState.SpinIn:
                        this.State = DrawingState.Solid;
                        this.RemainingTimeInCurrentState = this.SolidTime;
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
            var spinFactor = ((float)this.RemainingTimeInCurrentState.TotalSeconds / (float)this.SpinTime.TotalSeconds) * (MathHelper.TwoPi * 2f) + this.Rotation;     // Spin twice per second (4 radians).
            var scaleFactor = (this.ActualSize.X / (float)this.Texture.Width) * Math.Abs((float)(this.SpinTime.Subtract(this.RemainingTimeInCurrentState).TotalSeconds)) * (1f / (float)this.SpinTime.TotalSeconds);   // Scale from small to full size.
            this.SpriteBatch.Draw(this.Texture, this.TopLeftOnScreen, null, this.Colour, spinFactor, this.TextureCentre, scaleFactor, SpriteEffects.None, 0);
        }
        private void DrawFadeIn(GameTime gameTime)
        {
            this.SpriteBatch.Draw(this.Texture, this.DestinationRectangle, null, this.Colour * ((float)this.FadeInTime.Subtract(this.RemainingTimeInCurrentState).TotalSeconds / (float)this.FadeInTime.TotalSeconds), this.Rotation * MathHelper.TwoPi, this.TextureCentre, SpriteEffects.None, 0f);
        }
        private void DrawSolid(GameTime gameTime)
        {
            this.SpriteBatch.Draw(this.Texture, this.DestinationRectangle, null, this.Colour, this.Rotation * MathHelper.TwoPi, this.TextureCentre, SpriteEffects.None, 0f);
        }
        private void DrawFadeOut(GameTime gameTime)
        {
            this.SpriteBatch.Draw(this.Texture, this.DestinationRectangle, null, this.Colour * ((float)(this.RemainingTimeInCurrentState.TotalSeconds) / (float)this.FadeOutTime.TotalSeconds), this.Rotation * MathHelper.TwoPi, this.TextureCentre, SpriteEffects.None, 0f);
        }


        private Rectangle DestinationRectangle
        {
            get
            {
                var topLeft = this.TopLeftOnScreen;
                return new Rectangle((int)topLeft.X, (int)topLeft.Y, (int)this.ActualSize.X, (int)this.ActualSize.Y);
            }
        }
        private Vector2 TopLeftOnScreen
        {
            get
            {
                return this.Location;
            }
        }
        private Vector2 TextureCentre
        {
            get
            {
                return new Vector2((float)this.Texture.Width, (float)this.Texture.Height) / 2;
            }
        }
    }
}
