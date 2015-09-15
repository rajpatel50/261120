using GeniusX.AXA.FrontendModules.Claims.Model;
using Microsoft.Practices.Unity;
using XIAP.Frontend.Infrastructure;
using XIAP.FrontendModules.Application;
using XIAP.FrontendModules.Claims.Controller;
using XIAP.FrontendModules.Claims.Service;
using XIAP.FrontendModules.Common;

namespace GeniusX.AXA.FrontendModules.Claims.Controller
{
    public class AXAClaimReopenCloseController : ClaimReopenCloseController
    {
        public AXAClaimReopenCloseController(ApplicationModel applicationModel, IClaimClientService claimClientService, IMetadataClientService metadataService, AXAClaimReopenCloseModel model, IUnityContainer container, AppModel appModel)
            : base(applicationModel, claimClientService, metadataService, model, appModel, container)
        {
        }
    }
}
