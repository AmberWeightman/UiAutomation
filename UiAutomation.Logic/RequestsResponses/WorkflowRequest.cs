namespace UiAutomation.Logic.RequestsResponses
{
    public class WorkflowRequest : IWorkflowRequest
    {
        public const int DefaultTimeoutMS = 2000;

        public int InitialTimeoutMS = 0;

        public WorkflowRequest(int? initialTimeoutMS = null)
        {
            if (initialTimeoutMS != null && initialTimeoutMS.HasValue)
            {
                InitialTimeoutMS = initialTimeoutMS.Value;
            }
        }

        public virtual bool Validate()
        {
            return true;
        }
    }
}
