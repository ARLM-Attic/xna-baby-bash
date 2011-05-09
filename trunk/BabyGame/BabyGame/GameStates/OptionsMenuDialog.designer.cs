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
using Nuclex.UserInterface;
using Nuclex.UserInterface.Controls.Desktop;

namespace MurrayGrant.BabyGame
{
    partial class OptionsMenuDialog
    {
        private Nuclex.UserInterface.Controls.LabelControl lblBabyPackage;
        private Nuclex.UserInterface.Controls.LabelControl lblBabyPackagePath;
        private Nuclex.UserInterface.Controls.Desktop.ButtonControl btnBabyPackageBrowse;

        private Nuclex.UserInterface.Controls.LabelControl lblKeyBashingThreshold;
        private Nuclex.UserInterface.Controls.Desktop.InputControl txtKeyBashingThreshold;

        private Nuclex.UserInterface.Controls.LabelControl lblRunOnAllMonitors;
        private Nuclex.UserInterface.Controls.Desktop.OptionControl chkRunOnAllMonitors;

        private Nuclex.UserInterface.Controls.LabelControl lblMouseRequired;
        private Nuclex.UserInterface.Controls.Desktop.OptionControl chkMouseRequired;

        private Nuclex.UserInterface.Controls.LabelControl lblAutoPublishErrors;
        private Nuclex.UserInterface.Controls.Desktop.OptionControl chkAutoPublishErrors;

        private Nuclex.UserInterface.Controls.LabelControl lblDeveloperMode;
        private Nuclex.UserInterface.Controls.Desktop.OptionControl chkDeveloperMode;

        private Nuclex.UserInterface.Controls.LabelControl lblDisabledKeys;
        private Nuclex.UserInterface.Controls.LabelControl lblDisabledKeyCount;
        private Nuclex.UserInterface.Controls.Desktop.ButtonControl btnDisableKeys;

        private Nuclex.UserInterface.Controls.LabelControl lblErrorMessage;

        private Nuclex.UserInterface.Controls.Desktop.ButtonControl btnUndo;
        private Nuclex.UserInterface.Controls.Desktop.ButtonControl btnClose;
        private Nuclex.UserInterface.Controls.Desktop.ButtonControl btnAbout;

        
        #region NOT Component Designer generated code

        /// <summary> 
        ///   Required method for user interface initialization -
        ///   do modify the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblBabyPackage = new Nuclex.UserInterface.Controls.LabelControl();
            this.lblBabyPackagePath = new Nuclex.UserInterface.Controls.LabelControl();
            this.btnBabyPackageBrowse = new Nuclex.UserInterface.Controls.Desktop.ButtonControl();

            this.lblKeyBashingThreshold = new Nuclex.UserInterface.Controls.LabelControl();
            this.txtKeyBashingThreshold = new Nuclex.UserInterface.Controls.Desktop.InputControl();

            this.lblRunOnAllMonitors = new Nuclex.UserInterface.Controls.LabelControl();
            this.chkRunOnAllMonitors = new Nuclex.UserInterface.Controls.Desktop.OptionControl();

            this.lblMouseRequired = new Nuclex.UserInterface.Controls.LabelControl();
            this.chkMouseRequired = new Nuclex.UserInterface.Controls.Desktop.OptionControl();

            this.lblAutoPublishErrors = new Nuclex.UserInterface.Controls.LabelControl();
            this.chkAutoPublishErrors = new Nuclex.UserInterface.Controls.Desktop.OptionControl();

            this.lblDeveloperMode = new Nuclex.UserInterface.Controls.LabelControl();
            this.chkDeveloperMode = new Nuclex.UserInterface.Controls.Desktop.OptionControl();

            this.lblDisabledKeys = new Nuclex.UserInterface.Controls.LabelControl();
            this.lblDisabledKeyCount = new Nuclex.UserInterface.Controls.LabelControl();
            this.btnDisableKeys = new Nuclex.UserInterface.Controls.Desktop.ButtonControl();

            this.lblErrorMessage = new Nuclex.UserInterface.Controls.LabelControl();


            this.btnUndo = new Nuclex.UserInterface.Controls.Desktop.ButtonControl();
            this.btnClose = new Nuclex.UserInterface.Controls.Desktop.ButtonControl();
            this.btnAbout = new Nuclex.UserInterface.Controls.Desktop.ButtonControl();

            //
            // lblBabyPackage
            //
            this.lblBabyPackage.Text = "Baby Package of sounds and shapes:";
            this.lblBabyPackage.Bounds = new UniRectangle(20.0f, 30.0f, 300.0f, 30.0f);
            //
            // lblBabyPackagePath
            //
            this.lblBabyPackagePath.Bounds = new UniRectangle(30.0f, 50.0f, 300.0f, 30.0f);
            //
            // btnBabyPackageBrowse
            //
            this.btnBabyPackageBrowse.Bounds = new UniRectangle(350.0f, 30.0f, 80.0f, 24.0f);
            this.btnBabyPackageBrowse.Pressed += new EventHandler(this.btnBabyPackageBrowse_Pressed);
            this.btnBabyPackageBrowse.Text = "Select";

            //
            // lblKeyBashingThreshold
            //
            this.lblKeyBashingThreshold.Text = "Key Bashing Threshold (0 to turn off):";
            this.lblKeyBashingThreshold.Bounds = new UniRectangle(20.0f, 90.0f, 300.0f, 30.0f);
            //
            // txtKeyBashingThreshold
            //
            this.txtKeyBashingThreshold.Bounds = new UniRectangle(350.0f, 90.0f, 60.0f, 30.0f);


            //
            // lblMouseRequired
            //
            this.lblMouseRequired.Text = "Must Press Mouse Button When Moving:";
            this.lblMouseRequired.Bounds = new UniRectangle(20.0f, 120.0f, 300.0f, 30.0f);
            //
            // chkMouseRequired
            //
            this.chkMouseRequired.Bounds = new UniRectangle(350.0f, 120.0f, 60.0f, 30.0f);


            //
            // lblAutoPublishErrors
            //
            this.lblAutoPublishErrors.Text = "Automatically Publish Errors to Murray:";
            this.lblAutoPublishErrors.Bounds = new UniRectangle(20.0f, 150.0f, 300.0f, 30.0f);
            //
            // chkAutoPublishErrors
            //
            this.chkAutoPublishErrors.Bounds = new UniRectangle(350.0f, 150.0f, 60.0f, 30.0f);


            //
            // lblDeveloperMode
            //
            this.lblDeveloperMode.Text = "Developer Mode:";
            this.lblDeveloperMode.Bounds = new UniRectangle(20.0f, 180.0f, 300.0f, 30.0f);
            //
            // chkDeveloperMode
            //
            this.chkDeveloperMode.Bounds = new UniRectangle(350.0f, 180.0f, 60.0f, 30.0f);

            
            //
            // lblRunOnAllMonitors
            //
            this.lblRunOnAllMonitors.Text = "Run on all Monitors (Must Restart BabyBash):";
            this.lblRunOnAllMonitors.Bounds = new UniRectangle(20.0f, 150.0f, 300.0f, 30.0f);
            //
            // chkRunOnAllMonitors
            //
            this.chkRunOnAllMonitors.Bounds = new UniRectangle(350.0f, 150.0f, 60.0f, 30.0f);


            //
            // lblDisabledKeys
            //
            this.lblDisabledKeys.Text = "Deactivated Keys:";
            this.lblDisabledKeys.Bounds = new UniRectangle(20.0f, 180.0f, 300.0f, 30.0f);
            //
            // lblDisabledKeyCount
            //
            this.lblDisabledKeyCount.Text = "{0} Deactivated";
            this.lblDisabledKeyCount.Bounds = new UniRectangle(450.0f, 180.0f, 300.0f, 30.0f);
            //
            // btnDisableKeys
            //
            this.btnDisableKeys.Bounds = new UniRectangle(350.0f, 180.0f, 80f, 24f);
            this.btnDisableKeys.Pressed += new EventHandler(this.btnDisableKeys_Pressed);
            this.btnDisableKeys.Text = "Choose";


            //
            // lblErrorMessage
            //
            this.lblErrorMessage.Text = "";
            this.lblErrorMessage.Bounds = new UniRectangle(20.0f, 230.0f, 300.0f, 30.0f);

            //
            // btnUndo
            //
            this.btnUndo.Bounds = new UniRectangle(
              new UniScalar(1.0f, -250.0f), new UniScalar(1.0f, -40.0f), 80, 24
            );
            this.btnUndo.Pressed += new EventHandler(this.undoButton_Pressed);
            this.btnUndo.Text = "Undo";
            //
            // btnClose
            //
            this.btnClose.Bounds = new UniRectangle(
              new UniScalar(1.0f, -160.0f), new UniScalar(1.0f, -40.0f), 140, 24
            );
            this.btnClose.Pressed += new EventHandler(this.closeButton_Pressed);
            this.btnClose.Text = "Save and Close";
            //
            // btnAbout
            //
            this.btnAbout.Bounds = new UniRectangle(
              new UniScalar(0.0f, 40.0f), new UniScalar(1.0f, -40.0f), 80, 24
            );
            this.btnAbout.Pressed += new EventHandler(this.creditsButton_Pressed);
            this.btnAbout.Text = "About";

            //
            // DemoDialog
            //
            this.Bounds = new UniRectangle(new UniScalar(0, 0), new UniScalar(0, 0), 600f, 300f);
            this.Title = "Options";

            this.Children.Add(this.lblBabyPackage);
            this.Children.Add(this.lblBabyPackagePath);
            this.Children.Add(this.btnBabyPackageBrowse);

            this.Children.Add(this.lblKeyBashingThreshold);
            this.Children.Add(this.txtKeyBashingThreshold);

            this.Children.Add(this.lblMouseRequired);
            this.Children.Add(this.chkMouseRequired);

            this.Children.Add(this.lblAutoPublishErrors);
            this.Children.Add(this.chkAutoPublishErrors);

            this.Children.Add(this.lblDeveloperMode);
            this.Children.Add(this.chkDeveloperMode);

            // Disable these until they're implemented.
            //this.Children.Add(this.lblRunOnAllMonitors);
            //this.Children.Add(this.chkRunOnAllMonitors);

            //this.Children.Add(this.lblDisabledKeys);
            //this.Children.Add(this.lblDisabledKeyCount);
            //this.Children.Add(this.btnDisableKeys);

            this.Children.Add(this.lblErrorMessage);

            this.Children.Add(this.btnUndo);
            this.Children.Add(this.btnClose);
            this.Children.Add(this.btnAbout);
        }

        #endregion // NOT Component Designer generated code
    }

}
