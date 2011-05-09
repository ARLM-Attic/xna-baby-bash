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
using Microsoft.Xna.Framework.Graphics;


namespace MurrayGrant.BabyGame.Components
{
    public class FrameTimerComponent
        : DrawableGameComponent
    {
        private SpriteBatch _SpriteBatch;
        private System.Diagnostics.Stopwatch _TickStopwatch = new System.Diagnostics.Stopwatch();
        private System.Diagnostics.Stopwatch _UpdateStopwatch = new System.Diagnostics.Stopwatch();
        private System.Diagnostics.Stopwatch _DrawStopwatch = new System.Diagnostics.Stopwatch();
        public TimeSpan LastTickTime { get; private set; }
        public TimeSpan LastUpdateTime { get; private set; }
        public TimeSpan LastDrawTime { get; private set; }
        private SpriteFont _DebugFont;

        public LoadTimers LoadTimers { get; set; }

        public FrameTimerComponent(Game g, SpriteBatch sb) : base(g)
        {
            this._SpriteBatch = sb;
        }

        protected override void LoadContent()
        {
            this.LoadFont();
        }
        public void LoadFont()
        {
            // TOOD: load fonts based on screen size.
            if (this._DebugFont == null)
                this._DebugFont = this.Game.Content.Load<SpriteFont>(@"DejaVu Sans 10");
        }

        public void OnUpdateStart()
        {
            this._UpdateStopwatch.Restart();
        }
        public void OnUpdateComplete()
        {
            this._UpdateStopwatch.Stop();
            this.LastUpdateTime = this._UpdateStopwatch.Elapsed;
        }
        public void OnTickStart()
        {
            this._TickStopwatch.Restart();
        }
        public void OnTickComplete()
        {
            this._TickStopwatch.Stop();
            this.LastTickTime = this._TickStopwatch.Elapsed;
        }
        public void OnDrawStart()
        {
            this._DrawStopwatch.Restart();
        }
        public void OnDrawComplete()
        {
            this._DrawStopwatch.Stop();
            this.LastDrawTime = this._DrawStopwatch.Elapsed;
        }

        public override void Draw(GameTime gameTime)
        {
            this._SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            var fps = (float)(1 / gameTime.ElapsedGameTime.TotalSeconds);
            var updateCalcPercent = this.LastUpdateTime.TotalSeconds / this.Game.TargetElapsedTime.TotalSeconds;
            var drawCalcPercent = this.LastDrawTime.TotalSeconds / this.Game.TargetElapsedTime.TotalSeconds;
            var tickCalcPercent = this.LastTickTime.TotalSeconds / this.Game.TargetElapsedTime.TotalSeconds;
            int babyShapes = 0;
            var a = this.Game.Components.OfType<AggrigateComponent>();
            if (a.Any())
                babyShapes = a.First().Components.Count;
            var s = string.Format("{0:#0.00} fps\nTime in Update(): {1:F2}ms ({2:P2})\nTime in Draw(): {3:F2}ms ({4:P2})\nTotal Tick Estimate(): {5:F2} ({6:P2})\nTotal Load Time: {7:F2}ms, Cfg Load Time: {8:F2}ms, Pkg Load Time: {9:F2}ms\nComponents: {10}, BabyShapes: {11}"
                                    , fps, this.LastUpdateTime.TotalMilliseconds, updateCalcPercent, this.LastDrawTime.TotalMilliseconds, drawCalcPercent, this.LastTickTime.TotalMilliseconds, tickCalcPercent, this.LoadTimers.TotalLoadTime.TotalMilliseconds, this.LoadTimers.ConfigLoadTime.TotalMilliseconds, this.LoadTimers.BabyPackageLoadTime.TotalMilliseconds, this.Game.Components.Count, babyShapes);

            this._SpriteBatch.DrawString(this._DebugFont, s, Vector2.One, Color.Red);
            this._SpriteBatch.End();
            
        }
    }
}
