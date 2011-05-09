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
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MurrayGrant.BabyGame
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread()]
        static void Main(string[] args)
        {
            // TODO: I need access to config object here for multimonitor.
            // TODO: Stats logging and sending back to base.
            // TODO: Icon that's not the default
            // TODO: test me under Windows Vista and Windows XP
            DoSingleMonitorRun();            
        }

        static void DoSingleMonitorRun()
        {
            System.Windows.Forms.Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);   // Hook the old Application.ThreadException.
            try
            {
#if DEBUG
                using (var kh = new KeyboardHook(KeyboardHook.Parameters.DisableWindowsKey, false))
#else
                using (var kh = new KeyboardHook(KeyboardHook.Parameters.DisableAll, false))
#endif
                using (GameMain game = new GameMain(null))
                {
                    // TODO: when a laptop goes to sleep, you get an ObjectDisposedException here. Perhaps handling the graphics device destroyed event will help????
                    game.Run();
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }

        }

        static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            HandleException(e.Exception);
        }

        static void HandleException(Exception ex)
        {
            try
            {
                using (var failedGame = new FailedGame(ex))
                    failedGame.Run();
            }
            catch (Exception evenWorseEx)
            {
                System.Windows.Forms.MessageBox.Show(evenWorseEx.ToString(), "Baby Bash XNA Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
            
        }        

        static void DoMultiMonitorRun()
        {
            // TODO: lots of testing!
            // A thread and game object per monitor.
            var gameThreads = new Thread[GraphicsAdapter.Adapters.Count];
            var gameObjects = new GameMain[GraphicsAdapter.Adapters.Count];
            for (int i = 0; i < gameThreads.Length; i++)
            {
                gameObjects[i] = new GameMain(System.Windows.Forms.Screen.AllScreens[i]);
                gameThreads[i] = new Thread((obj) =>
                {
                    ((GameMain)obj).Run();
                });
            }
            gameObjects[0].OtherScreens.AddRange(gameObjects.Skip(1));      // Set references on the master screen to other screens.

            // Start them all up with the keyboard hook that disables the Windows key.
            using (var kh = new KeyboardHook(KeyboardHook.Parameters.DisableWindowsKey, false))
            {
                for (int i = 0; i < gameThreads.Length; i++)
                    gameThreads[i].Start(i + 1);

                // And block until they all complete.
                for (int i = 0; i < gameThreads.Length; i++)
                    gameThreads[i].Join();

                // Finally, dispose everything.
                for (int i = 0; i < gameThreads.Length; i++)
                    gameObjects[i].Dispose();
            }
        }
    }
#endif
}

