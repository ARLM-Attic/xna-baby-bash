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
using System.Windows.Forms;

namespace MurrayGrant.BabyGame
{
    public class MultiMonitorGraphicsDeviceManager : GraphicsDeviceManager
    {
        public Screen Monitor { get; set; }

        public MultiMonitorGraphicsDeviceManager(Game game, Screen monitor)
            : base(game)
        {
            this.Monitor = monitor;
        }
 
        protected override void OnPreparingDeviceSettings(object sender, PreparingDeviceSettingsEventArgs args)
        {
            base.OnPreparingDeviceSettings(sender, args);
        }
 
        protected override void RankDevices(List<GraphicsDeviceInformation> foundDevices)
        {
            if (this.Monitor != null)
                foundDevices.RemoveAll(di => !di.Adapter.DeviceName.Contains(this.Monitor.DeviceName));
        }
    }
}
