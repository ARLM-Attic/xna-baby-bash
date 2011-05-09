/// KEYBOARD.CS
/// (c) 2006 by Emma Burrows
/// This file contains the following items:
///  - KeyboardHook: class to enable low-level keyboard hook using
///    the Windows API.
///  - KeyboardHookEventHandler: delegate to handle the KeyIntercepted
///    event raised by the KeyboardHook class.
///  - KeyboardHookEventArgs: EventArgs class to contain the information
///    returned by the KeyIntercepted event.
///    
/// Change history:
/// 17/06/06: 1.0 - First version.
/// 18/06/06: 1.1 - Modified proc assignment in constructor to make class backward 
///                 compatible with 2003.
/// 10/07/06: 1.2 - Added support for modifier keys:
///                 -Changed filter in HookCallback to WM_KEYUP instead of WM_KEYDOWN
///                 -Imported GetKeyState from user32.dll
///                 -Moved native DLL imports to a separate internal class as this 
///                  is a Good Idea according to Microsoft's guidelines
/// 13/02/07: 1.3 - Improved modifier key support:
///                 -Added CheckModifiers() method
///                 -Deleted LoWord/HiWord methods as they weren't necessary
///                 -Implemented Barry Dorman's suggestion to AND GetKeyState
///                  values with 0x8000 to get their result
/// 23/03/07: 1.4 - Fixed bug which made the Alt key appear stuck
///                 - Changed the line
///                     if (nCode >= 0 && (wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP))
///                   to
///                     if (nCode >= 0)
///                     {
///                        if (wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP)
///                        ...
///                   Many thanks to "Scottie Numbnuts" for the solution.
///
/// 22/01/11: MG: - Change behavior disable specific keys and pass all others to the next hook rather than pass 
///                 everything to the custom handler or back to the OS. 
///               - Handle key down messages too.

// Modified 2011 Murray Grant 
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


// TODO: change default behaviour to allow all keys except a list of blocked ones.
// TODO: allow blocked keys to be reinjected as a generic key.

using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Text;

/// <summary>
/// Low-level keyboard intercept class to trap and suppress system keys.
/// </summary>
public class KeyboardHook : IDisposable
{
    /// <summary>
    /// Parameters accepted by the KeyboardHook constructor.
    /// </summary>
    [Flags()]
    public enum Parameters
    {
        None = 0,
        DisableAltTab = 0x01,
        DisableWindowsKey = 0x02,
        DisableAltTabAndWindows = 0x03,
        DisableAltF4 = 0x04,
        DisableAll = 0x07,
    }

    //Internal parameters
    private bool DisableAltF4 = false;
    private bool DisableAltTab = false;
    private bool DisableWindowsKey = false;
    private bool PassToEventHandler = false;

    //Keyboard API constants
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;


    //Modifier key constants
    private const int VK_SHIFT = 0x10;
    private const int VK_CONTROL = 0x11;
    private const int VK_MENU = 0x12;
    private const int VK_CAPITAL = 0x14;

    //Variables used in the call to SetWindowsHookEx
    private HookHandlerDelegate proc;
    private IntPtr hookID = IntPtr.Zero;
    internal delegate IntPtr HookHandlerDelegate(
        int nCode, IntPtr wParam, ref KBDLLHOOKSTRUCT lParam);

    /// <summary>
    /// Event triggered when a keystroke is intercepted by the 
    /// low-level hook.
    /// </summary>
    public event KeyboardHookEventHandler KeyIntercepted;

    // Structure returned by the hook whenever a key is pressed
    internal struct KBDLLHOOKSTRUCT
    {
        public int vkCode;
        int scanCode;
        public int flags;
        int time;
        int dwExtraInfo;
    }

    #region Constructors
    /// <summary>
    /// Sets up a keyboard hook to trap all keystrokes without 
    /// passing any to other applications.
    /// </summary>
    public KeyboardHook()
    {
        proc = new HookHandlerDelegate(HookCallback);
        using (Process curProcess = Process.GetCurrentProcess())
        using (ProcessModule curModule = curProcess.MainModule)
        {
            hookID = NativeMethods.SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                NativeMethods.GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    /// <summary>
    /// Sets up a keyboard hook with custom parameters.
    /// </summary>
    /// <param name="param">A valid name from the Parameter enum; otherwise, the 
    /// default parameter Parameter.None will be used.</param>
    public KeyboardHook(string param)
        : this()
    {
        if (!String.IsNullOrEmpty(param) && Enum.IsDefined(typeof(Parameters), param))
        {
            SetParameters((Parameters)Enum.Parse(typeof(Parameters), param));
        }
    }

    /// <summary>
    /// Sets up a keyboard hook with custom parameters.
    /// </summary>
    /// <param name="param">A value from the Parameters enum.</param>
    public KeyboardHook(Parameters param)
        : this()
    {
        SetParameters(param);
    }
    
    /// <summary>
    /// Sets up a keyboard hook with custom parameters.
    /// </summary>
    /// <param name="param">A value from the Parameters enum.</param>
    public KeyboardHook(Parameters param, bool passToEventHandler)
        : this()
    {
        SetParameters(param);
        this.PassToEventHandler = passToEventHandler;
    }

    private void SetParameters(Parameters param)
    {
        if (param == Parameters.None)
            return;

        DisableAltTab = param.HasFlag(Parameters.DisableAltTab);
        DisableWindowsKey = param.HasFlag(Parameters.DisableWindowsKey);
        DisableAltF4 = param.HasFlag(Parameters.DisableAltF4);
    }
    #endregion

    #region Check Modifier keys
    /// <summary>
    /// Checks whether Alt, Shift, Control or CapsLock
    /// is enabled at the same time as another key.
    /// Modify the relevant sections and return type 
    /// depending on what you want to do with modifier keys.
    /// </summary>
    private void CheckModifiers()
    {
        StringBuilder sb = new StringBuilder();

        if ((NativeMethods.GetKeyState(VK_CAPITAL) & 0x0001) != 0)
        {
            //CAPSLOCK is ON
            sb.AppendLine("Capslock is enabled.");
        }

        if ((NativeMethods.GetKeyState(VK_SHIFT) & 0x8000) != 0)
        { 
            //SHIFT is pressed
            sb.AppendLine("Shift is pressed.");
        }
        if ((NativeMethods.GetKeyState(VK_CONTROL) & 0x8000) != 0)
        {
            //CONTROL is pressed
            sb.AppendLine("Control is pressed.");
        }
        if ((NativeMethods.GetKeyState(VK_MENU) & 0x8000) != 0)
        {
            //ALT is pressed
            sb.AppendLine("Alt is pressed.");
        }
    }
    private bool CapslockOn()
    {
        return ((NativeMethods.GetKeyState(VK_CAPITAL) & 0x0001) != 0);
    }
    private bool ShiftPressed()
    {
        return ((NativeMethods.GetKeyState(VK_SHIFT) & 0x8000) != 0);
    }
    private bool ControlPressed()
    {
        return ((NativeMethods.GetKeyState(VK_CONTROL) & 0x8000) != 0);
    }
    private bool AltPressed()
    {
        return ((NativeMethods.GetKeyState(VK_MENU) & 0x8000) != 0);
    }
    #endregion Check Modifier keys

    #region Hook Callback Method
    /// <summary>
    /// Processes the key event captured by the hook.
    /// </summary>
    private IntPtr HookCallback(
        int nCode, IntPtr wParam, ref KBDLLHOOKSTRUCT lParam)
    {
        bool AllowKey = true;

        //Filter wParam for KeyUp and KeyDown events only
        if (nCode >= 0)
        {
            if (wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP 
                || wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
            {
                // TODO: raise the event handler for 'hidden' or extended keys so our app and still handle them.

                // Check for key combinations that are allowed to 
                // get through to Windows
                //
                // Ctrl+Esc or Windows key
                if (DisableWindowsKey)
                {
                    switch (lParam.flags & 0x00000001)
                    {
                        //Ctrl+Esc
                        case 0:
                            if (lParam.vkCode == 27 && this.ControlPressed())
                                AllowKey = false;
                            break;

                        //Windows keys
                        case 1:
                            if ((lParam.vkCode == 91) || (lParam.vkCode == 92))
                                AllowKey = false;
                            break;
                    }
                }
                // Alt+Tab
                if (DisableAltTab)
                {
                    if ((lParam.flags == 32) && (lParam.vkCode == 9))
                        AllowKey = false;
                }
                // Alt+F4
                if (DisableAltF4)
                {
                     if ((lParam.flags == 32) && (lParam.vkCode == 115))
                        AllowKey = false;
                }


                if (AllowKey && PassToEventHandler)
                {
                    OnKeyIntercepted(new KeyboardHookEventArgs(lParam.vkCode, AllowKey));       // Call event handler.
                    return (System.IntPtr)1;        // Return dummy value when intercepted.
                }
                else if (AllowKey && !PassToEventHandler)
                    return NativeMethods.CallNextHookEx(hookID, nCode, wParam, ref lParam);     // Not intercepted, pass to other apps.
                else if (!AllowKey)
                    return (System.IntPtr)1;        // Return dummy value when intercepted.
            }

        }
        //Pass key to next application
        return NativeMethods.CallNextHookEx(hookID, nCode, wParam, ref lParam);

    }
    #endregion

    #region Event Handling
    /// <summary>
    /// Raises the KeyIntercepted event.
    /// </summary>
    /// <param name="e">An instance of KeyboardHookEventArgs</param>
    public void OnKeyIntercepted(KeyboardHookEventArgs e)
    {
        if (KeyIntercepted != null)
            KeyIntercepted(e);
    }

    /// <summary>
    /// Delegate for KeyboardHook event handling.
    /// </summary>
    /// <param name="e">An instance of InterceptKeysEventArgs.</param>
    public delegate void KeyboardHookEventHandler(KeyboardHookEventArgs e);

    /// <summary>
    /// Event arguments for the KeyboardHook class's KeyIntercepted event.
    /// </summary>
    public class KeyboardHookEventArgs : System.EventArgs
    {

        private string keyName;
        private int keyCode;
        private bool passThrough;

        /// <summary>
        /// The name of the key that was pressed.
        /// </summary>
        public string KeyName
        {
            get { return keyName; }
        }

        /// <summary>
        /// The virtual key code of the key that was pressed.
        /// </summary>
        public int KeyCode
        {
            get { return keyCode; }
        }

        /// <summary>
        /// True if this key combination was passed to other applications,
        /// false if it was trapped.
        /// </summary>
        public bool PassThrough
        {
            get { return passThrough; }
        }

        public KeyboardHookEventArgs(int evtKeyCode, bool evtPassThrough)
        {
            keyName = ((Keys)evtKeyCode).ToString();
            keyCode = evtKeyCode;
            passThrough = evtPassThrough;
        }

    }

    #endregion

    #region IDisposable Members
    /// <summary>
    /// Releases the keyboard hook.
    /// </summary>
    public void Dispose()
    {
        NativeMethods.UnhookWindowsHookEx(hookID);
    }
    #endregion

    #region Native methods

    [ComVisibleAttribute(false),
     System.Security.SuppressUnmanagedCodeSecurity()]
    internal class NativeMethods
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook,
            HookHandlerDelegate lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, ref KBDLLHOOKSTRUCT lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.Winapi)]
        public static extern short GetKeyState(int keyCode);
        
    } 
 

    #endregion
}


