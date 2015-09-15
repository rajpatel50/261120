using System;
using System.Linq;
using Microsoft.Practices.Unity;
using Xiap.Framework.DecisionTable;
using XIAP.Frontend.Infrastructure;
using XIAP.Frontend.Infrastructure.Search;
using XIAP.FrontendModules.Application;
using XIAP.FrontendModules.Claims.Model;
using XIAP.FrontendModules.Claims.Search.Model;
using XIAP.FrontendModules.Claims.Service;
using XIAP.FrontendModules.Common;
using XIAP.FrontendModules.Common.SearchService;

namespace GeniusX.AXA.FrontendModules.Claims.Search.Controller
{
    public class AXAClaimPopupSearchController : XIAP.FrontendModules.Claims.Search.Controller.ClaimSearchController
    {
        private ClaimSearchModel claimSearchModel;
        private AppModel appModel;
        private string reference = string.Empty;

        public AXAClaimPopupSearchController(ApplicationModel applicationModel, IUnityContainer container, IClaimClientService claimService, AppModel appModel, ISearchServiceHandler searchService, ClaimSearchModel searchFilterModel, IMetadataClientService metadataService)
            : base(applicationModel, claimService, appModel, searchService, searchFilterModel, metadataService,container)
        {
            this.claimSearchModel = searchFilterModel;
            this.appModel = appModel;
        }

        public ClaimSearchModel ClaimSearchModel
        {
            get
            {
                return this.claimSearchModel;
            }
        }

        public override void Validate(XIAP.Frontend.Infrastructure.ValidateSearchResults plainSearchCompleted, bool isValidate)
        {
            this.ClaimSearchModel.SelectedResultsRow.ReferenceValue = this.GenerateClaimReference(this.ClaimSearchModel.SelectedResultsRow.ReferenceValue);
            if (this.reference.Equals(this.ClaimSearchModel.SelectedResultsRow.ReferenceValue))
            {
                plainSearchCompleted(null);
                return;
            }

            this.reference = this.ClaimSearchModel.SelectedResultsRow.ReferenceValue;
            base.Validate(plainSearchCompleted, isValidate);
        }

        protected override void PlainSearchCompleted(XIAP.FrontendModules.Common.SearchService.SearchData searchData)
        {
            SearchResultRow row = null;
            if (searchData != null && !searchData.SearchResultRowList.IsNullOrEmpty() && searchData.SearchResultRowList.Count == 1)
            {
                row = searchData.SearchResultRowList.SingleOrDefault();
            }

            base.PlainSearchCompleted(searchData);
            string claimReference = row != null ? row.Columns.First(r => r.ColumnName == "ClaimReference").Value.ToString() : this.reference;
            if (this.SearchPopupAction != null)
            {
                BulkEventEntryRow bulkEventRow = this.SearchPopupModel.ControlDataContext as BulkEventEntryRow;
                if (bulkEventRow != null)
                {
                    bulkEventRow.ClaimHeaderReference = claimReference;
                }
            }
        }

        private string GenerateClaimReference(string claimReference)
        {
            long referenceNumber = 0;

            // Validations.
            if (string.IsNullOrWhiteSpace(claimReference))
            {
                return string.Empty;
            }
            else
            {
                claimReference = claimReference.Trim();
            }

            if (claimReference.Length > 7)
            {
                return claimReference;
            }

            if (!long.TryParse(claimReference, out referenceNumber))
            {
                return claimReference;
            }

            // Get the reference prefix from the decision table.
            string referencePrefix = this.GetReferencePrefix();

            // Build the claim reference string.
            string reference = referencePrefix + referenceNumber.ToString("D7");

            return reference;
        }

        private string GetReferencePrefix()
        {
            IDecisionTableComponent decisionTableComponent = null;
            string customCode01 = this.AppModel.UserProfile.CustomCode01;
            this._AppModel.DecisionTableHelper.TryCall(AXAClaimConstants.CLAIMREFERENCEPREFIX_DECISIONTABLE, DateTime.Today, out decisionTableComponent, customCode01);
            if (decisionTableComponent.Action1 != null && !string.IsNullOrWhiteSpace(decisionTableComponent.Action1.ToString()))
            {
                return decisionTableComponent.Action1.ToString().ToUpper();
            }

            return string.Empty;
        }
    }
}
