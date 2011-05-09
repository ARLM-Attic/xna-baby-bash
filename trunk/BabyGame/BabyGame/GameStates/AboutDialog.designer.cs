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
    partial class AboutDialog
    {
        private Nuclex.UserInterface.Controls.Desktop.ButtonControl btnClose;
        private Nuclex.UserInterface.Controls.Desktop.ButtonControl btnCheckUpdates;
        private Nuclex.UserInterface.Controls.LabelControl lblUpdateInfo;
        private Nuclex.UserInterface.Controls.LabelControl lblCopyright;
        private Nuclex.UserInterface.Controls.LabelControl lblWebsite;
        private Nuclex.UserInterface.Controls.LabelControl lblVersion;


        #region NOT Component Designer generated code

        /// <summary> 
        ///   Required method for user interface initialization -
        ///   do modify the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnClose = new Nuclex.UserInterface.Controls.Desktop.ButtonControl();
            this.btnCheckUpdates = new Nuclex.UserInterface.Controls.Desktop.ButtonControl();
            this.lblUpdateInfo = new Nuclex.UserInterface.Controls.LabelControl();
            this.lblCopyright = new Nuclex.UserInterface.Controls.LabelControl();
            this.lblWebsite = new Nuclex.UserInterface.Controls.LabelControl();
            this.lblVersion = new Nuclex.UserInterface.Controls.LabelControl();

            //
            // lblCopyright
            //
            this.lblCopyright.Bounds = new UniRectangle(
              new UniScalar(0.0f, 20.0f), new UniScalar(0.0f, 40.0f),
              new UniScalar(0.6f, -30.0f), 25
            );
            this.lblCopyright.Text = "Baby Bash XNA - Copyright Murray Grant 2011";                
                
            //
            // lblWebsite
            //
            this.lblWebsite.Bounds = new UniRectangle(
              new UniScalar(0.0f, 20.0f), new UniScalar(0.0f, 65.0f),
              new UniScalar(0.6f, -30.0f), 25
            );
            this.lblWebsite.Text = "http://babybashxna.codeplex.com";

            //
            // lblVersion
            //
            this.lblVersion.Bounds = new UniRectangle(
              new UniScalar(0.0f, 40.0f), new UniScalar(0.0f, 90.0f), 
              new UniScalar(0.6f, -30.0f), 25
            );
            this.lblVersion.Text = "Version: " + Helper.GetApplicationVesion().ToString();


            //
            // lblUpdateInfo
            //
            this.lblUpdateInfo.Bounds = new UniRectangle(
              new UniScalar(0.0f, 20.0f), new UniScalar(1.0f, -80.0f),
              new UniScalar(0.8f, -30.0f), 25
            );
            this.lblUpdateInfo.Text = String.Empty;

            //
            // btnCheckUpdates
            //
            this.btnCheckUpdates.Bounds = new UniRectangle(
              new UniScalar(0.0f, 20.0f), new UniScalar(1.0f, -40.0f), 130, 24
            );
            this.btnCheckUpdates.Text = "Check for Updates";
            this.btnCheckUpdates.Pressed += new EventHandler(this.btnCheckUpdates_Pressed);
            //
            // btnClose
            //
            this.btnClose.Bounds = new UniRectangle(
              new UniScalar(1.0f, -100.0f), new UniScalar(1.0f, -40.0f), 80, 24
            );
            this.btnClose.Text = "Close";
            this.btnClose.Pressed += new EventHandler(this.btnClose_Pressed);

            //
            // DemoDialog
            //
            this.Bounds = new UniRectangle(new UniScalar(0, -20.0f), new UniScalar(0, -20.0f), 430, 200);
            this.Title = "About";

            this.Children.Add(this.btnClose);
            this.Children.Add(this.btnCheckUpdates);
            this.Children.Add(this.lblUpdateInfo);
            this.Children.Add(this.lblCopyright);
            this.Children.Add(this.lblWebsite);
            this.Children.Add(this.lblVersion);
        }

        #endregion // NOT Component Designer generated code

    }

}
