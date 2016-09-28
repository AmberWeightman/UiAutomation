using UiAutomation.Logic.Automation;
using UiAutomation.Logic.RequestsResponses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UiAutomation.Logic.Workflows
{
    public enum LINZTitleSearchType
    {
        TitleSearchWithDiagram,
        TitleSearchNoDiagram,
        Historical,
        Guaranteed,
    }

    public class LINZTitleSearchWorkflowRequest : WorkflowSearchRequest
    {
        // Use arrays to enforce order between TitleReferences, Types and OrderIds
        public string[] TitleReferences { get; set; }

        public LINZTitleSearchType[] Types { get; set; }

        private Dictionary<string, TitleSearchRequest> _sourceRequests;

        public LINZTitleSearchWorkflowRequest(TitleSearchRequest[] titleSearchRequests)
        {
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
            }

            TitleReferences = titleReferences;
            Types = types;
            OrderIds = orderIds;
        }

        public TitleSearchRequest GetSourceRequest(string orderId)
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
                    foreach (var fileName in FileNames)
                    {
                        var filePath = $"{WorkflowSearchRequest.OutputDirectory}{fileName}";
                        if (!File.Exists(filePath))
                        {
                            throw new ApplicationException($"Failed to successfully create file {filePath}.");
                        }

                        // Update the source request with the saved file name
                        var orderId = fileName.Split('_')[0];
                        var associatedSourceRequest = workflowSearchRequest.GetSourceRequest(orderId);
                        if (associatedSourceRequest != null)
                        {
                            associatedSourceRequest.Success = true;
                            associatedSourceRequest.OutputFilePath = filePath;
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
                            if (associatedSourceRequest.Errors == null) associatedSourceRequest.Errors = new List<string>();
                            associatedSourceRequest.Errors.Add(message.Text);
                        }
                    }
                }
            }
            
            return isValid;
        }
        
    }

    public class SearchResults
    {
        public string OrderId { get; set; }
        public List<SearchResult> Results { get; set; }

        public SearchResults(KeyValuePair<string, string> data)
        {
            OrderId = data.Key;
            Results = new List<SearchResult>();

            // All lines in data.Value (excluding the first line, which is always the header)
            var lines = data.Value.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Skip(1);
            foreach (var line in lines)
            {
                Results.Add(new SearchResult(line));
            }
        }
    }

    public class SearchResult
    {
        public string CT { get; set; }
        public string Owner { get; set; }
        public string Status { get; set; }
        public string PotentiallyMaoriLand { get; set; }
        public string LegalDescription { get; set; }
        public string IndicativeArea { get; set; }
        public string LandDistrict { get; set; }
        public string TimeshareWeek { get; set; }

        public SearchResult(string tabSeparatedData)
        {
            var values = tabSeparatedData.Split(new char[] { '\t' }, StringSplitOptions.None);
            CT = values[0];
            Owner = values[1];
            Status = values[2];
            PotentiallyMaoriLand = values[3];
            LegalDescription = values[4];
            IndicativeArea = values[5];
            LandDistrict = values[6];
            TimeshareWeek = values[7];
        }
    }
    
    public class LINZTitleSearchWorkflow : RobotWorkflowBase<LINZTitleSearchWorkflowRequest, LINZTitleSearchWorkflowResponse>
    {
        public  override string WorkflowFile => WorkflowFiles.LINZTitleSearchWorkflow;

        public override WorkflowType WorkflowType => WorkflowType.LINZTitleSearch;

        public override int MaxWorkflowDurationMins => 15; // must be suitable for the max number of title searches tht will be performed in any one batch (currently paged in blocks of 20)
    }
    
}
