using GeniusX.AXA.FrontendModules.Claims.Model;
using Microsoft.Practices.Unity;
using XIAP.FrontendModules.Claims.Search.Controller;
using XIAP.FrontendModules.Claims.Search.Views;
using XIAP.FrontendModules.Claims.Service;

namespace GeniusX.AXA.FrontendModules.Claims.Controller
{
    public class AXAClaimTransactionPreviewController : ClaimTransactionPreviewController
    {
       public AXAClaimTransactionPreviewController(IClaimClientService service, IUnityContainer container, AXAClaimTransactionPreviewModel previewModel, ClaimTransactionPreview preview)
            : base(service, previewModel,  container)
        {
        }
    }
}
