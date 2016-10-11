using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using UiAutomation.Contract.Models;
using UiAutomation.Service;
using System.Diagnostics;
using UiAutomation.Logic.Automation;

namespace UiAutomationTests
{
    [TestClass]
    public class LoadTest
    {
        // TODO tests are required with various initial states set
        // i.e. chrome closed; chrome open but landonline not open; landonline open; landonline open and download previously performed

        [TestMethod]
        public void LoadTest2()
        {
            ExecuteLoadTest(2);
        }

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

        private void ExecuteLoadTest(int count)
        {
            var autoOrderBulkId = $"LoadTest_{count}_{DateTime.Now.ToString("yyyyMMddHHmmss")}";
            
            var rand = new Random();
            var request = new LINZTitleSearch[count];
            for (var i = 0; i < count; i++)
            {
                request[i] = new LINZTitleSearch
                {
                    TitleReference = $"invalid{i}",
                    Type = (LINZTitleSearchType)rand.Next(0, 4),
                    OrderId = $"Order{i}"
                };
            }

            var service = new LINZTitleAutomationService();
            service.ExecuteTitleSearch(new LINZTitleSearchBatch { TitleSearchRequests = request, AutoOrderBulkId = autoOrderBulkId});

            //Landonline.ExecuteTitleSearch(request, autoOrderBulkId);

            Assert.IsTrue(request.All(r => r.Errors != null && r.Errors.Count() == 1), $"Unexpected errors returned: {string.Join("; ", request.Select(r => r.Errors == null ? "null" : string.Join(", ", r.Errors)))  }");
            Assert.IsTrue(request.All(r => r.Warnings == null || r.Warnings.Count() == 0), $"Unexpected warnings returned: {string.Join("; ", request.Select(r => r.Warnings == null ? "null" : string.Join(", ", r.Warnings)))  }");
            Assert.IsTrue(request.All(r => !r.Success));
        }
    }
}
