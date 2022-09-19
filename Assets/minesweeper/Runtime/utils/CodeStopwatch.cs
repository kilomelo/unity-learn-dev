using System.Diagnostics;

namespace Kilomelo.minesweeper.Runtime
{
    internal static class CodeStopwatch
    {
        private static Stopwatch _stopwatch;

        internal static void Start()
        {
            _stopwatch = Stopwatch.StartNew();
        }

        internal static long ElapsedMilliseconds()
        {
            return _stopwatch.ElapsedMilliseconds;
        }
    }
}