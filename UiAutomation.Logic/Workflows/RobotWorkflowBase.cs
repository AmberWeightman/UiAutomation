using System;
using UiAutomation.Logic.RequestsResponses;
using System.Linq;
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
                PrintMessages(response);

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

        private void PrintMessages(WorkflowResponse response)
        {
            // TODO this error checking probably doesn't belong here
            if (response.ErrorMessages.Any())
            {
                foreach (var error in response.ErrorMessages)
                {
                    Console.WriteLine($"Error {error.Code}: {error}");
                }
            }

            if (response.WarningMessages.Any())
            {
                foreach (var warn in response.WarningMessages)
                {
                    Console.WriteLine($"Warning {warn.Code}: {warn}");
                }
            }
        }

    }
    
}
