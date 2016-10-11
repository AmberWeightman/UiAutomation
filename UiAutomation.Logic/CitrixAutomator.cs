using System;
using UiAutomation.Logic.Automation;

namespace UiAutomation.Logic
{
    public abstract class CitrixAutomator : ICitrixAutomator
    {
        public abstract bool EnsureCitrixRunning(string processId, string screenshotDirectoryPath);

        private static string[] _citrixProcessNames = new string[] 
        {
            "AuthManSvr",
            "concentr",
            "Receiver",
            "redirector",
            "SelfServicePlugin",
            "wfcrun32",
            "wfica32"
        };

        public void KillCitrixProcesses()
        {
            ProcessHelper.EndProcessesByName(_citrixProcessNames, ProcessHelper.CloseProcessType.Kill);
        }
    }
}
