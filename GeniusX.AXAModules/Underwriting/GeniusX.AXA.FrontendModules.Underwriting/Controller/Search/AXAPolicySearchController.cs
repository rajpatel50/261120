using GeniusX.AXA.FrontendModules.Underwriting.Model.Search;
using Microsoft.Practices.Unity;
using Xiap.Metadata.Data.Enums;
using XIAP.Frontend.Infrastructure;
using XIAP.Frontend.Infrastructure.Names;
using XIAP.Frontend.Infrastructure.Search;
using XIAP.FrontendModules.Application;
using XIAP.FrontendModules.Common;
using XIAP.FrontendModules.Underwriting.Controller.Search;
using XIAP.FrontendModules.Underwriting.Search;
using XIAP.FrontendModules.Underwriting.Service;

namespace GeniusX.AXA.FrontendModules.Underwriting.Controller.Search
{
    public class AXAPolicySearchController : UWSearchController
    {
        private AXAPolicySearchModel _model = null;

        public AXAPolicySearchController(ApplicationModel applicationModel, IUnityContainer container, IRiskService riskService, AppModel appModel, ISearchServiceHandler searchService, AXAPolicySearchModel searchFilterModel, IIDSearches idSearch)
            : base(applicationModel, riskService, appModel, searchService, searchFilterModel, container, idSearch)
        {
            this._model = searchFilterModel;
            this.SetUnderwritingStage();
        }

        private void SetUnderwritingStage()
        {
            UWSearchFilterData filterdata = this._model.SearchFilterData as UWSearchFilterData;
            if (filterdata != null)
            {
                filterdata.UnderwritingStage = (short)StaticValues.UnderwritingStage.Policy;
                filterdata.DefaultUnderWritingStage = (short)StaticValues.UnderwritingStage.Policy;
            } 
        }

        public override void Validate(ValidateSearchResults plainSearchCompleted, bool isValidate)
        {
            if (this._model.FilterData.AvailableProducts.IsNullOrEmpty())
            {
                CodeLookupService.GetProductCodeList((obj, args) =>
                {
                    this._model.FilterData.SetCodeItems(args.Result);
                    base.Validate(plainSearchCompleted, isValidate);
                });
            }
            else
            {
                base.Validate(plainSearchCompleted, isValidate);
            }
        }
    }
}
