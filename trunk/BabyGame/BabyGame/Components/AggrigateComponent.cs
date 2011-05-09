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


namespace MurrayGrant.BabyGame
{
    /// <summary>
    /// A collection of many drawable components which are drawn in one sprite batch.
    /// </summary>
    public class AggrigateComponent : Nuclex.Game.DrawableComponent, IAggrigateComponentService
    {
        public GameMain Game { get; private set; }
        private SpriteBatch SpriteBatch { get; set; } 
        public GameComponentCollection Components { get; private set; }
        public AggrigateComponent Aggrigate { get { return this; } }

        public AggrigateComponent(GameMain game)
            : base()
        {
            this.Game = game;
            this.SpriteBatch = new SpriteBatch(this.Game.GraphicsDevice);
            this.Components = new GameComponentCollection();
            this.Components.ComponentAdded += new EventHandler<GameComponentCollectionEventArgs>(this.ComponentAdded);
            this.Components.ComponentRemoved += new EventHandler<GameComponentCollectionEventArgs>(this.ComponentRemoved);
        }

        private void ComponentAdded(object sender, GameComponentCollectionEventArgs e)
        {
            var ssb = e.GameComponent as ISharedSpriteBatchAndLifeCycle;
            if (ssb != null)
                ssb.SpriteBatch = this.SpriteBatch;
        }
        private void ComponentRemoved(object sender, GameComponentCollectionEventArgs e)
        {
            var ssb = e.GameComponent as ISharedSpriteBatchAndLifeCycle;
            if (ssb != null)
                ssb.SpriteBatch = null;
        }

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            // Remove components at the end of their life.
            foreach (var c in this.Components.OfType<ISharedSpriteBatchAndLifeCycle>().Where(comp => comp.AtEndOfLife).ToArray())
                this.Components.Remove(c as IGameComponent);

            foreach (var c in this.Components.OfType<IUpdateable>().Where(comp => comp.Enabled).OrderBy(comp => comp.UpdateOrder))
                c.Update(gameTime);

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            this.SpriteBatch.Begin();

            foreach (var c in this.Components.OfType<IDrawable>().Where(comp => comp.Visible).OrderBy(comp => comp.DrawOrder))
                c.Draw(gameTime);

            this.SpriteBatch.End();
            base.Draw(gameTime);
        }
    }

    public interface ISharedSpriteBatchAndLifeCycle
    {
        SpriteBatch SpriteBatch { get; set; }
        bool AtEndOfLife { get; }
    }
    public interface IAggrigateComponentService
    {
        AggrigateComponent Aggrigate { get; }
    }
}
