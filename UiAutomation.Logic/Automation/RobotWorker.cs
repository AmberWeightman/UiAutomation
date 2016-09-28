using Newtonsoft.Json;
using UiAutomation.Logic.Automation.UiPath;
using UiAutomation.Logic.RequestsResponses;
using UiAutomation.Logic.Workflows;
using System;
using System.Threading;

namespace UiAutomation.Logic.Automation
{
    public interface IRobotWorker
    {
        void RequestStop();

        void SetResult(WorkflowResponse response);

        WorkflowType WorkflowType { get; }

        Guid? RobotJobGuid { get; }
    }

    public class RobotWorker<TRequest, TResponse> : IRobotWorker, IDisposable
        where TRequest : WorkflowRequest
        where TResponse : WorkflowResponse, new()
    {
        private static string _user = @"INFOTRACK\amber.weightman";

        private static int _syncExecutionTimeoutMins = 10;

        //private string _robotWorkflowPath { get; set; }
        public TRequest InputArguments { get; set; }
        public TResponse Result { get; private set; }
        public bool IsActive { get; set; }

        public Guid? RobotJobGuid { get; private set; }
        
        private RobotClient _robotClient = null;
        
        public WorkflowType WorkflowType { get; private set; }
        
        internal WorkflowResponse ExecuteRobotJobSynchronously<TWorkflow>(TWorkflow robotWorkflowBase)
            where TWorkflow : RobotWorkflowBase<TRequest, TResponse>
        {
            if (Threading.ActiveRobotWorker != null)
            {
                throw new ApplicationException("Can only have one active robot thread.");
            }
            Threading.ActiveRobotWorker = this;

            var workerThread = Threading.CreateRobotThread(() => ExecuteRobotJob(robotWorkflowBase.WorkflowFile));
            if (workerThread == null)
            {
                throw new ApplicationException("Unable to launch robot thread at this time.");
            }
            
            workerThread.Start();

            // Loop until worker thread activates. 
            while (!workerThread.IsAlive);

            workerThread.Join(TimeSpan.FromMinutes(robotWorkflowBase.MaxWorkflowDurationMins));

            if (IsActive) // if the worker is still active, waiting has timed out, so stop it
            {
                RequestStop();
                Result = new TResponse();
                Result.Error = new ApplicationException($"Workflow process {robotWorkflowBase.WorkflowType} timed out");
            }

            return Result;
        }
        
        // Volatile is used as hint to the compiler that this data 
        // member will be accessed by multiple threads. 
        private volatile bool _shouldStop;

        public RobotWorker(TRequest inputArguments, WorkflowType workflowType)
        {
            InputArguments = inputArguments;
            WorkflowType = workflowType;
        }
        
        public void ExecuteRobotJob(string robotWorkflowPath)
        {
            IsActive = true;
            
            var resp = ExecuteRobotJobAsync(InputArguments, robotWorkflowPath);

            while (!_shouldStop)
            {
                // TODO timeout? (Although the parent thread does have a timeout after which it will tell this thread to terminate)
            }

            //Console.WriteLine("worker thread: terminating gracefully.");
            IsActive = false;

            //Threading.CloseRobotThread();

        }

        public void RequestStop()
        {
            _shouldStop = true;
        }

        public void SetResult(WorkflowResponse response)
        {
            var result = WorkflowResponseFactory.Create(WorkflowType, response);
            Result = result as TResponse;
        }

        private Guid ExecuteRobotJobAsync(TRequest inputArguments, string robotWorkflowPath)
        {
            _robotClient = new RobotClient();
            var job = new RobotClientJob(_robotClient)
            {
                WorkflowFile = robotWorkflowPath,
                InputArguments = inputArguments,
                User = _user,
                Type = 0,
            };
           
            RobotJobGuid = job.Start();
            
            return RobotJobGuid.Value;
        }

        public class RobotClientJob
        {
            public string WorkflowFile { get; set; }
            public TRequest InputArguments { get; set; }
            public TResponse OutputArguments { get; set; }
            public string User { get; set; }
            public int Type { get; set; }
            private RobotClient _robotClient;
            
            public RobotClientJob(RobotClient robotClient)
            {
                _robotClient = robotClient;
            }

            internal Guid Start()
            {
                var serialisedJob = JsonConvert.SerializeObject(this);
                var guid = _robotClient.StartJob(serialisedJob);
                return guid;
            }
        }
        
        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                var wasActive = (this == Threading.ActiveRobotWorker);
                if (disposing)
                {
                    // Dispose managed state (managed objects).
                    Threading.ActiveRobotWorker = null;
                }

                // Free unmanaged resources (unmanaged objects) and (TODO) override a finalizer below.
                if (wasActive && RobotJobGuid.HasValue)
                {
                    // Ask the UiRobot to close gracefully
                    _robotClient.CancelJob(RobotJobGuid.Value);
                    _robotClient.RemoveJob(RobotJobGuid.Value);

                    // TODO Should I sleep or wait? How long does this take? I don't know...

                    // The robot might still be running, or have child threads running, which need to be killed off. Don't trust it to handle its own memory.
                    Threading.KillRobotThread();
                }
               
                // TODO: set large fields to null.
                
                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
         ~RobotWorker() {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
         }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            GC.SuppressFinalize(this);
        }
        #endregion
    }


}
