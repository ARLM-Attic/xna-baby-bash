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
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System.Threading;
using System.Threading.Tasks;
using MurrayGrant.BabyGame.Helpers;
using MurrayGrant.BabyGame.Services;
using System.IO;

namespace MurrayGrant.BabyGame
{
    /// <summary>
    /// A game which displays an unhandled exception.
    /// </summary>
    public class FailedGame : Game
    {
        private enum ScreenState
        {
            GeneralMessageCountdown,
            PublishException,
            TechnicalDisplay,
        }

        private readonly Exception _Exception;
        private const String GeneralMessageTemplateAutoPublish = 
            "Baby Bash XNA had an error and had to stop.\n" +
            "Sorry about that.\n\n" +

            "In {0:N0} seconds, Baby Bash will send details of the error to Murray to help fix it.\n\n" +

            "Press Enter to send details now.\n" +
            "Press Escape to exit without sending error details.\n";

        private const String GeneralMessageTemplateManualPublish =
            "Baby Bash XNA had an error and had to stop.\n" +
            "Sorry about that.\n\n" +

            "In {0:N0} seconds, Baby Bash will exit.\n\n" +

            "Press Enter to send details of the error to Murray to help fix it.\n" +
            "Press Escape to exit now.\n";

        private const String TechnicalMessageTemplate =
            "Technical details (also on desktop) (press Escape to exit, Enter to publish):";
        private const String PublishExceptionTemplate =
            "Baby Bash XNA is sending error details{0}";
        private const String PublishExceptionTemplateSuccess =
            "Baby Bash XNA successfully sent error details.";
        private const String PublishExceptionTemplateFailed =
            "Baby Bash XNA could not send error details.";


        private ScreenState _State = ScreenState.GeneralMessageCountdown;
        private Configuration _Config;

        private String _Message;
        private String _TheExceptionDump;

        private TimeSpan _WhenToSendDetails = TimeSpan.MinValue;
        private readonly TimeSpan PublishTimeout = new TimeSpan(TimeSpan.TicksPerSecond * 15);
        private int _PeriodCount;
        private Task _Publisher;
        private TimeSpan _PeriodTimer;
        private readonly TimeSpan PeriodTimeout = new TimeSpan(TimeSpan.TicksPerMillisecond * 500);
        private TimeSpan _FinalTimer;
        private ExceptionAndComputerDetail _DetailsToSend;
        private Boolean _SavedToDesktop = false;

        private SpriteBatch _SpriteBatch;
        private SpriteFont _LargeFont;
        private SpriteFont _SmallFont;
        private Texture2D _Bug;
 
        public FailedGame(Exception e)
        {
            var publisher = new ExceptionPublisher(this);
            this._DetailsToSend = publisher.CreateErrorDetails(e);
            this._Exception = e;

            var graphics = new GraphicsDeviceManager(this);
            this.Content.RootDirectory = "Content";
            this.IsMouseVisible = false;


            var cfgMgr = new ConfigurationManager(this);
            try
            {
                cfgMgr.Load();
                this._Config = cfgMgr.Current;
            }
            catch(Exception)
            {
                this._Config = new Configuration();
            }

#if !DEBUG
            graphics.IsFullScreen = true;
            // TODO: test this with multimonitor (probably OK for it to only show on one monitor).
            graphics.PreferredBackBufferHeight = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;
            graphics.PreferredBackBufferWidth = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;
#endif
        }
 
        protected override void LoadContent()
        {
            this._SpriteBatch = new SpriteBatch(GraphicsDevice);
            // TODO: load font sizes based on screen resolution.
            this._LargeFont = Content.Load<SpriteFont>("DejaVu Sans 14");
            this._SmallFont = Content.Load<SpriteFont>("DejaVu Sans 10");
            // This 30kB PNG blows out to 900kB using the XNA content pipeline, so load it directly as a PNG.
            using (var bugFile = new System.IO.FileStream(System.IO.Path.Combine(this.Content.RootDirectory, "bug.png"), FileMode.Open, FileAccess.Read))
                this._Bug = Texture2D.FromStream(this.GraphicsDevice, bugFile);
        }
 
        protected override void Update(GameTime gameTime)
        {
            var keyState = Keyboard.GetState();

            // Update game state.
            if (this._WhenToSendDetails == TimeSpan.MinValue)
                this._WhenToSendDetails = gameTime.TotalGameTime.Add(this.PublishTimeout);
            this.UpdateGameState(keyState);

            // Act on current state.
            if (this._State == ScreenState.GeneralMessageCountdown)
            {
                // Build a message.
                if (this._Config.AutoPublishException)
                    this._Message = String.Format(GeneralMessageTemplateAutoPublish, this._WhenToSendDetails.Add(new TimeSpan(TimeSpan.TicksPerSecond)).Seconds);
                else
                    this._Message = String.Format(GeneralMessageTemplateManualPublish, this._WhenToSendDetails.Add(new TimeSpan(TimeSpan.TicksPerSecond)).Seconds);
                if (this._Config.InDeveloperMode)
                    this._Message += "Press F12 to see technical details of the error.\n";
                this._WhenToSendDetails = this._WhenToSendDetails.Subtract(gameTime.ElapsedGameTime);
            }
            else if (this._State == ScreenState.TechnicalDisplay)
            {
                // Build the exception to display.
                this._Message = TechnicalMessageTemplate;
                if (String.IsNullOrEmpty(this._TheExceptionDump))
                    this._TheExceptionDump = this._Exception.ToFullString();

                // Fire off a thread to save to desktop (fire and forget).
                if (!this._SavedToDesktop)
                {
                    Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            this.SaveToDesktop();
                        }
                        catch (Exception)
                        {
                            // Don't worry if we can't save to desktop.
                        }
                    }, TaskCreationOptions.LongRunning);
                    this._SavedToDesktop = true;
                }
            }
            else if (this._State == ScreenState.PublishException)
            {
                if (this._Publisher == null)
                {
                    // Initialise.
                    this._PeriodCount = 0;
                    this._PeriodTimer = this.PeriodTimeout;
                    this._FinalTimer = new TimeSpan(TimeSpan.TicksPerSecond * 2);
                    this._Message = String.Format(PublishExceptionTemplate, new String('.', this._PeriodCount));

                    // Fire off the publisher thread.
                    this._Publisher = new Task(this.SendErrorDetails, TaskCreationOptions.LongRunning);
                    this._Publisher.Start();
                }
                else if (!this._Publisher.IsCompleted)
                {
                    // Animate the periods on the message to give some progress feedback.
                    this._PeriodTimer = this._PeriodTimer.Subtract(gameTime.ElapsedGameTime);
                    if (this._PeriodTimer < TimeSpan.Zero)
                    {
                        this._PeriodTimer = this.PeriodTimeout;
                        this._PeriodCount++;
                    }
                    if (this._PeriodCount > 4)
                        this._PeriodCount = 0;

                    this._Message = String.Format(PublishExceptionTemplate, new String('.', this._PeriodCount));

                }
                else if (this._Publisher.IsCompleted)
                {
                    // Notify user of publish success or failure for a short time.
                    if (!this._Publisher.IsFaulted)
                        this._Message = PublishExceptionTemplateSuccess;
                    else
                        this._Message = PublishExceptionTemplateFailed;

                    this._FinalTimer = this._FinalTimer.Subtract(gameTime.ElapsedGameTime);
                    if (this._FinalTimer < TimeSpan.Zero)
                        this.Exit();
                }
            }
            
            base.Update(gameTime);
        }

        private void SendErrorDetails()
        {
            var publisher = new ExceptionPublisher(this);
            publisher.Publish(this._DetailsToSend);
        }
        private void SaveToDesktop()
        {
            var publisher = new ExceptionPublisher(this);
            publisher.SaveToDesktop(this._DetailsToSend);
        }

        private void UpdateGameState(KeyboardState keyState)
        {
            if (this._State == ScreenState.GeneralMessageCountdown && (keyState.IsKeyDown(Keys.Enter) || this._WhenToSendDetails < TimeSpan.Zero))
                this._State = ScreenState.PublishException;
            else if (this._State == ScreenState.GeneralMessageCountdown && keyState.IsKeyDown(Keys.F12) && this._Config.InDeveloperMode)
                this._State = ScreenState.TechnicalDisplay;
            else if (this._State == ScreenState.GeneralMessageCountdown && keyState.IsKeyDown(Keys.Escape))
                this.Exit();

            else if (this._State == ScreenState.TechnicalDisplay && keyState.IsKeyDown(Keys.Enter))
                this._State = ScreenState.PublishException;
            else if (this._State == ScreenState.TechnicalDisplay && keyState.IsKeyDown(Keys.Escape))
                this.Exit();

        }
        protected override void Draw(GameTime gameTime)
        {
            this.GraphicsDevice.Clear(Color.Black);


            this._SpriteBatch.Begin();
            var drawingPosition = new Vector2(GraphicsDevice.Viewport.TitleSafeArea.X + 20, GraphicsDevice.Viewport.TitleSafeArea.Y + 20);
            if (this._State == ScreenState.GeneralMessageCountdown)
            {
                // Draw the message.
                this._SpriteBatch.DrawString(this._LargeFont, this._Message, drawingPosition, Color.White);

                // And the bug!
                var resized = Vector2Helper.ResizeKeepingAspectRatio(Vector2Helper.GetProportionalSize(0.5f, this.GraphicsDevice.Viewport), this._Bug.Bounds);
                var location = new Vector2((float)this.GraphicsDevice.Viewport.Bounds.Center.X, (float)(this.GraphicsDevice.Viewport.Bounds.Bottom - (resized.Y + 10)));
                var rect = new Rectangle((int)location.X, (int)location.Y, (int)resized.X, (int)resized.Y);
                var centre = new Vector2((float)this._Bug.Width, (float)this._Bug.Height) / 2;
                this._SpriteBatch.Draw(this._Bug, rect, null, Color.White, 0f, centre, SpriteEffects.None, 0);
            }
            else if (this._State == ScreenState.TechnicalDisplay)
            {
                // Draw the exception message.
                this._SpriteBatch.DrawString(this._LargeFont, this._Message, drawingPosition, Color.White);
                this._SpriteBatch.DrawString(this._SmallFont, this._TheExceptionDump, drawingPosition + new Vector2(0, 30), Color.White);
            }
            else if (this._State == ScreenState.PublishException)
            {
                // Draw the publishing message.
                this._SpriteBatch.DrawString(this._LargeFont, this._Message, drawingPosition, Color.White);

                // And the bug!
                var resized = Vector2Helper.ResizeKeepingAspectRatio(Vector2Helper.GetProportionalSize(0.5f, this.GraphicsDevice.Viewport), this._Bug.Bounds);
                var location = new Vector2((float)this.GraphicsDevice.Viewport.Bounds.Center.X, (float)(this.GraphicsDevice.Viewport.Bounds.Bottom - (resized.Y + 10)));
                var rect = new Rectangle((int)location.X, (int)location.Y, (int)resized.X, (int)resized.Y);
                var centre = new Vector2((float)this._Bug.Width, (float)this._Bug.Height) / 2;
                this._SpriteBatch.Draw(this._Bug, rect, null, Color.White, 0f, centre, SpriteEffects.None, 0);
            }
            this._SpriteBatch.End();
 
            base.Draw(gameTime);
        }
    }
}
