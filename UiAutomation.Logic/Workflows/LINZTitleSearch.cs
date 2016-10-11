using UiAutomation.Logic.RequestsResponses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UiAutomation.Contract.Models;
using UiAutomation.Logic.Automation;

namespace UiAutomation.Logic.Workflows
{

    public class LINZTitleSearchWorkflowRequest : WorkflowSearchRequest
    {
        // Use arrays to enforce order between TitleReferences, Types and OrderIds
        public string[] TitleReferences { get; set; }

        public LINZTitleSearchType[] Types { get; set; }

        private Dictionary<string, LINZTitleSearch> _sourceRequests;

        public string OutputDirectoryTemp => LandonlineAutomator.CitrixOutputDirectoryTemp;

        public LINZTitleSearchWorkflowRequest(LINZTitleSearch[] titleSearchRequests, string screenshotBaseDirectory, string screenshotProcessSubDirectory) : base(screenshotBaseDirectory, screenshotProcessSubDirectory, WorkflowType.LINZTitleSearch.ToString())
        {
            if (!titleSearchRequests.Any()) return;

            _sourceRequests = titleSearchRequests.ToDictionary(a => a.OrderId, a => a);

            var requestCount = titleSearchRequests.Count();
            var titleReferences = new string[requestCount];
            var types = new LINZTitleSearchType[requestCount];
            var orderIds = new string[requestCount];

            for (var i = 0; i < requestCount; i++)
            {
                titleReferences[i] = titleSearchRequests[i].TitleReference;
                types[i] = titleSearchRequests[i].Type;
                orderIds[i] = titleSearchRequests[i].OrderId;

                if (string.IsNullOrEmpty(titleSearchRequests[i].OutputDirectory))
                {
                    titleSearchRequests[i].OutputDirectory = _defaultOutputDirectory;
                }
            }

            TitleReferences = titleReferences;
            Types = types;
            OrderIds = orderIds;
        }

        public LINZTitleSearch GetSourceRequest(string orderId)
        {
            return (_sourceRequests == null || !_sourceRequests.ContainsKey(orderId)) ? null : _sourceRequests[orderId];
        }

        public override bool Validate()
        {
            var isValid = base.Validate();

            if (TitleReferences == null || !TitleReferences.Any())
            {
                throw new ApplicationException("At least one title reference is required.");
            }
            if (TitleReferences.Count() != OrderIds.Count())
            {
                throw new ApplicationException("Mismatched OrderId/TitleReference count.");
            }
            if (Types.Count() != OrderIds.Count())
            {
                throw new ApplicationException("Mismatched OrderId/Type count.");
            }
           
            return isValid;
        }
    }

    public class LINZTitleSearchWorkflowResponse : WorkflowSearchResponse
    {
        public List<SearchResults> SearchResults { get; set; }

        public List<string> FileNames { get; set; }

        public override bool Validate(IWorkflowRequest request)
        {
            var isValid = base.Validate(request);

            var workflowSearchRequest = request as LINZTitleSearchWorkflowRequest;
            if (workflowSearchRequest != null)
            {
                if (FileNames != null && FileNames.Any())
                {
                    // Check that the files actually exist, because we can't trust the workflow for anything
                    for (var i = 0; i < FileNames.Count; i++)
                    {
                        var fileName = FileNames[i];
                        if (string.IsNullOrEmpty(fileName))
                        {
                            continue;
                        }

                        var orderId = fileName.Split('_')[0];
                        var associatedSourceRequest = workflowSearchRequest.GetSourceRequest(orderId);
                        if (associatedSourceRequest == null)
                        {
                            throw new ApplicationException($"Could not find source request for file {fileName}.");
                        }

                        if (string.IsNullOrEmpty(associatedSourceRequest.OutputDirectory))
                        {
                            throw new ApplicationException($"Output directory not set.");
                        }

                        // Unable to saved to a network file location from within Citrix
                        var tempFilePath = $"{workflowSearchRequest.OutputDirectoryTemp}{fileName}";
                        if (!File.Exists(tempFilePath))
                        {
                            throw new ApplicationException($"Failed to successfully create file {tempFilePath}.");
                        }
                        var filePath = $"{associatedSourceRequest.OutputDirectory}{fileName}";
                        File.Move(tempFilePath, filePath);
                        
                        // Update the source request with the saved file name
                        if (associatedSourceRequest != null)
                        {
                            associatedSourceRequest.Success = true;
                            associatedSourceRequest.AddOutputFilePath(filePath);
                        }
                    }
                }

                if (SearchResults != null )
                {
                    // Infill search results
                    foreach (var searchResult in SearchResults)
                    {
                        var associatedSourceRequest = workflowSearchRequest.GetSourceRequest(searchResult.OrderId);
                        if (associatedSourceRequest != null)
                        {
                            associatedSourceRequest.SearchResults = searchResult.Results;
                        }
                    }
                }

                if (WarningMessages != null)
                {
                    foreach (var message in WarningMessages.Where(w => !string.IsNullOrEmpty(w.OrderId)))
                    {
                        var associatedSourceRequest = workflowSearchRequest.GetSourceRequest(message.OrderId);
                        if (associatedSourceRequest != null)
                        {
                            if (associatedSourceRequest.Warnings == null) associatedSourceRequest.Warnings = new List<string>();
                            associatedSourceRequest.Warnings.Add(message.Text);// = 
                        }
                    }
                }

                if (ErrorMessages != null)
                {
                    foreach (var message in ErrorMessages.Where(w => !string.IsNullOrEmpty(w.OrderId)))
                    {
                        var associatedSourceRequest = workflowSearchRequest.GetSourceRequest(message.OrderId);
                        if (associatedSourceRequest != null)
                        {
                            if (associatedSourceRequest.Errors == null)
                            {
                                associatedSourceRequest.Errors = new List<string> { message.Text };
                                //associatedSourceRequest.Exception = new ApplicationException(associatedSourceRequest.Errors.First());
                                associatedSourceRequest.ExceptionType = new ApplicationException().GetType().Name;
                                associatedSourceRequest.ExceptionMsg = associatedSourceRequest.Errors.First();
                            }
                            else
                            {
                                associatedSourceRequest.Errors.Add(message.Text);
                            }
                        }
                    }
                }
            }
            
            return isValid;
        }    
    }
    
    public class LINZTitleSearchWorkflow : RobotWorkflowBase<LINZTitleSearchWorkflowRequest, LINZTitleSearchWorkflowResponse>
    {
        public  override string WorkflowFile => WorkflowFiles.LINZTitleSearchWorkflow;

        public override WorkflowType WorkflowType => WorkflowType.LINZTitleSearch;

        public override int MaxWorkflowDurationMins => 15; // must be suitable for the max number of title searches tht will be performed in any one batch (currently paged in blocks of 20)
    }
    
}
