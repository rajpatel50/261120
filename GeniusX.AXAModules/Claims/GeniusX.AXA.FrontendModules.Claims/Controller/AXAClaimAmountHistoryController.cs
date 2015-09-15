using System;
using Microsoft.Practices.Unity;
using XIAP.Frontend.Infrastructure;
using XIAP.Frontend.Infrastructure.Search;
using XIAP.FrontendModules.Claims.Controller;
using XIAP.FrontendModules.Claims.Model;
using XIAP.FrontendModules.Claims.Service;
using XIAP.FrontendModules.Common;

namespace GeniusX.AXA.FrontendModules.Claims.Controller
{
    public class AXAClaimAmountHistoryController : ClaimAmountHistoryController
    {
        public AXAClaimAmountHistoryController(IUnityContainer container, IClaimClientService claimService, AppModel appModel, ISearchServiceHandler searchService, ClaimAmountHistoryModel searchFilterModel, IMetadataClientService metadataService)
            : base(claimService, appModel, searchService, searchFilterModel, metadataService, container)
        {
        }

        public event EventHandler OnPaymentCancellation;

        protected override void PaymentCancelled()
        {
            if (this.OnPaymentCancellation != null)
            {
                this.OnPaymentCancellation(this, new EventArgs());
            }
        }
    }
}
