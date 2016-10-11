namespace UiAutomation.Logic.Workflows
{
    public enum WorkflowType
    {
        CheckCitrixAvailable,
        ChromeDownloadCitrix,
        LINZTitleSearch
    }

    public static class WorkflowFiles
    {
        private static string _workflowPath = @"C:\Git\UiAutomation\UiAutomation.Logic\Workflows\";

        public static string CheckCitrixAvailableWorkflow = $@"{_workflowPath}CheckCitrixAvailable\Main.xaml";
        public static string ChromeDownloadCitrixWorkflow = $@"{_workflowPath}ChromeDownloadCitrix\Main.xaml";
        public static string LINZTitleSearchWorkflow = $@"{_workflowPath}LINZTitleSearch\Main.xaml";
    }
}
