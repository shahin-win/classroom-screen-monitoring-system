namespace ScreenServer
{
    public enum RemoteInputType
    {
        MouseMove,
        MouseDown,
        MouseUp,
        MouseWheel,
        KeyDown,
        KeyUp
    }
    public class RemoteInputCommand
    {
       
        public RemoteInputType Type { get; set; }

        // Mouse
        public int X { get; set; }
        public int Y { get; set; }
        public int Delta { get; set; }
        public int Button { get; set; }

        // Keyboard
        public int KeyCode { get; set; }
    }
}
