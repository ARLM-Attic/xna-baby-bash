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
using Nuclex.UserInterface;
using Nuclex.Input;
using Nuclex.Game.States;

namespace MurrayGrant.BabyGame
{
    /// <summary>
    /// This is a game component that implements IUpdateable.
    /// </summary>
    public class OptionsMenuGameState : Nuclex.Game.States.DrawableGameState
    {
        public GameMain Game { get; private set; }
        private GuiManager _Gui;
        private OptionsMenuDialog _Dialog;

        public OptionsMenuGameState(GameMain game)
            : base()
        {
            this.Game = game;
            
            this._Gui = new GuiManager(this.Game.Services);
            this._Gui.Screen = new Screen(this.Game.GraphicsDevice.Viewport.Width, this.Game.GraphicsDevice.Viewport.Height);
            this._Gui.Screen.Desktop.Bounds = new UniRectangle(
                new UniScalar(0.1f, 0.0f), new UniScalar(0.1f, 0.0f),
                new UniScalar(0.8f, 0.0f), new UniScalar(0.8f, 0.0f));
            this._Dialog = new OptionsMenuDialog(this.Game);
            this._Dialog.Load();
            this._Dialog.Closed += new EventHandler(this.dialog_Closed);
            this._Gui.Screen.Desktop.Children.Add(this._Dialog);

            this.Game.Components.Add(this._Gui);
        }

        private void dialog_Closed(object sender, EventArgs e)
        {
            this._Dialog.Close();
            this.Game.Components.Remove(this._Gui);
            var gsm = this.Game.Services.GetService<IGameStateService>();
            gsm.Pop();
            this._Dialog.Closed -= new EventHandler(this.dialog_Closed);
            this.Game.Services.RemoveService(typeof(IGuiService));
        }

        
        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
        }
        public override void Draw(GameTime gameTime)
        {
        }

    }
}
