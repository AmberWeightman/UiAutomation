using Newtonsoft.Json;
using UiAutomation.Logic.RequestsResponses;
using System;
using System.IO;
using System.ServiceModel;

namespace UiAutomation.Logic.Automation.UiPath
{
    /// <summary>
    /// Wrapper around the UIPath Robot Service
    /// </summary>
    [CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
    public class RobotClient : UiPathRemote.IUiPathRemoteDuplexContractCallback
    {
        // UiPath Remote Channel
        protected UiPathRemote.IUiPathRemoteDuplexContract Channel = null;
        protected DuplexChannelFactory<UiPathRemote.IUiPathRemoteDuplexContract> DuplexChannelFactory = null;

        public RobotClient()
        {
            DuplexChannelFactory = new DuplexChannelFactory<UiPathRemote.IUiPathRemoteDuplexContract>(new InstanceContext(this), "DefaultDuplexEndpoint");
            DuplexChannelFactory.Credentials.Windows.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Impersonation;
            Channel = DuplexChannelFactory.CreateChannel();
        }

        #region Service methods

        public bool IsAlive()
        {
            return Channel.IsAlive();
        }

        public Guid StartJob(string serializedJob)
        {
            var startJobResponse = Channel.StartJob(SerializeStringToStream(serializedJob));

            return Guid.Parse(startJobResponse);
        }

        public Guid StartJobAsync(string serializedJob)
        {
            var startJobResponse = Channel.StartJobAsync(SerializeStringToStream(serializedJob));
            return Guid.Parse(startJobResponse.Result);
        }

        public static Stream SerializeStringToStream(string jobValue)
        {
            if (jobValue == null) return null;

            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(jobValue);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public void CancelJob(Guid jobId)
        {
            Channel.CancelJob(jobId.ToString());
        }

        public void RemoveJob(Guid jobId)
        {
            Channel.RemoveJob(jobId.ToString());
        }

        public WorkflowResponse QueryJob(Guid jobId)
        {
            var response = Channel.QueryJob(jobId.ToString());

            Console.WriteLine($"Queried job: {response}");
            var completedResult = JsonConvert.DeserializeObject<WorkflowResponse>(response, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None });
            return completedResult;
        }

        public WorkflowResponse QueryJobAsync(Guid jobId)
        {
            var response = Channel.QueryJobAsync(jobId.ToString());

            var status = response.Status;

            Console.WriteLine($"Queried job: {response} with status {status}");
            var completedResult = JsonConvert.DeserializeObject<WorkflowResponse>(response.Result, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None });
            return completedResult;
        }
        
        #endregion

        #region Duplex callbacks

        public bool OnTrackReceived(string serializedTrackingRecord)
        {
            return false;
        }

        // Implementation can allow for invokeCompletedInfo to be anything that is serialisable
        public void OnJobCompleted(string invokeCompletedInfo)
        {
            //Console.WriteLine($"CompletedInfo: {invokeCompletedInfo}");
            var completedResult = JsonConvert.DeserializeObject<WorkflowResponse>(invokeCompletedInfo, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None });

            var worker = Threading.ActiveRobotWorker;
            
            if (completedResult.State == System.Activities.ActivityInstanceState.Faulted)
            {
                Console.WriteLine($"{worker.WorkflowType} has errors:");
                Console.WriteLine(completedResult.Error.Message);
                return;
            }
            else if(completedResult.State ==  System.Activities.ActivityInstanceState.Canceled)
            {
                Console.WriteLine($"{worker.WorkflowType} cancelled.\n");
            }
            else
            {
                Console.WriteLine($"{worker.WorkflowType} completed without fatal errors.\n");
            }

            worker.SetResult(completedResult);
            worker.RequestStop();
        }

        public void OnLog(string logMessage)
        {
        }

        public void OnPackagesUpdated()
        {
            throw new NotImplementedException();
        }

        #endregion Duplex callbacks
    }
}
