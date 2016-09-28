using UiAutomation.Logic.RequestsResponses;

namespace UiAutomation.Logic.Workflows
{
    public class DownloadCitrixRequest : WorkflowRequest
    {
        public static string CitrixPassword = "5Vc-1Dm5";

        public DownloadCitrixRequest(int? initialTimeoutMS = null) : base(initialTimeoutMS) { }
    }

    public class ChromeDownloadCitrixWorkflow : RobotWorkflowBase<DownloadCitrixRequest, WorkflowResponse>
    {
        public  override string WorkflowFile => WorkflowFiles.ChromeDownloadCitrixWorkflow;

        public override WorkflowType WorkflowType => WorkflowType.ChromeDownloadCitrix;

        public override int MaxWorkflowDurationMins => 5;
    }

}
