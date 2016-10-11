using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace UiAutomation.Logic.Automation
{
    public static class Threading
    {
        // TODO this is probably a really bad way to handle threading
        //public volatile static Dictionary<Guid, IRobotWorker> RobotWorkers = new Dictionary<Guid, IRobotWorker>();
        public static IRobotWorker ActiveRobotWorker = null;

        // Can only have one robot thread running at a time, because UiPath and UiRobot are unreliable with memory as it is.
        private static bool _isRobotWorkerThreadActive = false;

        internal static Thread CreateRobotThread(ThreadStart threadStart)
        {
            if (_isRobotWorkerThreadActive)
            {
                return null;
            }
            _isRobotWorkerThreadActive = true;
            return new Thread(threadStart);
        }

        internal static void KillRobotThread()
        {
            KillRobots();
            _isRobotWorkerThreadActive = false;
        }

        // UiRobot has memory inefficiencies. We can't rely on it to clear its own memory
        private static void KillRobots()
        {
            ProcessHelper.EndProcessesByName(new string[] { "UiRobot" }, ProcessHelper.CloseProcessType.Kill, tryCloseSafely: false);
        }

    }
}
