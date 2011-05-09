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
using Nuclex.Game.States;
using Nuclex.Input;
using MurrayGrant.BabyGame.Services;
using MurrayGrant.BabyGame.Helpers;

namespace MurrayGrant.BabyGame
{
    /// <summary>
    /// This is the main game state.
    /// </summary>
    public class BashingGameState : Nuclex.Game.States.DrawableGameState
    {
        private readonly TimeSpan MouseMoveGraceTime = TimeSpan.FromMilliseconds(500);
        private readonly TimeSpan GamepadAnalogueMoveGraceTime = TimeSpan.FromMilliseconds(500);
        
        public GameMain Game { get; private set; }
        private HashSet<Keys> _LastPressedKeys = new HashSet<Keys>();
        private Dictionary<PlayerIndex, ControllerButton> _LastPressedButtons = new Dictionary<PlayerIndex, ControllerButton>();
        private MouseState _LastMouseState;
        private TimeSpan _LastMouseMoveDetected;
        private TimeSpan _LastGamepadAnalogueMoveDetected;
        private TimeSpan _LastAction;
        private SpriteBatch _SpriteBatch;
        private AggrigateComponent _BabyShapes;
        private bool _FirstUpdate = true;
        private BabyShape _BashingShape;
        private StringShape _HelpText;

        public BashingGameState(GameMain game, StringShape helpText)
            : base()
        {
            this.Game = game;
            this._SpriteBatch = new SpriteBatch(this.Game.GraphicsDevice);
            this._HelpText = helpText;
            this._BabyShapes = new AggrigateComponent(this.Game);
            this._BabyShapes.Components.Add(helpText);
            this.Game.Components.Add(this._BabyShapes);

            // Set the initial mouse and keyboard states so shapes don't appear for no reason.
            this._LastMouseState = Mouse.GetState();
            this.UpdateLastKeysPressed(Keyboard.GetState());

            // Set the last pressed controller buttons to zero.
            for (var player = PlayerIndex.One; player <= PlayerIndex.Four; player++)
                this._LastPressedButtons.Add(player, ControllerButton.None);
        }


        #region Update
        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            // Get services.
            var babyPackpageProvider = this.Game.Services.GetService<IBabyPackageProvider>();
            babyPackpageProvider.CurrentGameTime = gameTime; 
            var config = this.Game.Services.GetService<IConfigurationService>().Current;
            var soundService = this.Game.Services.GetService<ISoundService>().SoundService;
            soundService.RemoveNonPlayingSounds();  // Remove any sounds that have finished playing to allow new ones to play.

            // Open options menu.
            if (this.IsOptionsMenuCondition(Keyboard.GetState()) && !this.Paused)
            {
                var gsm = this.Game.Services.GetService<IGameStateService>();
                gsm.Push(new OptionsMenuGameState(this.Game), GameStateModality.Popup);
            }

            if (this._FirstUpdate)
                this.DoFirstUpdate(gameTime, config, babyPackpageProvider, soundService);

            if (this.IsGenerateDebugException(Keyboard.GetState()))     // This is outside the normal keyboard handling because it needs so many keys pressed together it is almost guarenteed to trigger keybashing.
                throw new TestException("Throwing a DEBUG exception.");

            // A lockout applies for bashing keys to make the point more firmly.
            if (!this.BashingLockedOutInput(gameTime) && !this.Paused)
            {
                this.DoKeyboardHandling(gameTime, config, babyPackpageProvider, soundService);
                
                this.DoMouseHandling(gameTime, config, babyPackpageProvider, soundService);

                this.DoGamepadHandling(gameTime, config, babyPackpageProvider, soundService);
            }


            this.DoIdleHandling(gameTime, config, babyPackpageProvider, soundService);

            this.DoEndOfLoop(gameTime, config, babyPackpageProvider, soundService);
        }

        #region DoFirstUpdate
        protected virtual void DoFirstUpdate(GameTime gameTime, Configuration config, IBabyPackageProvider babyPackage, SoundService sound)
        {
            // Play the startup sound.
            this.HandleShapeAndSound(sound, babyPackage.Startup(), SoundAction.Analogue);

            if (!this._BabyShapes.Components.Contains(this._HelpText))
                // Display the package details if the help message has gone.
                this.BabyShapes_HelpTextComponentRemoved(this, new GameComponentCollectionEventArgs(null));
            else
                // Wait for the help text to be removed.
                this._BabyShapes.Components.ComponentRemoved += new EventHandler<GameComponentCollectionEventArgs>(BabyShapes_HelpTextComponentRemoved);                
        }
        #endregion
        #region DoKeyboardHandling
        protected virtual void DoKeyboardHandling(GameTime gameTime, Configuration config, IBabyPackageProvider babyPackage, SoundService sound)
        {
            // Keyboard handling.
            var keyboardState = Keyboard.GetState();

            // Idle timeout reset.
            if (keyboardState.GetPressedKeys().Length > 0)
                this._LastAction = gameTime.TotalGameTime;

            if (!this.IsBashingKeys(keyboardState))
            {
                // Add characters pressed to be drawn.
                foreach (var key in this.GetKeyPresses(keyboardState))
                {
                    var keyResult = babyPackage.MapKeyPress(key);
                    this.HandleShapeAndSound(sound, keyResult, SoundAction.Button);
                }
            }
            else
            {
                // Bashing detected, display something different and clear all other shapes / letters on screen.
                var bashingAction = babyPackage.KeyBashingDetected();
                if (bashingAction != null)
                {
                    foreach (var element in this._BabyShapes.Components.OfType<BabyShape>().ToArray())
                        this._BabyShapes.Components.Remove(element);

                    sound.StopPlayingAllSounds();
                    this.HandleShapeAndSound(sound, bashingAction, SoundAction.ForceButton);

                    this._BashingShape = bashingAction.Shape;
                    this._BabyShapes.Components.ComponentRemoved += new EventHandler<GameComponentCollectionEventArgs>(BabyShapes_BashingComponentRemoved);
                }

            }
        }
        #endregion
        #region DoMouseHandling
        protected virtual void DoMouseHandling(GameTime gameTime, Configuration config, IBabyPackageProvider babyPackage, SoundService sound)
        {
            var mouseState = Mouse.GetState();
            var mouseButtons = mouseState.GetPressedButtons();

            // Mouse movement.
            var mouseMoveResult = babyPackage.DoMouseMovement(mouseState.X, mouseState.Y, this._LastMouseState.X, this._LastMouseState.Y, mouseState.AnyButtonPressed());
            if (mouseMoveResult != null)
            {
                this._LastAction = this._LastMouseMoveDetected = gameTime.TotalGameTime;

                // Add the shape for the mouse cursor.
                if (mouseMoveResult.Shape != null && !this._BabyShapes.Components.Contains(mouseMoveResult.Shape))
                    this._BabyShapes.Components.Add(mouseMoveResult.Shape);

                if (mouseMoveResult.Sound != null && sound.LongSoundPlayingState == SoundState.Stopped)
                    // Play mouse sound while the mouse is moving.
                    sound.TryPlaySoundLong(mouseMoveResult.Sound, LongSoundOwner.MouseMovement);
            }
            else if (mouseMoveResult == null || mouseMoveResult.Sound == null)
            {
                // Stop the sound after a short grace time.
                if (gameTime.TotalGameTime > this._LastMouseMoveDetected.Add(MouseMoveGraceTime)
                       && sound.LongSoundPlayingState == SoundState.Playing && sound.LongSoundOwner == LongSoundOwner.MouseMovement)
                    sound.StopPlayingLongSound();
            }

            // Mouse buttons.
            foreach (var button in this.GetMouseButtonPresses(mouseButtons))
            {
                var buttonResult = babyPackage.MapMouseButton(button, mouseState.X, mouseState.Y);
                this.HandleShapeAndSound(sound, buttonResult, SoundAction.Button);
            }

            // Mouse wheel.
            var mouseWheelResult = babyPackage.DoMouseWheelMovement(mouseState.ScrollWheelValue, this._LastMouseState.ScrollWheelValue, mouseState.AnyButtonPressed());
            this.HandleShapeAndSound(sound, mouseWheelResult, SoundAction.Button);
        }
        #endregion
        #region DoGamepadHandling
        protected virtual void DoGamepadHandling(GameTime gameTime, Configuration config, IBabyPackageProvider babyPackage, SoundService sound)
        {
            // Gamepads.
            var buttonPresses = this.GetButtonPresses();
            if (buttonPresses.Any())
                this._LastAction = gameTime.TotalGameTime;
            
            // Add buttons pressed to be drawn.
            foreach (var buttonAndPlayer in buttonPresses)
            {
                var buttonResult = babyPackage.MapControllerButton(buttonAndPlayer.Button, buttonAndPlayer.Player);
                this.HandleShapeAndSound(sound, buttonResult, SoundAction.Button);
            }

            // Deal with analogue inputs.
            for (PlayerIndex player = PlayerIndex.One; player <= PlayerIndex.Four; player++)
            {
                if (GamePad.GetState(player).IsConnected)
                {
                    var analogueInputs = new ControllerAnalogue(GamePad.GetState(player), ControllerReversal.LeftThumb);
                    if (!analogueInputs.IsZero)
                    {
                        this._LastAction = gameTime.TotalGameTime;
                        this._LastGamepadAnalogueMoveDetected = gameTime.TotalGameTime;
                    }

                    var analogueResult = babyPackage.DoControllerAnalogue(analogueInputs, player);
                    if (analogueResult != null && analogueResult.Any())
                    {
                        foreach (var action in analogueResult)
                        {
                            // Add the shape.
                            if (action.Shape != null && !this._BabyShapes.Components.Contains(action.Shape))
                                this._BabyShapes.Components.Add(action.Shape);

                            if (action.Sound != null && sound.LongSoundPlayingState == SoundState.Stopped)
                                // Play sound while analogue controls are being used.
                                sound.TryPlaySoundLong(action.Sound, LongSoundOwner.GamePadAnalogueInputs);
                        }
                    }

                    if (analogueResult == null || !analogueResult.Any() || analogueResult.All(a => a.Sound == null))
                    {
                        // No analogue action: stop playing sound after short delay.
                        if (gameTime.TotalGameTime > this._LastGamepadAnalogueMoveDetected.Add(GamepadAnalogueMoveGraceTime)
                               && sound.LongSoundPlayingState == SoundState.Playing && sound.LongSoundOwner == LongSoundOwner.GamePadAnalogueInputs)
                            sound.StopPlayingLongSound();
                    }
                }
            }

        }
        #endregion
        #region DoIdleHandling
        protected virtual void DoIdleHandling(GameTime gameTime, Configuration config, IBabyPackageProvider babyPackage, SoundService sound)
        {
            // Idle timeout.
            if (gameTime.TotalGameTime > this._LastAction.Add(babyPackage.IdleTimeout))
            {
                // Get the idle timeout action.
                var timeoutAction = babyPackage.DoIdleTimeout(this._LastAction);
                if (timeoutAction != null)
                {
                    if (timeoutAction.Shape != null)
                        this._BabyShapes.Components.Add(timeoutAction.Shape);
                    if (timeoutAction.Sound != null)
                        sound.TryPlaySoundLong(timeoutAction.Sound, LongSoundOwner.Idle);
                }

                // Display the 'about' notice again (so parents know how to exit!).
                this._BabyShapes.Components.Add(this.Game.GetHelpText());

                this._LastAction = gameTime.TotalGameTime;      // Reset the timeout.
            }
        }
        #endregion
        #region DoEndOfLoop
        protected virtual void DoEndOfLoop(GameTime gameTime, Configuration config, IBabyPackageProvider babyPackage, SoundService sound)
        {
            // Save key / button state pressed for the next game update.
            this.UpdateLastKeysPressed(Keyboard.GetState());
            this.UpdateLastButtonsPressed();
            this._LastMouseState = Mouse.GetState();
            this._FirstUpdate = false;
        }
        #endregion

        #region Helpers
        private bool IsOptionsMenuCondition(KeyboardState keyboardState)
        {
            return
                    (keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl))
                    && (keyboardState.IsKeyDown(Keys.LeftAlt) || keyboardState.IsKeyDown(Keys.RightAlt))
                    && (keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift))
                    && (keyboardState.IsKeyDown(Keys.F12));

        }

        private bool IsBashingKeys(KeyboardState keyboardState)
        {
            var config = this.Game.Services.GetService<IConfigurationService>().Current;
            // Ignore control, alt and shift for key bashing (because it's looks ugly when you're trying to exit!).
            return
                (config.KeyBashingThreshold > 0 &&
                keyboardState.GetPressedKeys().Where(
                    k =>
                           k != Keys.LeftAlt && k != Keys.LeftControl && k != Keys.LeftShift
                        && k != Keys.RightAlt && k != Keys.RightControl && k != Keys.RightShift)
                .Count() >= config.KeyBashingThreshold);
        }

        private bool IsGenerateDebugException(KeyboardState keyboardState)
        {
            var config = this.Game.Services.GetService<IConfigurationService>().Current;
            
            return config.InDeveloperMode &&
                    (keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl))
                    && (keyboardState.IsKeyDown(Keys.LeftAlt) || keyboardState.IsKeyDown(Keys.RightAlt))
                    && (keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift))
                    && (keyboardState.IsKeyDown(Keys.Back) 
                    &&  keyboardState.IsKeyDown(Keys.T));
        }

        private bool BashingLockedOutInput(GameTime gameTime)
        {
            // Bashing locks out input until the shape has disappeared.
            return (this._BashingShape != null && !this._BashingShape.AtEndOfLife);
        }

        private IEnumerable<Keys> GetKeyPresses(KeyboardState keyboardState)
        {
            var result = new List<Keys>();

            // Get all the keys pressed since the last game update.
            foreach (var key in keyboardState.GetPressedKeys().Where(k => !this._LastPressedKeys.Contains(k)))
                result.Add(key);

            return result;
        }
        private IEnumerable<MouseButton> GetMouseButtonPresses(IEnumerable<MouseButton> mouseButtonPresses)
        {
            var result = new List<MouseButton>();

            // Get all the buttons pressed since the last game update.
            foreach (var button in mouseButtonPresses.Where(k => !this._LastMouseState.GetPressedButtons().Contains(k)))
                result.Add(button);

            return result;
        }
        private IEnumerable<ControllerButtonAndPlayer> GetButtonPresses()
        {
            var result = new List<ControllerButtonAndPlayer>();

            // Get all the buttons pressed since the last game update.
            for (var player = PlayerIndex.One; player <= PlayerIndex.Four; player++)
            {
                var gps = GamePad.GetState(player);
                if (gps.IsConnected)
                {
                    // Tricky bitwise operations to remove any buttons that were pressed last frame.
                    var buttonPresses = gps.GetPressedButtons();
                    var newPresses = (ControllerButton)((buttonPresses ^ this._LastPressedButtons[player]) & buttonPresses);

                    foreach (var button in newPresses.GetAllSelectedItems<ControllerButton>().Where(b => b != ControllerButton.None))
                        result.Add(new ControllerButtonAndPlayer(button, player));
                }
            }

            return result;
        }

        private enum SoundAction
        {
            Button,
            Analogue,
            ForceButton,
            ForceAnalogue
        }
        private void HandleShapeAndSound(SoundService soundService, ShapeAndSoundTuple shapeAndSound, SoundAction soundAction, LongSoundOwner longSoundOwner)
        {
            if (shapeAndSound != null)
            {
                if (shapeAndSound.Shape != null && !this._BabyShapes.Components.Contains(shapeAndSound.Shape))
                    this._BabyShapes.Components.Add(shapeAndSound.Shape);
                if (shapeAndSound.Sound != null)
                {
                    if (soundAction == SoundAction.Button)
                        soundService.TryPlaySoundButton(shapeAndSound.Sound);
                    else if (soundAction == SoundAction.Analogue)
                        soundService.TryPlaySoundLong(shapeAndSound.Sound, longSoundOwner);
                    else if (soundAction == SoundAction.ForceButton)
                        soundService.ForcePlaySoundButton(shapeAndSound.Sound);
                    else if (soundAction == SoundAction.ForceAnalogue)
                        soundService.TryPlaySoundLong(shapeAndSound.Sound, longSoundOwner);
                    else
                        throw new ApplicationException("Forgot a soundType.");
                }
            }
        }
        private void HandleShapeAndSound(SoundService soundService, ShapeAndSoundTuple shapeAndSound, SoundAction soundAction)
        {
            this.HandleShapeAndSound(soundService, shapeAndSound, soundAction, LongSoundOwner.None);
        }

        private void UpdateLastKeysPressed(KeyboardState keyboardState)
        {
            // Refresh keys pressed from last game update.
            this._LastPressedKeys.Clear();
            foreach (var key in keyboardState.GetPressedKeys())
                this._LastPressedKeys.Add(key);
        }
        private void UpdateLastButtonsPressed()
        {
            // Refresh buttons pressed from last game update.
            for (var player = PlayerIndex.One; player < PlayerIndex.Four; player++)
            {
                var gps = GamePad.GetState(player);
                if (gps.IsConnected)
                    this._LastPressedButtons[player] = gps.GetPressedButtons();
            }
        }
        private void BabyShapes_BashingComponentRemoved(object sender, GameComponentCollectionEventArgs e)
        {
            // Clean up the bashing shape once it's lifespan is over.
            if (Object.ReferenceEquals(this._BashingShape, e.GameComponent))
            {
                this._BashingShape = null;
                this._BabyShapes.Components.ComponentRemoved -= new EventHandler<GameComponentCollectionEventArgs>(BabyShapes_BashingComponentRemoved);
            }
        }
        private void BabyShapes_HelpTextComponentRemoved(object sender, GameComponentCollectionEventArgs e)
        {
            // Clean up the helptext shape once it's lifespan is over.
            if (Object.ReferenceEquals(this._HelpText, e.GameComponent))
            {
                this._HelpText = null;
                this._BabyShapes.Components.ComponentRemoved -= new EventHandler<GameComponentCollectionEventArgs>(BabyShapes_HelpTextComponentRemoved);
            }

            // And add a message about the babypackage.
            var packageMessage = this.CreatePackageDetailsMessage(this.Game.Services.GetService<IBabyPackageProvider>());
            if (packageMessage != null)
                this._BabyShapes.Components.Add(packageMessage);
        }

        protected virtual StringShape CreatePackageDetailsMessage(IBabyPackageProvider babyPackage)
        {
            if (String.IsNullOrEmpty(babyPackage.Author)
                    && String.IsNullOrEmpty(babyPackage.Title)
                    && babyPackage.Website == null)
                return null;

            var website = String.Empty;
            if (babyPackage.Website != null)
                website = babyPackage.Website.ToString().TrimEnd('/').Replace(babyPackage.Website.Scheme + "://", String.Empty);
            var helpText = new StringShape(this.Game);
            helpText.String = String.Format("{0}\n{1}\n{2}", babyPackage.Title, String.IsNullOrEmpty(babyPackage.Author) ? String.Empty : "By " + babyPackage.Author, website);
            helpText.Colour = Color.Black;
            helpText.ShadowColour = Color.White;
            helpText.ShadowOffset = 1f;
            helpText.Location = new Vector2(10);
            helpText.SpinTime = TimeSpan.Zero;
            helpText.FadeInTime = TimeSpan.FromSeconds(0.4);
            helpText.OnScreenTime = TimeSpan.FromSeconds(10);
            helpText.FadeOutTime = TimeSpan.FromSeconds(1.5);
            // TODO: load font sizes based on screen resolution and from a pool of fonts so I'm not newing one up here.
            helpText.Font = this.Game.Content.Load<SpriteFont>(@"DejaVu Sans 14");
            return helpText;
        }
        #endregion
        #endregion

        #region Draw
        public override void Draw(GameTime gameTime)
        {
            // Wait for the loader to complete before attempting to get backgrounds, etc.
            var babyPackpageProvider = this.Game.Services.GetService<IBabyPackageProvider>();

            this.Game.GraphicsDevice.Clear(babyPackpageProvider.GetBackgroundColour());

            // Show the background image.
            var background = babyPackpageProvider.GetBackground();
            if (background != null)
            {
                // Resize to fit to the screen.
                var resized = Vector2Helper.ResizeKeepingAspectRatio(this.Game.GraphicsDevice.Viewport.Bounds, background.Bounds);
                Rectangle r;
                if (resized.X == this.Game.GraphicsDevice.Viewport.Bounds.Width)
                    r = new Rectangle(0, (int)((this.Game.GraphicsDevice.Viewport.Bounds.Height - resized.Y) / 2), (int)resized.X, (int)resized.Y);
                else
                    r = new Rectangle((int)((this.Game.GraphicsDevice.Viewport.Bounds.Width - resized.X) / 2), 0, (int)resized.X, (int)resized.Y);
                this._SpriteBatch.Begin();
                this._SpriteBatch.Draw(background, r, Color.White);
                this._SpriteBatch.End();
            }
        }
        #endregion
    }
}
