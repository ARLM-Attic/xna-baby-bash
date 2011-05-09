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
using System.Threading;
using Nuclex.UserInterface;
using Nuclex.UserInterface.Controls.Desktop;
using Microsoft.Xna.Framework;
using MurrayGrant.BabyGame.Services;

namespace MurrayGrant.BabyGame
{
    public partial class AboutDialog : WindowControl, IObserver<Version>, IObserver<System.Deployment.Application.UpdateCheckInfo>
    {
        private IDisposable _UnsubscribeUpdateAvailable;
        private IDisposable _UnsubscribeUpdateInstalled;
        private SynchronizationContext _UiThread = SynchronizationContext.Current;
        private Game Game;

        public AboutDialog(Game game)
            : base()
        {
            InitializeComponent();
            this.Game = game;
            
            // Only show the update button if this is installed via ClickOnce (which isn't the case when Murray is developing!)
            var updater = this.Game.Services.GetService<Services.IApplicationUpdater>();
            if (!updater.SupportsUpdates)
                this.Children.Remove(this.btnCheckUpdates);
        }

        private void btnClose_Pressed(object sender, EventArgs e)
        {
            ((IObserver<System.Deployment.Application.UpdateCheckInfo>)this).OnCompleted();
            ((IObserver<Version>)this).OnCompleted(); 
            this.Close();
        }
        private void btnCheckUpdates_Pressed(object sender, EventArgs e)
        {
#if DEBUG
            System.Diagnostics.Debugger.Launch();
#endif
            var updater = this.Game.Services.GetService<Services.IApplicationUpdater>();
            this._UnsubscribeUpdateAvailable = updater.Subscribe((IObserver<System.Deployment.Application.UpdateCheckInfo>)this);
            this._UnsubscribeUpdateInstalled = updater.Subscribe((IObserver<Version>)this);

            this.lblUpdateInfo.Text = "Checking for Updates...";
            this.btnCheckUpdates.Enabled = false;

            if (!updater.UpdatingNow)
            {
                var t = new System.Threading.Tasks.Task(() =>
                    {
                        updater.DoCheckAndUpdate(true);
                    }, System.Threading.Tasks.TaskCreationOptions.LongRunning);
                this.Game.Services.GetService<TaskManager>().RegisterTask(t);
                t.Start();
            }
        }

        #region IObserver for UpdateAvailable
        void IObserver<System.Deployment.Application.UpdateCheckInfo>.OnCompleted()
        {
            if (this._UnsubscribeUpdateAvailable != null)
            {
                this._UnsubscribeUpdateAvailable.Dispose();
                this._UnsubscribeUpdateAvailable = null;
            }
        }

        void IObserver<System.Deployment.Application.UpdateCheckInfo>.OnError(Exception error)
        {
            this._UiThread.Post((o) =>
                {
                    if (error is System.Deployment.Application.DeploymentDownloadException)
                        this.lblUpdateInfo.Text = String.Format("Network error: please check your Internet connection and try again later.");
                    else
                        this.lblUpdateInfo.Text = String.Format("Unable to check for new version: {0}", error.Message);
                    this.btnCheckUpdates.Enabled = true;
                    ((IObserver<System.Deployment.Application.UpdateCheckInfo>)this).OnCompleted();     // Tear down.
                }, null);
        }

        void IObserver<System.Deployment.Application.UpdateCheckInfo>.OnNext(System.Deployment.Application.UpdateCheckInfo value)
        {
            this._UiThread.Post((o) =>
                {
                    if (value.UpdateAvailable)
                        this.lblUpdateInfo.Text = String.Format("New version {0} available. Downloading...", value.AvailableVersion);
                    else
                        this.lblUpdateInfo.Text = "No new version available.";
                }, null);
        }
        #endregion

        #region IObserver for UpdateInstalled
        void IObserver<Version>.OnCompleted()
        {
            if (this._UnsubscribeUpdateInstalled != null)
            {
                this._UnsubscribeUpdateInstalled.Dispose();
                this._UnsubscribeUpdateInstalled = null;
            }
        }

        void IObserver<Version>.OnError(Exception error)
        {
            this._UiThread.Post((o) =>
                {
                    if (error is System.Deployment.Application.DeploymentDownloadException)
                        this.lblUpdateInfo.Text = String.Format("Network error: please check your Internet connection and try again later.");
                    else
                        this.lblUpdateInfo.Text = String.Format("Unable to check for new version: {0}", error.Message);
                    this.btnCheckUpdates.Enabled = true;
                    ((IObserver<Version>)this).OnCompleted();     // Tear down.
                }, null);
        }

        void IObserver<Version>.OnNext(Version value)
        {
            this._UiThread.Post((o) =>
                {
                    if (value > Helper.GetApplicationVesion())
                        this.lblUpdateInfo.Text = String.Format("New version {0} installed. Please restart Baby Bash.", value);
                    else
                        this.lblUpdateInfo.Text = "No new version available.";
                    this.btnCheckUpdates.Enabled = true;
                }, null);
        }
        #endregion
    }
}
