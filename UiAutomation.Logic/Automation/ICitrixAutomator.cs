using System;

namespace UiAutomation.Logic.Automation
{
    public interface ICitrixAutomator
    {
        /// <summary>
        /// Check whether a Citrix application has been launched successfully, is running, and is ready to use
        /// with no error or warning popups. If the application is not running and available, launch it.
        /// If the application has error or warning popups, dismiss them. If the application still has error
        /// or warning popups or failed to launch, kill all related processes and attempt relaunch.
        /// </summary>
        bool EnsureCitrixRunning(string processId, string screenshotDirectoryPath);

        /// <summary>
        /// Avoid calling this if at all possible
        /// </summary>
        void KillCitrixProcesses();
    }
}
