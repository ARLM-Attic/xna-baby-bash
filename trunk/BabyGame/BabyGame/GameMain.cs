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
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Nuclex.Game.States;
using Nuclex.Input;
using MurrayGrant.BabyGame.Services;
using MurrayGrant.BabyGame.Components;
using System.Threading;
using System.Threading.Tasks;

namespace MurrayGrant.BabyGame
{
    /// <summary>
    /// The main game class, although most logic resides elsewhere.
    /// </summary>
    public class GameMain : Microsoft.Xna.Framework.Game
    {
        #region Members
        #region RandomGenerator
        private static System.Threading.ThreadLocal<Random> _RandomGenerator = new System.Threading.ThreadLocal<Random>(
                                            () => new Random(Environment.TickCount ^ System.Threading.Thread.CurrentThread.ManagedThreadId) );
        public Random RandomGenerator  { get { return _RandomGenerator.Value; } }
        #endregion
        private const long TicksFor30Fps = 333334;  // ~33ms
        private const long TicksFor60Fps = 166667;  // ~16ms

        private bool _LowPowerMode = false;
        private SpriteFont _TextFont;
        private SpriteBatch _SpriteBatch;
        private FrameTimerComponent _Timers;
        private IConfigurationService _CfgMgr;

        public IList<GameMain> OtherScreens { get; set; }
        public bool IsMasterScreen { get { return (this.OtherScreens.Count > 0); } }

        private CancellationTokenSource _LoaderCancelObj;
        #endregion

        #region Initialization Methods
        public GameMain(System.Windows.Forms.Screen monitor)
            : base()
        {
            var graphics = new MultiMonitorGraphicsDeviceManager(this, monitor);
            this.Content.RootDirectory = "Content";
            this.IsMouseVisible = true;
            this.OtherScreens = new List<GameMain>();
            this.Services.AddService(typeof(TaskManager), new TaskManager(this));

            this.Components.Add(new Nuclex.Input.InputManager(this.Services, this.Window.Handle));

            var gsm = new GameStateManager();
            this.Components.Add(gsm);
            this.Services.AddService(typeof(IGameStateService), gsm);

#if !DEBUG
            graphics.IsFullScreen = true;
            // TODO: multimonitor support goes here.
            graphics.PreferredBackBufferHeight = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;
            graphics.PreferredBackBufferWidth = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;
#endif
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();
            
            this.UpdatePowerStatus();       // Sets power settings based on battery status.
            
            var gsm = this.Services.GetService<IGameStateService>();
            this._LoaderCancelObj = new CancellationTokenSource();
            var loader = new LoadingGameState(this, this._LoaderCancelObj.Token);
            loader.LoadComplete += new EventHandler<LoadCompleteEventArgs>(this.loader_LoadComplete);
            gsm.Push(loader);
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Load only the bare minimum here.
            // The remainder of loading occurs in the background with an animation.
            // TODO: load font sizes based on screen resolution.
            this._TextFont = this.Content.Load<SpriteFont>(@"DejaVu Sans 14");
            this._SpriteBatch = new SpriteBatch(this.GraphicsDevice);

            base.LoadContent();
        }

        // Once the loader is complete, load the main game bashing part.
        private void loader_LoadComplete(object sender, LoadCompleteEventArgs e)
        {
            var gsm = this.Services.GetService<IGameStateService>();
            var loader = gsm.Pop() as LoadingGameState;
            loader.LoadComplete -= new EventHandler<LoadCompleteEventArgs>(this.loader_LoadComplete);

            if (this._LoaderCancelObj.IsCancellationRequested)
                return;
            this._LoaderCancelObj.Dispose();
            this._LoaderCancelObj = null;

            this._Timers = new FrameTimerComponent(this, this._SpriteBatch);
            this._Timers.LoadTimers = e.LoadTimers;
            this._Timers.LoadFont();

            this._CfgMgr = this.Services.GetService<IConfigurationService>();

            var mainBasher = new BashingGameState(this, e.HelpText);
            gsm.Push(mainBasher);
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            base.UnloadContent();
        }
        #endregion

        #region Global Game Logic
        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // For checking the actual time spent in Update() and Draw().
            if (this._CfgMgr != null && this._CfgMgr.Current.InDeveloperMode)
            {
                this._Timers.OnTickStart();
                this._Timers.OnUpdateStart();
            }

            this.Services.GetService<TaskManager>().InspectAndRetireRegisteredTasks();  // Checks for unhandled exceptions in background tasks and removes completed ones.

            if (this.IsActive)
            {
                this.UpdatePowerStatus();       // Sets power settings based on battery status.
#if !DEBUG
                this.EnsureMousePointerTrapped();       // trap the mouse pointer within the game.
#endif

                // Exit condition: Ctrl + Alt + Shift + F4
                if (this.IsActive && this.IsExitCondition(Keyboard.GetState()))
                    this.ExitOrTellLoaderToCancel();
                this.ExitIfLoaderCancelled();

                base.Update(gameTime);
            }

            if (this._CfgMgr != null && this._CfgMgr.Current.InDeveloperMode)
                this._Timers.OnUpdateComplete();
        }

        private bool IsExitCondition(KeyboardState keyboardState)
        {
            return
                    (keyboardState.IsKeyDown(Keys.LeftControl)  || keyboardState.IsKeyDown(Keys.RightControl))
                    && (keyboardState.IsKeyDown(Keys.LeftAlt)   || keyboardState.IsKeyDown(Keys.RightAlt))
                    && (keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift))
                    && (keyboardState.IsKeyDown(Keys.F4));
        }
        private void ExitOrTellLoaderToCancel()
        {
            if (this._LoaderCancelObj != null)
                // Tell the laoder to exit.
                this._LoaderCancelObj.Cancel();
            else
                // Or just exit right away.
                this.Exit();
        }
        private void ExitIfLoaderCancelled()
        {
            if (this._LoaderCancelObj != null && this._LoaderCancelObj.IsCancellationRequested)
            {
                // Check if the loader has finished.
                var gsm = this.Services.GetService<IGameStateService>();
                var active = gsm.ActiveState;
                if (active == null || active.GetType() != typeof(LoadingGameState))
                    // And exit!
                    this.Exit();
            }
        }
        private void EnsureMousePointerTrapped()
        {
            // TODO: multi monitor support: the mouse pointer may not be on this game's surface, but it could be on another!
            // This works unless you really try to move the pointer off the screen, in which case you can, if you're quick enough, click outside the window.
               // I'll need to hook some low level mouse events to move the mouse pointer before it is displayed to fix that.
            var m = Mouse.GetState();
            var p = new Point(m.X, m.Y);
            if (!this.GraphicsDevice.Viewport.Bounds.Contains(p))
            {
                // Find the closest edge to snap the mouse pointer to.
                var newX = p.X;
                if (p.X <= 0)
                    newX = 0;
                else if (p.X >= this.GraphicsDevice.Viewport.Width)
                    newX = this.GraphicsDevice.Viewport.Width;

                var newY = p.Y;
                if (p.Y <= 0)
                    newY = 0;
                else if (p.Y >= this.GraphicsDevice.Viewport.Height)
                    newY = this.GraphicsDevice.Viewport.Height;

                Mouse.SetPosition(newX, newY);
            }
        }


        #region Help Text
        internal StringShape GetHelpText()
        {
            var helpText = new StringShape(this);
            helpText.String = "Baby Bash XNA - A BabySmash clone using the XNA Framework\n© Murray Grant 2011\nPress  Ctrl + Alt + Shift + F4    to Exit\nPress  Ctrl + Alt + Shift + F12  for Options";
            helpText.Colour = Color.Black;
            helpText.ShadowColour = Color.White;
            helpText.ShadowOffset = 1f;
            helpText.Location = new Vector2(10);
            helpText.SpinTime = TimeSpan.Zero;
            helpText.FadeInTime = TimeSpan.FromSeconds(0.4);
            helpText.OnScreenTime = TimeSpan.FromSeconds(10);
            helpText.FadeOutTime = TimeSpan.FromSeconds(1.5);
            // TODO: load font sizes based on screen resolution.
            helpText.Font = this._TextFont;
            return helpText;
        }
        #endregion

        #region UpdatePowerStatus()
        private void UpdatePowerStatus()
        {
            if (System.Windows.Forms.SystemInformation.PowerStatus.PowerLineStatus == System.Windows.Forms.PowerLineStatus.Offline && !this._LowPowerMode)
            {
                this._LowPowerMode = true;
                this.TargetElapsedTime = new TimeSpan(TicksFor30Fps);
            }
            else if ((System.Windows.Forms.SystemInformation.PowerStatus.PowerLineStatus == System.Windows.Forms.PowerLineStatus.Online ||  System.Windows.Forms.SystemInformation.PowerStatus.PowerLineStatus == System.Windows.Forms.PowerLineStatus.Unknown) && this._LowPowerMode)
            {
                this._LowPowerMode = false;
                this.TargetElapsedTime = new TimeSpan(TicksFor60Fps);
            }
            
        }
        #endregion
        #endregion

        #region Drawing
        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            // For checking the actual time spent in Draw().
            if (this._CfgMgr != null && this._CfgMgr.Current.InDeveloperMode)
                this._Timers.OnDrawStart();

            GraphicsDevice.Clear(Color.White);

            base.Draw(gameTime);        // Draws all the other drawable components.

            // Draw the debug timers when in development mode.
            if (this._CfgMgr != null && this._CfgMgr.Current.InDeveloperMode)
            {
                this._Timers.Draw(gameTime);
                this._Timers.OnDrawComplete();
            }
        }

        protected override void EndDraw()
        {
            if (this._CfgMgr != null && this._CfgMgr.Current.InDeveloperMode)
                this._Timers.OnTickComplete();
            base.EndDraw();
        }
        #endregion

    }
}

