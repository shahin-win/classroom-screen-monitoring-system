using System;
using System.Runtime.InteropServices;

namespace Service_Host_Visual
{
    public static class InputInjector
    {
        // ============================
        // Win32 Imports
        // ============================
        [DllImport("user32.dll")]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        // ============================
        // Constants
        // ============================
        private const int INPUT_MOUSE = 0;
        private const int INPUT_KEYBOARD = 1;

        private const uint MOUSEEVENTF_MOVE = 0x0001;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_WHEEL = 0x0800;
        private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;

        private const uint KEYEVENTF_KEYUP = 0x0002;

        // ============================
        // MAIN ENTRY
        // ============================
        public static void Inject(RemoteInputCommand cmd)
        {
            switch (cmd.Type)
            {
                case RemoteInputType.MouseMove:
                    InjectMouseMove(cmd.X, cmd.Y);
                    break;

                case RemoteInputType.MouseDown:
                    InjectMouseButton(cmd.Button, true);
                    break;

                case RemoteInputType.MouseUp:
                    InjectMouseButton(cmd.Button, false);
                    break;

                case RemoteInputType.MouseWheel:
                    InjectMouseWheel(cmd.Delta);
                    break;

                case RemoteInputType.KeyDown:
                    InjectKey(cmd.KeyCode, false);
                    break;

                case RemoteInputType.KeyUp:
                    InjectKey(cmd.KeyCode, true);
                    break;
            }
        }

        // ============================
        // MOUSE
        // ============================
        private static void InjectMouseMove(int x, int y)
        {
            int screenW = GetSystemMetrics(0);
            int screenH = GetSystemMetrics(1);

            //int absX = x * 65535 / screenW;
            //int absY = y * 65535 / screenH;

            int absX = (int)Math.Round(x * 65535.0 / (screenW - 1));
            int absY = (int)Math.Round(y * 65535.0 / (screenH - 1));


            INPUT input = new INPUT
            {
                type = INPUT_MOUSE,
                U = new InputUnion
                {
                    mi = new MOUSEINPUT
                    {
                        dx = absX,
                        dy = absY,
                        dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE
                    }
                }
            };

            SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
        }


        private static void InjectMouseButton(int button, bool down)
        {
            uint flag = button switch
            {
                1 => down ? MOUSEEVENTF_LEFTDOWN : MOUSEEVENTF_LEFTUP,
                2 => down ? MOUSEEVENTF_RIGHTDOWN : MOUSEEVENTF_RIGHTUP,
                _ => 0
            };

            if (flag == 0) return;

            INPUT input = new INPUT
            {
                type = INPUT_MOUSE,
                U = new InputUnion
                {
                    mi = new MOUSEINPUT
                    {
                        dwFlags = flag | MOUSEEVENTF_ABSOLUTE
                    }
                }
            };

            SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
        }


        private static void InjectMouseWheel(int delta)
        {
            INPUT input = new INPUT
            {
                type = INPUT_MOUSE,
                U = new InputUnion
                {
                    mi = new MOUSEINPUT
                    {
                        mouseData = (uint)delta,
                        dwFlags = MOUSEEVENTF_WHEEL
                    }
                }
            };

            SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
        }

        // ============================
        // KEYBOARD
        // ============================
        private static void InjectKey(int keyCode, bool keyUp)
        {
            INPUT input = new INPUT
            {
                type = INPUT_KEYBOARD,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = (ushort)keyCode,
                        dwFlags = keyUp ? KEYEVENTF_KEYUP : 0
                    }
                }
            };

            SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
        }

        // ============================
        // STRUCTS (CORRECT)
        // ============================
        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public int type;
            public InputUnion U;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }
    }
}
