using GeniusX.AXA.FrontendModules.Underwriting.Model.Search;
using Microsoft.Practices.Unity;
using XIAP.Frontend.Infrastructure;
using XIAP.Frontend.Infrastructure.Search;
using XIAP.FrontendModules.Application;
using XIAP.FrontendModules.Underwriting.Controller.Search;
using XIAP.FrontendModules.Underwriting.Service;
using XIAP.Frontend.Infrastructure.Names;
using XIAP.FrontendModules.Common;


namespace GeniusX.AXA.FrontendModules.Underwriting.Controller.Search
{
    /// <summary>
    /// The AXA Specific CoverageVerificationPolicySearchController
    /// </summary>
    public class AXACoverageVerificationPolicySearchController : CoverageVerificationPolicySearchController
    {
        public AXACoverageVerificationPolicySearchController(ApplicationModel applicationModel, IUnityContainer container, IRiskService riskService, IMetadataClientService metadataService, AppModel appModel, ISearchServiceHandler searchService, AXACoverageVerificationPolicyPopupModel searchFilterModel, IIDSearches idSearch)
            : base(applicationModel, riskService, appModel, searchService,metadataService, searchFilterModel,container, idSearch)
        {
        }

        /// <summary>
        /// Show pop up dialog
        /// </summary>
        /// <param name="dialogButtonCallBack">Dialogue button call back </param>
        public override void ShowPopupDialog(DialogButtonCallBack dialogButtonCallBack)
        {
            if (this.CoverageVerificationPolicySearchPopupModel.IsExternalPolicySearch == true)
            {
                this.CoverageVerificationPolicySearchPopupModel.UWSearchFields.InsuredField.Readonly = true;
            }

            base.ShowPopupDialog(dialogButtonCallBack);
        }
    }
}
