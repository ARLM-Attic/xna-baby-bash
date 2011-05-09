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
using System.Deployment;
using System.Deployment.Application;
using System.Threading.Tasks;

namespace MurrayGrant.BabyGame.Services
{
    public interface IApplicationUpdater : IDisposable, IObservable<UpdateCheckInfo>, IObservable<Version>
    {
        void DoCheckAndUpdate();
        void DoCheckAndUpdate(bool forceCheck);
        bool SupportsUpdates { get; }
        bool UpdatingNow { get; }
    }

    public sealed class ApplicationUpdater
        : IApplicationUpdater
    {
        public GameMain Game { get; private set; }
        public bool UpdatingNow { get; private set; }
        private HashSet<IObserver<UpdateCheckInfo>> _UpdateAvailableObservers = new HashSet<IObserver<UpdateCheckInfo>>();
        private HashSet<IObserver<Version>> _UpdateInstalledObservers = new HashSet<IObserver<Version>>();

        public ApplicationUpdater(GameMain game)
        {
            this.Game = game;
        }

        public bool SupportsUpdates
        {
            get
            {
                return ApplicationDeployment.IsNetworkDeployed;
            }
        }
        public void DoCheckAndUpdate()
        {
            this.DoCheckAndUpdate(false);
        }
        public void DoCheckAndUpdate(bool forceUpdate)
        {
            if (!this.SupportsUpdates)
            {
                this.ForEachObserver(this._UpdateInstalledObservers, ob => ob.OnError(new UpdateNotSupportedException("Not a ClickOnce deployment.")));
                this.ForEachObserver(this._UpdateAvailableObservers, ob => ob.OnError(new UpdateNotSupportedException("Not a ClickOnce deployment.")));
                return;
            }

            // Get details of the current installation.
            this.UpdatingNow = true;
            try
            {
                var ad = ApplicationDeployment.CurrentDeployment;
                if (forceUpdate || DateTime.Now.Subtract(ad.TimeOfLastUpdateCheck).Days > 1)     // Check for updates every 24 hours.
                {
                    UpdateCheckInfo updateInfo = null;
                    try
                    {
                        updateInfo = ad.CheckForDetailedUpdate(true);

                        // Notify that an application update is available.
                        this.ForEachObserver(this._UpdateAvailableObservers, ob => ob.OnNext(updateInfo));
                    }
                    catch (InvalidOperationException)
                    {
                        // Eat: ClickOnce is already downloading / applying an update.
                    }
                    catch (DeploymentDownloadException ex)
                    {
                        // Notify subscribers but don't do anything else.
                        this.ForEachObserver(this._UpdateAvailableObservers, ob => ob.OnError(ex));
                    }
                    catch (InvalidDeploymentException ex)
                    {
                        // Something's wrong with the installation: bring down the app.
                        throw new UpdateException("Cannot check for a new version of the application. The ClickOnce deployment is corrupt. Please redeploy the application and try again.", ex);
                    }

                    if (updateInfo != null && updateInfo.UpdateAvailable)
                    {
                        try
                        {
                            ad.Update();

                            // Notify that an application update is available.
                            this.ForEachObserver(this._UpdateInstalledObservers, ob => ob.OnNext(updateInfo.AvailableVersion));
                            // Continue with the current version until restart.
                        }
                        catch (InvalidOperationException)
                        {
                            // Eat: ClickOnce is already downloading / applying an update.
                        }
                        catch (InvalidDeploymentException ex)
                        {
                            // Something's wrong with the installation: bring down the app.
                            throw new UpdateException("Cannot check for a new version of the application. The ClickOnce deployment is corrupt. Please redeploy the application and try again.", ex);
                        }
                        catch (DeploymentDownloadException ex)
                        {
                            // Notify observers but don't do anything else
                            // Ignore network errors.
                            this.ForEachObserver(this._UpdateInstalledObservers, ob => ob.OnError(ex));
                            //String.Format("Cannot install the latest version of the application. \n\nPlease check your network connection, or try again later.");
                        }
                        catch (TrustNotGrantedException ex)
                        {
                            // Something's wrong with the installation: bring down the app.
                            throw new UpdateException("Cannot install the latest version of the application. ClickOnce was not granted permission to install.", ex);
                        }
                    }
                }
            }
            finally
            {
                this.ForEachObserver(this._UpdateAvailableObservers, ob => ob.OnCompleted());
                this.ForEachObserver(this._UpdateInstalledObservers, ob => ob.OnCompleted());
                this.UpdatingNow = false;
            }
        }

        public void Dispose()
        {
            if (this._UpdateAvailableObservers != null)
            {
                this.ForEachObserver(this._UpdateAvailableObservers, ob => ob.OnCompleted());
                this._UpdateAvailableObservers.Clear();
                this._UpdateAvailableObservers = null;
            }
            if (this._UpdateInstalledObservers != null)
            {
                this.ForEachObserver(this._UpdateInstalledObservers, ob => ob.OnCompleted());
                this._UpdateInstalledObservers.Clear();
                this._UpdateInstalledObservers = null;
            }
            
            this.Game.Services.RemoveService(typeof(IApplicationUpdater));
        }

        public IDisposable Subscribe(IObserver<UpdateCheckInfo> observer)
        {
            return this.Subscribe(this._UpdateAvailableObservers, observer);
        }
        public IDisposable Subscribe(IObserver<Version> observer)
        {
            return this.Subscribe(this._UpdateInstalledObservers, observer);
        }

        #region Helpful IObservable<T> Types and Methods
        private IDisposable Subscribe<T>(HashSet<IObserver<T>> collection, IObserver<T> observer)
        {
            lock (collection)
            {
                if (!collection.Contains(observer))
                    collection.Add(observer);
            }
            return new EndObservation<T>(collection, observer);
        }
        private void ForEachObserver<T>(HashSet<IObserver<T>> collection, Action<IObserver<T>> action)
        {
            IObserver<T>[] toIterate = null;
            lock (collection)
                toIterate = collection.ToArray();   // The action can modify the collection, so iterate over a copy.
            foreach (var ob in toIterate)        
                action(ob);
        }
        private sealed class EndObservation<T> : IDisposable
        {
            private IObserver<T> _Observer;
            private HashSet<IObserver<T>> _Observers;
            public EndObservation(HashSet<IObserver<T>> observers, IObserver<T> observer)
            {
                this._Observer = observer;
                this._Observers = observers;
            }

            public void Dispose()
            {
                lock (this._Observers)
                {
                    if (this._Observers.Contains(this._Observer))
                        this._Observers.Remove(this._Observer);
                }

                // Break references to prevent memory leaks like with events.
                this._Observers = null;
                this._Observer = null;
            }
        }
        #endregion
    }
}
