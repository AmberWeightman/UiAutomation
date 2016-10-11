using System;
using UiAutomation.Logic.RequestsResponses;

namespace UiAutomation.Logic.Workflows
{
    public class CheckCitrixAvailabilityWorkflowRequest : WorkflowRequest
    {
        public CheckCitrixAvailabilityWorkflowRequest(string screenshotBaseDirectory, string screenshotProcessSubDirectory, int? initialTimeoutMS = null) : base(screenshotBaseDirectory, screenshotProcessSubDirectory, WorkflowType.CheckCitrixAvailable.ToString(), initialTimeoutMS) { }
    }

    public class CheckCitrixAvailabilityWorkflow : RobotWorkflowBase<WorkflowRequest, WorkflowResponse>
    {
        public override string WorkflowFile => WorkflowFiles.CheckCitrixAvailableWorkflow;

        public override WorkflowType WorkflowType => WorkflowType.CheckCitrixAvailable;

        public override int MaxWorkflowDurationMins => 2;

    }

}
