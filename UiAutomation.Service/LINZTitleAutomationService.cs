using System;
using System.Linq;
using System.ServiceModel.Activation;
using UiAutomation.Contract;
using UiAutomation.Contract.Models;
using UiAutomation.Logic.Automation;

namespace UiAutomation.Service
{
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
    public class LINZTitleAutomationService : ILINZTitleAutomationService
    {
        public LINZTitleSearchBatch ExecuteTitleSearch(LINZTitleSearchBatch request)
        {
            if (request.TitleSearchRequests == null || !request.TitleSearchRequests.Any())
            {
                var e = new ArgumentNullException("requests");
                AddException(e, request);
                return request;
            }

            if (string.IsNullOrEmpty(request.AutoOrderBulkId))
            {
                request.AutoOrderBulkId = Guid.NewGuid().ToString();
            }
            
            try
            {
                var landonlineSearchAutomator = new LandonlineAutomator();

                var citrixIsRunning = landonlineSearchAutomator.EnsureCitrixRunning(request.AutoOrderBulkId, request.ScreenshotDirectoryPath);
                if (citrixIsRunning)
                {
                    landonlineSearchAutomator.ExecuteTitleSearch(request);
                }
                else
                {
                    var e = new ApplicationException("Unable to launch Citrix.");
                    AddException(e, request);
                }
            }
            catch (Exception e)
            {
                AddException(e, request);
            }

            return request;
        }

        private void AddException(Exception e, LINZTitleSearchBatch titleSearchBatch)
        {
            titleSearchBatch.ExceptionType = e.GetType().Name;
            titleSearchBatch.ExceptionMsg = e.Message;
            foreach (var titleSearchRequest in titleSearchBatch.TitleSearchRequests.Where(t => string.IsNullOrEmpty(t.ExceptionType)))
            {
                titleSearchRequest.ExceptionType = titleSearchBatch.ExceptionType;
                titleSearchRequest.ExceptionMsg = titleSearchBatch.ExceptionMsg;
            }
        }
    }

}
