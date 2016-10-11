using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace UiAutomation.Logic.Automation
{
    public static class ProcessHelper
    {
        public enum CloseProcessType
        {
            Kill,
            CloseMainWindow
        }

        private static void EndProcessesByName(string[] processNames, CloseProcessType closeProcessType)
        {
            int timeoutSeconds = 0;
            var processes = GetProcessesByNames(processNames);
            
            try
            {
                foreach (Process proc in processes)
                {
                    switch (closeProcessType)
                    {
                        case CloseProcessType.Kill: 
                            timeoutSeconds = 60;
                            proc.Kill();
                            break;
                        case CloseProcessType.CloseMainWindow: 
                            timeoutSeconds = 10;
                            proc.CloseMainWindow();
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            // Loop until termination of processes has succeeded (or timeout has passed)
            var i = 0;
            var processesStillRunning = true;
            while (processesStillRunning && i++ < timeoutSeconds)
            {
                Thread.Sleep(1000);

                // If any of the processes now running under these process names are the same ones we tried to kill earlier, keep trying
                var processes2 = GetProcessesByNames(processNames);
                if (!processes2.Any(a => processes.Any(b => b.Id == a.Id)))
                {
                    processesStillRunning = false;
                }
            }
        }

        /// <summary>
        /// Terminate a running process.
        /// </summary>
        /// <param name="processNames">Process names to close.</param>
        /// <param name="closeProcessType">Kill, Close, or CloseMainWindow.</param>
        /// <param name="tryCloseSafely">If true (default), where closeProcessType is Kill, attempt to run using
        /// CloseMainWindow before running using Kill.</param>
        public static void EndProcessesByName(string[] processNames, CloseProcessType closeProcessType, bool tryCloseSafely = true)
        {
            if (!tryCloseSafely || closeProcessType == CloseProcessType.CloseMainWindow)
            {
                EndProcessesByName(processNames, closeProcessType);
            }
            else if (closeProcessType == CloseProcessType.Kill)
            {
                // Let it attempt to close the main window gracefully before killing the process
                EndProcessesByName(processNames, CloseProcessType.CloseMainWindow);
                EndProcessesByName(processNames, closeProcessType);
            }
        }

        private static List<Process> GetProcessesByNames(string[] processNames)
        {
            var processes = new List<Process>();
            foreach (var processName in processNames)
            {
                processes.AddRange(Process.GetProcessesByName(processName));
            }
            return processes.OrderBy(a => a.StartTime).ToList();
        }
    }
}
