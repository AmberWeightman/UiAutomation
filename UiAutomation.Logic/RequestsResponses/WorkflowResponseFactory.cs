using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UiAutomation.Logic.Workflows;

namespace UiAutomation.Logic.RequestsResponses
{
    public static class WorkflowResponseFactory
    {
        // TODO this is being called from 2 different places, one of which is probably redundant...
        public static WorkflowResponse Create(WorkflowType type, WorkflowResponse copyFrom = null)
        {
            WorkflowResponse workflowResponse;
            switch (type)
            {
                case WorkflowType.CheckCitrixAvailable:
                    {
                        workflowResponse = new WorkflowResponse(); // Does not have its own response type
                        CopyFrom(workflowResponse, copyFrom);
                        break;
                    }
                case WorkflowType.ChromeDownloadCitrix:
                    {
                        workflowResponse = new WorkflowResponse(); // Does not have its own response type
                        CopyFrom(workflowResponse, copyFrom);
                        break;
                    }
                case WorkflowType.LINZTitleSearch:
                    {
                        workflowResponse = new LINZTitleSearchWorkflowResponse();
                        CopyFrom(workflowResponse, copyFrom);

                        if (copyFrom != null)
                        {
                            if (copyFrom.Output.ContainsKey("SearchResults"))
                            {
                                var searchResults = JsonConvert.DeserializeObject<Dictionary<string, string>>(copyFrom.Output["SearchResults"].ToString(), new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None });
                                if (searchResults.Any())
                                {
                                    ((LINZTitleSearchWorkflowResponse)workflowResponse).SearchResults = new List<SearchResults>();
                                    foreach (var searchResult in searchResults)
                                    {
                                        ((LINZTitleSearchWorkflowResponse)workflowResponse).SearchResults.Add(new SearchResults(searchResult));
                                    }
                                }
                            }

                            if (copyFrom.Output.ContainsKey("FileNames"))
                            {
                                ((LINZTitleSearchWorkflowResponse)workflowResponse).FileNames = JsonConvert.DeserializeObject<List<string>>(copyFrom.Output["FileNames"].ToString(), new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None });
                            }
                        }
                        break;
                    }

                default:
                    throw new ApplicationException($"Workflow type {type} is not recognised.");
            }

            return workflowResponse;
        }

        private static void CopyFrom(WorkflowResponse copyToTarget, WorkflowResponse copyFromSource = null)
        {
            if (copyFromSource == null) return;
            copyToTarget.State = copyFromSource.State;
            copyToTarget.Error = copyFromSource.Error;
            copyToTarget.Output = copyFromSource.Output;
            copyToTarget.Token = copyFromSource.Token;
            copyToTarget.WorkflowFile = copyFromSource.WorkflowFile;

            if (copyToTarget is WorkflowResponse && copyFromSource.Output.ContainsKey("Success"))
            {
                bool success;
                Boolean.TryParse(copyFromSource.Output["Success"].ToString(), out success);
                ((WorkflowResponse)copyToTarget).Success = success;
            }

            if (copyToTarget is WorkflowSearchResponse && copyFromSource.Output.ContainsKey("OrderIds"))
            {
                ((WorkflowSearchResponse)copyToTarget).OrderIds = JsonConvert.DeserializeObject<string[]>(copyFromSource.Output["OrderIds"].ToString(), new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None });
            }
        }
    }
}
