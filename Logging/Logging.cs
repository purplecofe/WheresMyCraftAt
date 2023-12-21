using ExileCore;
using SharpDX;
using static WheresMyCraftAt.WheresMyCraftAt;

namespace WheresMyCraftAt.Handlers
{
    public static class Logging
    {
        public static void DebugPrint(string printString, LogMessageType messageType)
        {
            if (Main.Settings.DebugPrint)
            {
                Color messageColor = Main._logMessageColors[messageType];
                DebugWindow.LogMsg(printString, Main.Settings.DebugPrintLingerTime, messageColor);
            }
        }
    }
}