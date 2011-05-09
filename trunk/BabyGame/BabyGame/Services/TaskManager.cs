using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nuclex.Game.States;

namespace MurrayGrant.BabyGame.Services
{
    public class TaskManager
    {
        private List<Task> _BackgroundTasks = new List<Task>();
        private GameMain _Game;

        public TaskManager(GameMain game)
        {
            this._Game = game;
        }

        public void RegisterTask(Task t)
        {
            lock (this._BackgroundTasks)
                this._BackgroundTasks.Add(t);
        }
        public void InspectAndRetireRegisteredTasks()
        {
            lock (this._BackgroundTasks)
            {
                if (this._BackgroundTasks.Count == 0)
                    return;

                // Task had an exception, rethrow it here to bring down the game with the FailedGame handler.
                foreach (var t in this._BackgroundTasks.Where(t => t.IsFaulted && t.Exception.GetBaseException().GetType() != typeof(OperationCanceledException)))
                    throw t.Exception;

                // Check if the loader task was cancelled.
                if (this._BackgroundTasks.Any(t => t.IsFaulted && t.Exception.GetBaseException().GetType() == typeof(OperationCanceledException)))
                    this._Game.Services.GetService<IGameStateService>().Pop();        // Remove the loader to allow the game to exit.

                // Clean up any tasks which are completed.
                foreach (var t in this._BackgroundTasks.Where(t => t.IsCompleted).ToList())
                    this._BackgroundTasks.Remove(t);
            }
        }
    }
}
