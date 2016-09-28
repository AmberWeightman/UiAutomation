using System;
using System.Collections.Generic;
using System.Linq;

namespace UiAutomation.Logic.RequestsResponses
{
    public abstract class WorkflowSearchRequest : WorkflowRequest
    {
        public string[] OrderIds { get; set; }
        
        public const string OutputDirectory = @"C:\LandonlineOutputDirectory\";

        public override bool Validate()
        {
            var isValid = base.Validate();

            if (OrderIds == null || !OrderIds.Any())
            {
                throw new ApplicationException("OrderId is mandatory");
            }

            return isValid;
        }
    }

    public abstract class WorkflowSearchResponse : WorkflowResponse
    {
        public string[] OrderIds { get; set; }
        
        public override bool Validate(IWorkflowRequest request)
        {
            var isValid = base.Validate(request);
            
            if (OrderIds == null || !OrderIds.Any())
            {
                throw new ApplicationException("OrderId is mandatory");
            }

            var workflowSearchRequest = request as WorkflowSearchRequest;
            if (workflowSearchRequest != null)
            {
                if (workflowSearchRequest.OrderIds.Count() != OrderIds.Count() 
                    || workflowSearchRequest.OrderIds.Intersect(OrderIds).Count() != OrderIds.Count())
                {
                    throw new ApplicationException("OrderIds do not match request");
                }
            }

            return isValid;
        }
    }
}
