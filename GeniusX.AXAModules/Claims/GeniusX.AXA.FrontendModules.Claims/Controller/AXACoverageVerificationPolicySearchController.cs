using System;
using GeniusX.AXA.FrontendModules.Claims.Model;
using Microsoft.Practices.Unity;
using XIAP.Frontend.Infrastructure;
using XIAP.Frontend.Infrastructure.Configuration;
using XIAP.FrontendModules.Application;
using XIAP.FrontendModules.Claims.Controller;
using XIAP.FrontendModules.Claims.Service;
using XIAP.FrontendModules.Common.ClaimService;
using XIAP.FrontendModules.Common;

namespace GeniusX.AXA.FrontendModules.Claims.Controller
{
    public class AXACoverageVerificationPolicySearchController : CoverageVerificationPolicySearchController
    {
        private IUnityContainer _container;

        public AXACoverageVerificationPolicySearchController(ApplicationModel applicationModel, AppModel appModel, IMetadataClientService metadataService, AXACoverageVerificationPolicySearchModel PolicySearchModel, IUnityContainer container, ClaimClientService claimClientService)
            : base(applicationModel, appModel, metadataService, PolicySearchModel, claimClientService, container)
        {
            this._container = container;
            PolicySearchModel.CoverageVerificationSearchControllerName = "AXACoverageVerificationPolicySearchController";
        }

        protected override void IsPolicyExist(bool isPolicyExist)
        {
            base.IsPolicyExist(isPolicyExist);
            if (!isPolicyExist)
            {
                throw new InvalidOperationException(GeniusX.AXA.FrontendModules.Claims.Resources.StringResources.Attachment_Not_Allowed);
            }
        }

        protected override void GetPolicyDataCompleted(UWHeaderData headerData)
        {
            ConfigurationManager configurationManager = this._container.Resolve<ConfigurationManager>();
            string policyVerifiedStatus = configurationManager.GetValue(AXAClaimConstants.POLICY_VERIFIED_HEADERSTATUS);

            if (headerData == null)
            {
                throw new InvalidOperationException(GeniusX.AXA.FrontendModules.Claims.Resources.StringResources.Attachment_Not_Allowed);
            }
            else if (headerData != null && headerData.HeaderStatusCode != policyVerifiedStatus)
            {
                throw new InvalidOperationException(string.Format(GeniusX.AXA.FrontendModules.Claims.Resources.StringResources.Policy_Not_Verified, headerData.HeaderReference));
            }

            if (this.Model != null)
            {
                base.GetPolicyDataCompleted(headerData);
            }
        }
    }
}
