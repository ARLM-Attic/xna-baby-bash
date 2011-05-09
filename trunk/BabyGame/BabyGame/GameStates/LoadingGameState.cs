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
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using MurrayGrant.BabyGame;
using MurrayGrant.BabyGame.Services;

namespace MurrayGrant.BabyGame
{
    /// <summary>
    /// This is a game component that implements IUpdateable.
    /// </summary>
    public class LoadingGameState : Nuclex.Game.States.DrawableGameState
    {
        public GameMain Game { get; set; }
        private Task _Loader;
        private AnimatedSprite _LoaderAnimation;
        private StringShape _HelpText;
        private SpriteBatch _SpriteBatch;

        public event EventHandler<LoadCompleteEventArgs> LoadComplete;

        public LoadingGameState(GameMain game, CancellationToken cancelMarker)
            : base()
        {
            this.Game = game;
            var taskMgr = this.Game.Services.GetService<TaskManager>();

            this._SpriteBatch = new SpriteBatch(this.Game.GraphicsDevice);

            // Display help text at the start.
            this._HelpText = this.Game.GetHelpText();
            this._HelpText.SpriteBatch = this._SpriteBatch;

            // Show an animation while loading.
            this._LoaderAnimation = this.GetLoadingAnimation();
            this._LoaderAnimation.SpriteBatch = this._SpriteBatch;

            var loadTimers = new LoadTimers();

            // Create loader thread.
            // This is executed in the first call to Update().
            this._Loader = new Task(() =>
            {
                var loadStopwatch = System.Diagnostics.Stopwatch.StartNew();

                if (cancelMarker.IsCancellationRequested)
                    cancelMarker.ThrowIfCancellationRequested();

                // Create services.
                this.Game.Services.AddService(typeof(IConfigurationService), new ConfigurationManager(this.Game));
                this.Game.Services.AddService(typeof(IBabyPackageProvider), new XmlFolderBabyPackage(this.Game));
                this.Game.Services.AddService(typeof(ISoundService), new SoundServiceContainer(new SoundService(this.Game)));
                this.Game.Services.AddService(typeof(IApplicationUpdater), new ApplicationUpdater(this.Game));

                if (cancelMarker.IsCancellationRequested)
                    cancelMarker.ThrowIfCancellationRequested();

                // Load content based on those services.
                var startConfigLoad = loadStopwatch.Elapsed;
                var configMgr = this.Game.Services.GetService<IConfigurationService>();
                bool configExists = configMgr.Exists;
                configMgr.Load();       
                if (!configExists)       // Ensure the configuration file is saved.
                    configMgr.Save(configMgr.Current);
                loadTimers.ConfigLoadTime = loadStopwatch.Elapsed.Subtract(startConfigLoad);

                if (cancelMarker.IsCancellationRequested)
                    cancelMarker.ThrowIfCancellationRequested();

                // Load up the baby package.
                var startBabyPackageLoad = loadStopwatch.Elapsed;
                var babyPackpageProvider = this.Game.Services.GetService(typeof(IBabyPackageProvider)) as IBabyPackageProvider;
                babyPackpageProvider.Load(configMgr.Current.PathToBabyPackage, cancelMarker);
                loadTimers.BabyPackageLoadTime = loadStopwatch.Elapsed;

                if (cancelMarker.IsCancellationRequested)
                    cancelMarker.ThrowIfCancellationRequested();

                // Loading can generate lots of garbage, so do a collection while the loading animation is still on screen.
                GC.Collect();

                loadStopwatch.Stop();
                loadTimers.TotalLoadTime = loadStopwatch.Elapsed;
            });
            
            var loaderComplete = this._Loader.ContinueWith((t) =>
            {                
                // When loading is finished, null it and remove the loading animation.
                this._Loader = null;

                // Notify that we've finished loading.
                if (this.LoadComplete != null)
                {
                    this._HelpText.SpriteBatch = null;
                    this.LoadComplete(this, new LoadCompleteEventArgs(this._HelpText, loadTimers));
                }

                // Load up a few assemblies from disk to keep the options screen responsive.
                var optionsLoader = new Task(() =>
                    {
                        var optionsScreen = new OptionsMenuDialog(this.Game);
                        var guiManager = new Nuclex.UserInterface.GuiManager(this.Game.Services);
                        this.Game.Services.RemoveService(typeof(Nuclex.UserInterface.IGuiService));
                        var fileBrowseDialog = new System.Windows.Forms.OpenFileDialog();
                    });
                taskMgr.RegisterTask(optionsLoader);
                optionsLoader.Start();
            }, CancellationToken.None, TaskContinuationOptions.NotOnFaulted, TaskScheduler.FromCurrentSynchronizationContext());

            var updaterTask = loaderComplete.ContinueWith(t =>
            {
                // Spin up the updater (which delays a bit before running).
                var updater = this.Game.Services.GetService<IApplicationUpdater>();
                if (updater.SupportsUpdates)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(30));
                    updater.DoCheckAndUpdate();
                }
            }, TaskContinuationOptions.NotOnFaulted);

            // Register all the tasks with the game to handle exceptions.
            taskMgr.RegisterTask(this._Loader);
            taskMgr.RegisterTask(loaderComplete);
            taskMgr.RegisterTask(updaterTask);
        }

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            // Start loading on background thread.
            if (this._Loader.Status == TaskStatus.Created)
                this._Loader.Start();

            this._HelpText.Update(gameTime);
            this._LoaderAnimation.Update(gameTime);
        }

        internal AnimatedSprite GetLoadingAnimation()
        {
            var loaderAnimation = new AnimatedSprite(this.Game);
            loaderAnimation.Animation = this.Game.Content.Load<Texture2D>(@"Loader");
            loaderAnimation.DrawOrder = 1;
            loaderAnimation.FrameSize = new Vector2(64, 64);
            loaderAnimation.FramesPerSecond = 8;
            loaderAnimation.Position = new Vector2((this.Game.GraphicsDevice.Viewport.Bounds.Width - loaderAnimation.FrameSize.X) / 2
                                                 , (this.Game.GraphicsDevice.Viewport.Bounds.Height - loaderAnimation.FrameSize.Y) / 2);
            return loaderAnimation;
        }

        public override void Draw(GameTime gameTime)
        {
            this._SpriteBatch.Begin();
            this._HelpText.Draw(gameTime);
            this._LoaderAnimation.Draw(gameTime);
            this._SpriteBatch.End();
        }
    }

    public class LoadCompleteEventArgs : EventArgs
    {
        public StringShape HelpText { get; private set; }
        public LoadTimers LoadTimers { get; private set; }

        internal LoadCompleteEventArgs(StringShape helpText, LoadTimers loadTimers)
        {
            this.LoadTimers = loadTimers;
            this.HelpText = helpText;
        }
    }

    public class LoadTimers
    {
        public TimeSpan ConfigLoadTime { get; set; }
        public TimeSpan BabyPackageLoadTime { get; set; }
        public TimeSpan TotalLoadTime { get; set; }
        public TimeSpan OverheadLoadTime { get { return this.TotalLoadTime.Subtract(this.ConfigLoadTime.Add(this.BabyPackageLoadTime)); } }
    }
}
