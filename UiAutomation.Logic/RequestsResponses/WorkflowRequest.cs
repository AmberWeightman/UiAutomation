using System;
using System.IO;
using UiAutomation.Logic.Automation;

namespace UiAutomation.Logic.RequestsResponses
{
    public abstract class WorkflowRequest : IWorkflowRequest
    {
        public const int DefaultTimeoutMS = 2000;

        public int InitialTimeoutMS = 0;

        private static string _defaultScreenshotBaseDirectoryPath = @"\\MT-SCHFILE01-L\AutoOrderAttachments\UiAutomation\";

        public string SavedScreenShotDirectory { get; private set; }
        
        public WorkflowRequest(string screenshotBaseDirectory, string screenshotProcessSubDirectory, string screenshotWorkflowSubDirectory, int? initialTimeoutMS = null)
        {
            if (initialTimeoutMS != null && initialTimeoutMS.HasValue)
            {
                InitialTimeoutMS = initialTimeoutMS.Value;
            }

            if (string.IsNullOrEmpty(screenshotBaseDirectory))
            {
                screenshotBaseDirectory = _defaultScreenshotBaseDirectoryPath;
            }

            // TODO regex or something would be better, this looks pretty inefficient...
            screenshotProcessSubDirectory = screenshotProcessSubDirectory.Trim().TrimStart('\\').TrimEnd('\\');
            if (!string.IsNullOrEmpty(screenshotProcessSubDirectory))
            {
                screenshotProcessSubDirectory += @"\";
            }

            var dateTimeStamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            screenshotWorkflowSubDirectory = (string.IsNullOrEmpty(screenshotWorkflowSubDirectory)) ? dateTimeStamp : $"{screenshotWorkflowSubDirectory}_{dateTimeStamp}";

            SavedScreenShotDirectory = $@"{screenshotBaseDirectory}{screenshotProcessSubDirectory}{screenshotWorkflowSubDirectory}\";
            Directory.CreateDirectory(SavedScreenShotDirectory);
        }

        public virtual bool Validate()
        {
            if (string.IsNullOrEmpty(SavedScreenShotDirectory))
            {
                throw new ApplicationException("SavedScreenShotDirectory must be set.");
            }
            return true;
        }
    }
}
