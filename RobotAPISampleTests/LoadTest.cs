using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UiAutomation.Logic.Automation;
using System.Linq;

namespace UiAutomationTests
{
    [TestClass]
    public class LoadTest
    {
        [TestMethod]
        public void LoadTest10()
        {
            ExecuteLoadTest(10);
        }

        [TestMethod]
        public void LoadTest100()
        {
            ExecuteLoadTest(100);
        }

        [TestMethod]
        public void LoadTest1000()
        {
            ExecuteLoadTest(1000);
        }

        [TestMethod]
        public void LoadTest10000()
        {
            ExecuteLoadTest(10000);
        }

        private void ExecuteLoadTest(int count)
        {
            var citrixIsRunning = Landonline.EnsureCitrixRunning();
            Assert.IsTrue(citrixIsRunning, "Unable to launch Citrix");

            var rand = new Random();
            var request = new TitleSearchRequest[count];
            for (var i = 0; i < count; i++)
            {
                request[i] = new TitleSearchRequest
                {
                    TitleReference = $"invalid{i}",
                    Type = (UiAutomation.Logic.Workflows.LINZTitleSearchType)rand.Next(0, 4),
                    OrderId = $"Order{i}"
                };
            }

            Landonline.ExecuteTitleSearch(request);

            
            Assert.IsTrue(request.All(r => r.Errors != null && r.Errors.Count() == 1), "Unexpected errors returned");
            Assert.IsTrue(request.All(r => r.Warnings == null || r.Warnings.Count() == 0), "Unexpected warnings returned");
            Assert.IsTrue(request.All(r => !r.Success), "Unexpected warnings returned");


            //Assert.AreEqual(count, response.OrderIds.Count(), "Wrong number of results/orderIds returned");
            //Assert.IsTrue(request.All(r => r.Errors.Count() == 1), "Unexpected error count per request");
            //Assert.AreEqual(WorkflowStatus.Finished, response.WorkflowStatus, "Workflow did not successfully finish");
            //Assert.IsFalse(response.Success); // Because none of the searches were valid
        }
    }
}
