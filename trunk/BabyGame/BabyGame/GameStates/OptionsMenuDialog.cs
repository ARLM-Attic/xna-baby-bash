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
using System.IO;
using Nuclex.UserInterface;
using Nuclex.UserInterface.Controls;
using Nuclex.UserInterface.Controls.Desktop;
using MurrayGrant.BabyGame.Services;

namespace MurrayGrant.BabyGame
{
    public partial class OptionsMenuDialog : WindowControl
    {
        private FileInfo _BabyPackage;
        public GameMain Game { get; set; }
        private Configuration _EditingConfiguration;

        // TODO: display the BabyPackage Author, Title, etc properties here.
        public OptionsMenuDialog(GameMain game)
            : base()
        {
            InitializeComponent();

            this.Game = game;
        }

        public event EventHandler Closed;

        public void Load()
        {
            var configMgr = this.Game.Services.GetService<IConfigurationService>();
            this._EditingConfiguration = configMgr.Current.Copy();
            this.ConfigToScreen(this._EditingConfiguration);
        }

        private void closeButton_Pressed(object sender, EventArgs e)
        {
            // Save.
            var result = this.ScreenToConfig();
            if (result.Item2.Count() == 0)
            {
                var configMgr = this.Game.Services.GetService<IConfigurationService>();
                configMgr.Save(result.Item1);

                // And Close.
                if (this.Closed != null)
                    this.Closed(this, EventArgs.Empty);
            }
            else
            {
                // Display the error message.
                this.lblErrorMessage.Text = String.Join(Environment.NewLine, result.Item2.Select((err) => err.Item2));
            }
        }

        private void undoButton_Pressed(object sender, EventArgs e)
        {
            this.Load();
        }
        private void creditsButton_Pressed(object sender, EventArgs e)
        {
            var gui = this.Game.Services.GetService<IGuiService>();
            if (!gui.Screen.Desktop.Children.Any(c => c.GetType() == typeof(AboutDialog)))
            {
                var credits = new AboutDialog(this.Game);
                gui.Screen.Desktop.Children.Add(credits);
                credits.BringToFront();
            }
            else
            {
                var credits = gui.Screen.Desktop.Children.First(c => c.GetType() == typeof(AboutDialog));
                credits.BringToFront();
            }
        }
        private void btnDisableKeys_Pressed(object sender, EventArgs e)
        {
            // TODO: have another dialog to capture any keys and list them as disabled (except ctl, alt, shift, F4, F12).
            // TODO: change the keyboard hooking class to allow capture of these keys.
            throw new NotImplementedException();
        }


        private void btnBabyPackageBrowse_Pressed(object sender, EventArgs e)
        {
            // TODO: This is very much a hack!
            // If you don't reset the GraphicsDevice before displaying the OpenFileDialog in full screen mode, 
            // the dialog steals input focus but remains invisible.
            // It doesn't seem that you need to reset again after displaying the dialog.
            var parms = new Microsoft.Xna.Framework.Graphics.PresentationParameters();
            parms.IsFullScreen = false;
            parms.DeviceWindowHandle = this.Game.Window.Handle;
            this.Game.GraphicsDevice.Reset(parms);

            var dlgBrowse = new System.Windows.Forms.OpenFileDialog();
            dlgBrowse.CheckPathExists = true;
            dlgBrowse.CheckFileExists = true;
            dlgBrowse.AddExtension = true;
            dlgBrowse.DefaultExt = Helper.BabyPackageExtension;
            dlgBrowse.Title = "Select a Baby Package File";
            dlgBrowse.Multiselect = false;
            dlgBrowse.InitialDirectory = this._BabyPackage.DirectoryName;
            dlgBrowse.Filter = "Baby Package Files (*.babypackage,*.zip)|*" + Helper.BabyPackageExtension + ";*.zip|All Files (*.*)|*.*";
            dlgBrowse.FilterIndex = 0;
            dlgBrowse.FileName = this._BabyPackage.FullName;
            if (dlgBrowse.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                // Copy the new babypackage to the config folder to ensure it doesn't mysteriously disappear (ie: be deleted from temporary internet files or a download folder).
                var cfgMgr = new ConfigurationManager(this.Game);
                var dest = new FileInfo(Path.Combine(cfgMgr.ConfigurationFile.Directory.FullName, Path.GetFileName(dlgBrowse.FileName)));
                File.Copy(dlgBrowse.FileName, dest.FullName, true);
                this._BabyPackage = dest;
                this.lblBabyPackagePath.Text = this._BabyPackage.Name;      // TODO: crack open the babypackage and put the author, title, description, website here.
            }
        }

        private void ConfigToScreen(Configuration config)
        {
            this._BabyPackage = new FileInfo(config.PathToBabyPackage);
            this.lblBabyPackagePath.Text = Path.GetFileNameWithoutExtension(this._BabyPackage.Name);
            this.txtKeyBashingThreshold.Text = config.KeyBashingThreshold.ToString();
            this.chkRunOnAllMonitors.Selected = config.DisplayOnAllScreens;
            this.chkMouseRequired.Selected = config.MouseButtonRequiredWhenMoving;
            this.chkAutoPublishErrors.Selected = config.AutoPublishException;
            this.chkDeveloperMode.Selected = config.InDeveloperMode;
            this.lblDisabledKeyCount.Text = string.Format("{0} Deactivated", config.DisabledKeys.Count);
        }
        private Tuple<Configuration, IEnumerable<Tuple<Control, string>>> ScreenToConfig()
        {
            int i;
            var errors = new List<Tuple<Control, string>>();

            var result = new Configuration();
            result.DisplayOnAllScreens = this.chkRunOnAllMonitors.Selected;
            result.PathToBabyPackage = this._BabyPackage.FullName;
            if (Int32.TryParse(this.txtKeyBashingThreshold.Text, out i) && i >= 0)
                result.KeyBashingThreshold = i;
            else
                errors.Add(new Tuple<Control, string>(this.txtKeyBashingThreshold, "Cannot read the bashing threashold. Please enter a whole number."));

            result.MouseButtonRequiredWhenMoving = this.chkMouseRequired.Selected;
            result.AutoPublishException = this.chkAutoPublishErrors.Selected;
            result.InDeveloperMode = this.chkDeveloperMode.Selected;

            return new Tuple<Configuration, IEnumerable<Tuple<Control, string>>>(result, errors);
        }

    }
}
