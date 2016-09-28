using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UiAutomation.Logic.Workflows;

namespace UiAutomation.Logic.RequestsResponses
{
    public enum WorkflowStatus
    {
        Unknown,
        Processing,
        Finished,
    }

    public class WorkflowResponse : EventArgs, IWorkflowResponse
    {
        /// <summary>
        /// Don't rely on this unless it has been explicitly set in the workflow - it's not a default field
        /// </summary>
        public bool Success { get; set; }

        public ActivityInstanceState State { get; set; }

        public Exception Error { get; set; }

        public IDictionary<string, object> Output { get; set; }

        public Guid Token { get; set; }

        public string WorkflowFile { get; set; }
        
        /// <summary>
        /// Did execution of the workflow complete fully?
        /// </summary>
        public WorkflowStatus WorkflowStatus
        {
            get
            {
                if (Output != null && Output.ContainsKey("WorkflowStatus"))
                {
                    WorkflowStatus status;
                    Enum.TryParse(Output["WorkflowStatus"].ToString(), out status);
                    return status;
                }
                return WorkflowStatus.Unknown;
            } 
        }

        public List<Message> ErrorMessages
        {
            get
            {
                if (Output == null || !Output.ContainsKey("ErrorMessages"))
                    return new List<Message>();

                // Where each incoming message has [errCode, args]
                var receivedMessages = JsonConvert.DeserializeObject<List<KeyValuePair<string, object[]>>>(Output["ErrorMessages"].ToString(), new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None });
                return receivedMessages.Select(message => Message.Create(MessageType.Error, message.Key, message.Value)).ToList();
            }
        }

        public List<Message> WarningMessages
        {
            get
            {
                if (Output == null || !Output.ContainsKey("WarningMessages"))
                    return new List<Message>();

                // Where each incoming message has [errCode, args]
                var receivedMessages = JsonConvert.DeserializeObject<List<KeyValuePair<string, object[]>>>(Output["WarningMessages"].ToString(), new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None });
                return receivedMessages.Select(message => Message.Create(MessageType.Warning, message.Key, message.Value)).ToList();
            }
        }
        
        public virtual bool Validate(IWorkflowRequest request)
        {
            if (Error != null)
            {
                // If there's already an exception, any other error is redundant
                throw (Error);
            }
            
            if (WorkflowStatus != WorkflowStatus.Finished)
            {
                throw new ApplicationException("Robot job failed to properly execute");
            }

            if (!Output.ContainsKey("Success"))
            {
                throw new ApplicationException("Please review workflow script - it is not returning Success");
            }
            if (!Output.ContainsKey("WorkflowStatus"))
            {
                throw new ApplicationException("Please review workflow script - it is not returning WorkflowStatus");
            }
            if (!Output.ContainsKey("ErrorMessages"))
            {
                throw new ApplicationException("Please review workflow script - it is not returning ErrorMessages");
            }
            if (!Output.ContainsKey("WarningMessages"))
            {
                throw new ApplicationException("Please review workflow script - it is not returning WarningMessages");
            }
            return true;
        }
        
    }
}
