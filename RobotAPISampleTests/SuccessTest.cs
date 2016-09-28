using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UiAutomation.Logic.Automation;
using UiAutomation.Logic.Workflows;
using System.Linq;
using System.IO;

namespace UiAutomationTests
{
    [TestClass]
    public class SuccessTest
    {
        [TestMethod]
        public void TestSuccessfulExecution()
        {
            // Each successful execution charges $10, so be careful!
            //ExecuteTestSuccessfulExecution();
        }

        private void ExecuteTestSuccessfulExecution()
        {
            var citrixIsRunning = Landonline.EnsureCitrixRunning();
            Assert.IsTrue(citrixIsRunning, "Unable to launch Citrix");
            
            var request = new TitleSearchRequest[]
            {
                new TitleSearchRequest
                {
                    TitleReference = "invalid",
                    Type = LINZTitleSearchType.TitleSearchNoDiagram,
                    OrderId = "Order000"
                },
                new TitleSearchRequest
                {
                    TitleReference = "WN516/98",
                    Type = LINZTitleSearchType.Guaranteed,
                    OrderId = "Order001"
                },
                new TitleSearchRequest
                {
                    TitleReference = "invalid",
                    Type = LINZTitleSearchType.TitleSearchWithDiagram,
                    OrderId = "Order002"
                },
                new TitleSearchRequest
                {
                    TitleReference = "OT17A/765",
                    Type = LINZTitleSearchType.Historical,
                    OrderId = "Order003"
                }
            };

            Landonline.ExecuteTitleSearch(request);

            Assert.AreEqual(1, request[0].Errors.Count());
            Assert.IsTrue(request[0].Warnings == null || request[0].Warnings.Count() == 0);
            Assert.IsFalse(request[0].Success);

            Assert.AreEqual(1, request[2].Errors.Count());
            Assert.IsTrue(request[2].Warnings == null || request[2].Warnings.Count() == 0);
            Assert.IsFalse(request[2].Success);

            Assert.IsTrue(request[1].Errors == null || request[1].Errors.Count() == 0);
            Assert.IsTrue(request[1].Warnings == null || request[1].Warnings.Count() == 0);
            Assert.IsTrue(!string.IsNullOrEmpty(request[1].OutputFilePath), $"Output file path not set for request {request[1].OrderId} {request[1].TitleReference}");
            Assert.AreEqual(1, request[1].SearchResults.Count(), $"Wrong number of search results returned for request {request[1].OrderId} {request[1].TitleReference}");
            Assert.IsTrue(request[1].Success);
            Assert.IsTrue(File.Exists(request[1].OutputFilePath));

            Assert.IsTrue(request[3].Errors == null || request[3].Errors.Count() == 0);
            Assert.IsTrue(request[3].Warnings == null || request[3].Warnings.Count() == 0);
            Assert.IsTrue(!string.IsNullOrEmpty(request[3].OutputFilePath), $"Output file path not set for request {request[3].OrderId} {request[3].TitleReference}");
            Assert.AreEqual(1, request[3].SearchResults.Count(), $"Wrong number of search results returned for request {request[3].OrderId} {request[3].TitleReference}");
            Assert.IsTrue(request[3].Success);
            Assert.IsTrue(File.Exists(request[3].OutputFilePath));
        }
    }
}
