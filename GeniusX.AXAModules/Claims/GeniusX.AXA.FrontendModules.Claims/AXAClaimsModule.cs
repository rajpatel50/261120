using GeniusX.AXA.Claims.FrontendModules.Views;
using GeniusX.AXA.FrontendModules.Claims.Controller;
using GeniusX.AXA.FrontendModules.Claims.Model;
using GeniusX.AXA.FrontendModules.Claims.Navigation;
using GeniusX.AXA.FrontendModules.Claims.Search;
using GeniusX.AXA.FrontendModules.Claims.Search.Controller;
using GeniusX.AXA.FrontendModules.Claims.Search.Views;
using GeniusX.AXA.FrontendModules.Claims.Views;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Xiap.Framework.Logging;
using Xiap.Metadata.Data.Enums;
using XIAP.Frontend.Infrastructure;
using XIAP.Frontend.Infrastructure.Events;
using XIAP.Frontend.Infrastructure.Navigation;
using XIAP.Frontend.Infrastructure.Search;
using XIAP.Frontend.Infrastructure.Tree;
using XIAP.FrontendModules.Application;
using XIAP.FrontendModules.Claims.Navigation.AvilabilityBehaviours;
using XIAP.FrontendModules.Common;
using XIAP.FrontendModules.Infrastructure.Search;

namespace GeniusX.AXA.FrontendModules.Claims
{
    public class AXAClaimsModule : BaseModule, IModule
    {
        private static readonly ILogger _Logger = LoggerFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private ApplicationModel _application;
        private IUnityContainer _container;
        private IRegionManager _regionManager;
        private IEventAggregator _eventAggregator;
        private NavigationManager _navigationManager;

        public AXAClaimsModule(ApplicationModel application, IUnityContainer container, IRegionManager regionManager, IEventAggregator eventAggregator, NavigationManager navigationManager)
            : base(container, eventAggregator, navigationManager)
        {
            this._application = application;
            this._container = container;
            this._regionManager = regionManager;
            this._eventAggregator = eventAggregator;
            this._navigationManager = navigationManager;
        }

        public new void Initialize()
        {
            if (_Logger.IsInfoEnabled)
            {
                _Logger.Info("AXA Claims Module started initialisation");
            }

            this.RegisterViewsAndServices();

            // Claims Search
            this._eventAggregator.GetEvent<DuplicateSearchEvent>().Subscribe(this.ClaimsSearch, ThreadOption.UIThread, true, this.ClaimsSearchFilter);
            this._eventAggregator.GetEvent<StartUpCompleteEvent>().Publish("AXA");

            if (_Logger.IsInfoEnabled)
            {
                _Logger.Info("AXA Claims Module completed initialisation");
            }

            base.Initialize();
        }

        public void ClaimsSearch(SearchContext searchContext)
        {
            this._container.Resolve<AXAClaimSearch>().RegisterSearchChannel(searchContext);
        }

        public bool ClaimsSearchFilter(SearchContext searchContext)
        {
            if (this.DuplicateSearchTypeFilter(searchContext))
            {
                return true;
            }

            if (searchContext.ChannelCriteria.SearchGroup == SearchGroup.Global)
            {
                return true;
            }

            return false;
        }

        private bool DuplicateSearchTypeFilter(SearchContext searchContext)
        {
            if (searchContext == null || searchContext.ChannelCriteria == null)
            {
                return false;
            }

            if (searchContext.ChannelCriteria.SearchGroup == SearchGroup.Global)
            {
                return true;
            }

            return false;
        }

        protected void RegisterViewsAndServices()
        {
            this._container.RegisterType<IViewController, ClaimSearchController>("ClaimSearchController");
            this._container.RegisterType<IViewController, AXAClaimController>("AXAClaimController");
            this._container.RegisterType<IView, ClaimSearchResult>("ClaimSearchResult");
            this._container.RegisterType<PresentationModel, AXAGenericDataItemModel>("AXAGenericDataItemModel");
            this._container.RegisterType<IView, ClaimTotalsView>("ClaimTotalsView");
            this._container.RegisterType<IView, ConfirmInactiveClaimDetailView>("ConfirmInactiveClaimDetailView");
            this._container.RegisterType<IView, ConfirmInactiveRecoveryClaimDetailView>("ConfirmInactiveRecoveryClaimDetailView");
            this._container.RegisterType<IView, ClaimTaskInitialDueDateView>("ClaimTaskInitialDueDateView");
            this._container.RegisterType<IViewController, ClaimTaskInitialDueDateController>("ClaimTaskInitialDueDateController");
            this._container.RegisterType<IViewController, ValidateReviewTaskController>("ValidateReviewTaskController");
            this._container.RegisterType<IView, BlankView>("BlankView");
            this._container.RegisterType<IView, EventDetailsView>("EventDetailsView");
            this._container.RegisterType<IView, AddressNameInvolvementView>("AddressNameInvolvementView");
            this._container.RegisterType<IViewController, AXAManualAuthorisationController>("AXAManualReserveRejectionController",
                            new InjectionProperty("AmountType", StaticValues.AmountType.Reserve),
                            new InjectionProperty("TransactionAction", TransactionAction.Reject));
            this._container.RegisterType<IViewController, AXAManualAuthorisationController>("AXAManualPaymentRejectionController",
                            new InjectionProperty("AmountType", StaticValues.AmountType.Payment),
                            new InjectionProperty("TransactionAction", TransactionAction.Reject));
            this._container.RegisterType<IViewController, AXAManualAuthorisationController>("AXAManualRecieptRejectionController",
                            new InjectionProperty("AmountType", StaticValues.AmountType.RecoveryReceipt),
                            new InjectionProperty("TransactionAction", TransactionAction.Reject));
            this._container.RegisterType<IView, ClaimPaymentRejectReasonView>("ClaimPaymentRejectReasonView");
            this._container.RegisterType<IViewController, AXABulkEventEntryController>("AXABulkEventEntryController");
            this._container.RegisterType<ISearchPopupController, AXAClaimPopupSearchController>("AXAClaimPopupSearchController");
            this._container.RegisterType<IViewController, AXAClaimCoverageVerificationController>("AXAClaimCoverageVerificationController");
            this._container.RegisterType<IView, DeductibleReasonCodesView>("DeductibleReasonCodesView");
            this._container.RegisterType<IViewController, AXADeductibleReasonCodeController>("AXADeductibleReasonCodeController");
            this._container.RegisterType<IViewController, AXAClaimReopenCloseController>("AXAClaimReopenCloseController");
            this._container.RegisterType<IViewController, AXADocumentDataController>("AXADocumentDataController");
            this._container.RegisterType<IPreviewController, AXAClaimTransactionPreviewController>("AXAClaimTransactionPreviewController");
            this._container.RegisterType<IViewController, AXAGlobalSearchController>("AXAGlobalSearchController");
            this._container.RegisterType<IView, ClaimAmountHistorySearch>("ClaimAmountHistorySearch");
            this._container.RegisterType<IViewController, AXAStandaloneClaimEventController>("StandaloneClaimEventController");
            this._container.RegisterType<INodeRenderBehaviour, AXAClaimsEventNodeRenderBehaviour>("AXAClaimsEventNodeRenderBehaviour");
            this._container.RegisterType<INodeRenderBehaviour, AXAClaimGenericDataItemNodeRenderBehaviour>("AXAClaimGenericDataItemNodeRenerBehaviour");
            this._container.RegisterType<INodeRenderBehaviour, AXASAndSD2NodeRenderBehaviour>("AXASAndSD2NodeRenderBehaviour");
            this._container.RegisterType<INodeAvailabilityBehaviour, AXAClaimDetailChildrenAvailabilityBehaviour>("IsCDChildAvailable");
            this._container.RegisterType<INodeRenderBehaviour, AXAAllDriverNodeRenderBehaviour>("AXAAllDriverNodeRenderBehaviour",
                new InjectionProperty("NameUsageTypeCode","DRV"),
                new InjectionProperty("NameInvolvementType", StaticValues.NameInvolvementType_ClaimNameInvolvement.Driver));
            this._container.RegisterType<INodeRenderBehaviour, AXAAllDriverNodeRenderBehaviour>("AXAAllClaimantNodeRenderBehaviour",
                new InjectionProperty("NameUsageTypeCode", "UCL"),
                new InjectionProperty("NameInvolvementType", StaticValues.NameInvolvementType_ClaimNameInvolvement.AdditionalClaimant));
            this._container.RegisterType<INodeRenderBehaviour, AXADriverNodeRenderBehaviour>("AXADriverNodeRenderBehaviour");
            this._container.RegisterType<INodeRenderBehaviour, AXADriverNodeRenderBehaviour>("AXAClaimantNodeRenderBehaviour");
            this._container.RegisterType<INodeAvailabilityBehaviour, AXAClaimIOLinksNodeAvailabilityBehaviour>("AXAIsVehicleLinkAvailable",
                new InjectionProperty(ClaimIOLinksNodeAvailabilityBehaviour.InternalIOTypeDependency, StaticValues.InternalIOType.Vehicle),
                new InjectionProperty(ClaimIOLinksNodeAvailabilityBehaviour.LinkableComponentTypeDependency, StaticValues.LinkableComponentType.InsuredObject));
            this._container.RegisterType<INodeAvailabilityBehaviour, AXAClaimDetailInvolvementLinksNodeAvailabilityBehaviour>("AXAIsLitigationLinkAvailable",
               new InjectionProperty(InvolvementLinksNodeAvailabilityBehaviour.LinkableComponentTypeDependency, StaticValues.LinkableComponentType.Litigation));
            this._container.RegisterType<INodeAvailabilityBehaviour, AXAClaimDetailInvolvementLinksNodeAvailabilityBehaviour>("AXAIsRecoveryLinkAvailable",
                new InjectionProperty(InvolvementLinksNodeAvailabilityBehaviour.LinkableComponentTypeDependency, StaticValues.LinkableComponentType.Recovery));

            this._container.RegisterType<INodeAvailabilityBehaviour, AXALitigationChildrenAvailabilityBehaviour>("AXAIsLitigationChildAvailable");
            this._container.RegisterType<INodeAvailabilityBehaviour, AXAClaimDetailLinksNodeAvailabilityBehaviour>("AXAIsClaimDetailLinkAvailable");

            this._container.RegisterType<INodeAvailabilityBehaviour, AXALitigationInvolvementLinksNodeAvailabilityBehaviour>("AXAIsInvolvementLinkAvailable",
                new InjectionProperty(InvolvementLinksNodeAvailabilityBehaviour.LinkableComponentTypeDependency, StaticValues.LinkableComponentType.NameInvolvement));
            this._container.RegisterType<INodeCreationBehaviour, AXAClaimNameInvolvementGroupNodeBehaviour>("AXAClaimNameInvolvementDriverGroupNodeBehaviour",
                 new InjectionProperty("ClaimNameInvolvementType", StaticValues.NameInvolvementType_ClaimNameInvolvement.Driver));

            this._container.RegisterType<INodeChildrenLoadingBehaviour, AXAClaimNameInvolvementGroupNodeBehaviour>("AXAClaimNameInvolvementDriverGroupNodeBehaviour");

            this._container.RegisterType<INodeCreationBehaviour, AXAClaimNameInvolvementGroupNodeBehaviour>("AXAClaimNameInvolvementClaimantGroupNodeBehaviour",
                new InjectionProperty("ClaimNameInvolvementType", StaticValues.NameInvolvementType_ClaimNameInvolvement.AdditionalClaimant));

            this._container.RegisterType<INodeChildrenLoadingBehaviour, AXAClaimNameInvolvementGroupNodeBehaviour>("AXAClaimNameInvolvementClaimantGroupNodeBehaviour");
            // register render behaviour
            this._container.RegisterType<INodeRenderBehaviour, AXAAllClaimNameInvolvementNodeRenderBehaviour>("AXAAllClaimNameInvolvementNodeRenderBehaviour");
            this._container.RegisterType<INodeRenderBehaviour, AXAClaimDetailNodeRenderBehaviour>("AXAClaimDetailNodeRenderBehaviour");
            this._container.RegisterType<INodeChildrenLoadingBehaviour, AXAClaimSummaryTreeNodeLoadingBehaviour>("AXAClaimSummaryTreeNodeLoadingBehaviour");
            this._container.RegisterType<INodeChildrenLoadingBehaviour, AXAClaimVehiclesChildrenLoadingBehaviour>("AXAClaimVehiclesChildrenLoadingBehaviour");
            return;
        }

        protected override void RegisterDeepLinkingLinks(DeeplinkingConfig deeplinkingConfig)
        {
        }
    }
}
