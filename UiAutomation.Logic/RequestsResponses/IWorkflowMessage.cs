namespace UiAutomation.Logic.RequestsResponses
{
    public interface IWorkflowRequest
    {
        bool Validate();
    }

    public interface IWorkflowResponse
    {
        bool Validate(IWorkflowRequest request);
    }
}
