using System.ServiceModel;
using UiAutomation.Contract.Models;

namespace UiAutomation.Contract
{
    [ServiceContract]
    [ServiceKnownType(typeof(LINZTitleSearchType))]
    public interface ILINZTitleAutomationService
    {
        [OperationContract]
        LINZTitleSearchBatch ExecuteTitleSearch(LINZTitleSearchBatch request);
    }
}
