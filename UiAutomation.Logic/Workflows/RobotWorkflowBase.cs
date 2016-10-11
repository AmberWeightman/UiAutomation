using System;
using UiAutomation.Logic.RequestsResponses;
using UiAutomation.Logic.Automation;

namespace UiAutomation.Logic.Workflows
{
    public abstract class RobotWorkflowBase<TRequest, TResponse>
        where TRequest : WorkflowRequest
        where TResponse : WorkflowResponse, new()
    {
        public abstract string WorkflowFile { get; }

        public abstract WorkflowType WorkflowType { get; }

        public abstract int MaxWorkflowDurationMins { get; }
        
        public virtual TResponse Execute(TRequest request)
        {
            request.Validate();

            using (var robotWorker = new RobotWorker<TRequest, TResponse>(request, WorkflowType))
            {
                var response = robotWorker.ExecuteRobotJobSynchronously(this);

                response.Validate(request);
                if (response.Error != null)
                {
                    throw response.Error;
                }

                return response as TResponse;
            }
        }
        
        public virtual Guid ExecuteAsync(TRequest request)
        {
            request.Validate();

            using (var robotWorker = new RobotWorker<TRequest, TResponse>(request, WorkflowType))
            {
                throw new NotImplementedException();
            }
        }
        
    }
    
}
