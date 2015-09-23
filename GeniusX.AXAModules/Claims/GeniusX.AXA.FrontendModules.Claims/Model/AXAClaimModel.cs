using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using GeniusX.AXA.FrontendModules.Claims.ClaimSummaryCalculation;
using Microsoft.Practices.Prism.Commands;
using Xiap.Framework.Data;
using Xiap.Framework.Metadata;
using XIAP.Frontend.CommonInfrastructure.Model.Documents;
using XIAP.Frontend.Infrastructure.Events;
using XIAP.Frontend.Infrastructure.QuickMenu;
using XIAP.FrontendModules.Claims.Model;
using XIAP.FrontendModules.Claims.Model.ClaimIO;
using XIAP.FrontendModules.Common.ClaimService;
using XIAP.FrontendModules.Common.SearchService;
using XIAP.FrontendModules.Infrastructure.NavTree;
using System.Collections.Generic;
using Xiap.Metadata.Data.Enums;
using XIAP.FrontendModules.Common.NameInvolvements;
using XIAP.FrontendModules.Common;
using XIAP.Frontend.Infrastructure;
using System.Windows;
using XIAP.FrontendModules.Claims.Enumerations;
using XIAP.Frontend.CommonInfrastructure.Model.Events;
using System.ComponentModel;

namespace GeniusX.AXA.FrontendModules.Claims.Model
{
    public class AXAClaimModel : ClaimModel
    {
        #region Private Variables
        private SearchParameters searchParameters;
        private SearchData searchData;
        private decimal? _AXAManagedShare;
        private decimal? _AXAShare;
        private decimal? _TotalClaimLoss = 0.00m;
        private decimal? _Excess = 0.00m;
        private decimal? _OutstandingEstimate = 0.00m;
        private decimal? _OutstandingRecoveryEstimate = 0.00m;
        private decimal? _OutstandingULREstimate = 0.00m;
        private decimal? _PaymentsInProgress = 0.00m;
        private decimal? _RecoveryInProgress = 0.00m;
        private decimal? _ULR = 0.00m;
        private decimal? _TotalPaymentsPaid = 0.00m;
        private decimal? _RecoveriesCompleted = 0.00m;
        private decimal? _ULRCompleted = 0.00m;
        private decimal? _MovementAmount = 0.00m;
        private decimal? _TransactionAmount = 0.00m;

        private string _SelectedClaimDetailFilter = "1";
        private string _SelectedClaimTransactionFilter = "1";
        private string _SelectedClaimAmountFilter = "1";
        private string selectedShareShown = "1";
        private ClaimDetailDto _selectedClaimDetail;
        private string _createdByUserName;
        private string _taskInitialUserName;
        private bool _canExecute;
        private string _claimTotalTitle;
        private bool _initializeHeader = true;
        private Visibility isAmountShownFilterVisible;
        private ObservableCollection<CodeRow> _ClaimDetailFilter = new ObservableCollection<CodeRow>();
        private ObservableCollection<CodeRow> _ClaimTransactionFilter = new ObservableCollection<CodeRow>();
        private ObservableCollection<CodeRow> _ClaimAmountFilter = new ObservableCollection<CodeRow>();
        private ObservableCollection<CodeRow> shareShownFilter = new ObservableCollection<CodeRow>();
        private AXAClaimDetailModel axaClaimDetailModel;
        private ObservableCollection<ClaimFinancialAmount> claimAmountSearchRows = new ObservableCollection<ClaimFinancialAmount>();
        private string _clientName;
        #endregion

        public bool IsBaseFieldsMetadta = false;
       
        public AXAClaimModel(XIAPTreeModel treeModel)
            : base(treeModel)
        {
            ////Dupicate serach button command.
            this.DuplicateClaimCheckCommand = new DelegateCommand<ClaimHeaderDto>(this.OnSearchClick, this.CanSearch);
            this.CoverageVarificationCommand = new DelegateCommand<NavigationMenuItemEventArgs>(this.OnCoverageVerification, this.CanCoverageVarifcaionClick);
            this.SelectedItemChanged = new Action<DtoBase, bool>(this.RefreshTotals);
            this.axaClaimDetailModel = new AXAClaimDetailModel(this);
            this.IsBaseFieldsMetadta = true;
            this.INACTReviewCommand = new NamedDelegateCommand<NavigationMenuItemEventArgs>("INACTReviewCommand", this.DoINACTReviewCommand, this.CanINACTReviewClick);
            this.InactRecoveryReviewCommand = new NamedDelegateCommand<NavigationMenuItemEventArgs>("InactRecoveryReviewCommand", this.DoInactRecoveryReviewCommand , this.CanInactRecoveryReviewClick); 
        }

        #region Public events
        public event EventHandler ReloadTotals;
        public event EventHandler ReloadClaimants;
        public event EventHandler CustomCoverageVerification;
        public event EventHandler<CommandEventArgs<ClaimHeaderDto>> DuplicateClaimCheckClick;
        public event EventHandler OnINACTReviewClick;
        public event EventHandler OnInactRecoveryReviewClick;
        public DelegateCommand<ClaimHeaderDto> DuplicateClaimCheckCommand { get; set; }
        public DelegateCommand<NavigationMenuItemEventArgs> INACTReviewCommand { get; private set; }
        public DelegateCommand<NavigationMenuItemEventArgs> InactRecoveryReviewCommand { get; private set; }    

        #endregion

          #region Public properties
        
        public decimal? AXAManagedShare
        {
            get
            {
                return this._AXAManagedShare;
            }

            set
            {
                this._AXAManagedShare = value;
                this.OnPropertyChanged("AXAManagedShare");
            }
        }

        public decimal? AXAShare
        {
            get
            {
                return this._AXAShare;
            }

            set
            {
                this._AXAShare = value;
                this.OnPropertyChanged("AXAShare");
            }
        }

        public SearchParameters SearchParameters
        {
            get
            {
                return this.searchParameters;
            }

            set
            {
                this.searchParameters = value;
            }
        }

        public ObservableCollection<CodeRow> ShareShownFilter
        {
            get
            {
                if (this.shareShownFilter.Count == 0)
                {
                    CodeRow shareShownFilterList = new CodeRow();
                    shareShownFilterList.Code = "1";
                    shareShownFilterList.Description = "AXA Managed Share";
                    this.shareShownFilter.Add(shareShownFilterList);
                    shareShownFilterList = new CodeRow();
                    shareShownFilterList.Code = "2";
                    shareShownFilterList.Description = "AXA Share Only";
                    this.shareShownFilter.Add(shareShownFilterList);
                    shareShownFilterList = new CodeRow();
                    shareShownFilterList.Code = "3";
                    shareShownFilterList.Description = "100%";
                    this.shareShownFilter.Add(shareShownFilterList);
                }

                return this.shareShownFilter;
            }
        }

        public string SelectedShareShown
        {
            get
            {
                return this.selectedShareShown;
            }

            set
            {
                this.selectedShareShown = value;
                this.OnPropertyChanged("SelectedShareShown");
                this.InvokeEvent(this.ReloadTotals);
            }
        }

        public Visibility IsAmountShownFilterVisible
        {
            get
            {
                short? ClaimHeaderAutomaticDeductibleProcessingMethod = this.ProductClaimDefinitionItem.ClaimHeaderAutomaticDeductibleProcessingMethod;
                return (ClaimHeaderAutomaticDeductibleProcessingMethod.HasValue && ClaimHeaderAutomaticDeductibleProcessingMethod.GetValueOrDefault() != (short)StaticValues.ClaimHeaderAutomaticDeductibleProcessingMethod.NotActive) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public Visibility IsShareShowmFilterVisible
        {
            get
            {
                return this.IsAmountShownFilterVisible == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public Field AXAManagedShareField
        {
            get
            {
                return (Field)this.HeaderData.CustomProperties["AXAManagedShareField"];
            }
        }

        public Field AXAShareField
        {
            get
            {
                return (Field)this.HeaderData.CustomProperties["AXAShareField"];
            }
        }

        public Field TotalClaimLossField
        {
            get
            {
                return (Field)this.HeaderData.CustomProperties["TotalClaimLossField"];
            }
        }

        public SearchData SearchData
        {
            get { return this.searchData; }
            set { this.searchData = value; }
        }

        public ObservableCollection<ClaimFinancialAmount> ClaimAmountSearchRows
        {
            get
            {
                return this.claimAmountSearchRows;
            }

            set
            {
                this.claimAmountSearchRows = value;
                OnPropertyChanged("ClaimAmountSearchRows");
            }
        }

        public decimal? TotalClaimLoss
        {
            get 
            {
                return this._TotalClaimLoss; 
            }

            set
            {
                this._TotalClaimLoss = value;
                this.OnPropertyChanged("TotalClaimLoss");
            }
        }

        public decimal? Excess
        {
            get
            {
                return this.HeaderData.CustomNumeric10;
            }
        }

        public decimal? OutstandingEstimate
        {
            get
            {
                return this._OutstandingEstimate; 
            }

            set
            {
                this._OutstandingEstimate = value;
                this.OnPropertyChanged("OutstandingEstimate");
            }
        }

        public decimal? OutstandingRecoveryEstimate
        {
            get 
            {
                return this._OutstandingRecoveryEstimate; 
            }

            set
            {
                this._OutstandingRecoveryEstimate = value;
                this.OnPropertyChanged("OutstandingRecoveryEstimate");
            }
        }

        public decimal? OutstandingULREstimate
        {
            get 
            {
                return this._OutstandingULREstimate; 
            }

            set
            {
                this._OutstandingULREstimate = value;
                this.OnPropertyChanged("OutstandingULREstimate");
            }
        }

        public decimal? PaymentsInProgress
        {
            get 
            {
                return this._PaymentsInProgress; 
            }

            set
            {
                this._PaymentsInProgress = value;
                this.OnPropertyChanged("PaymentsInProgress");
            }
        }

        public decimal? RecoveryInProgress
        {
            get
            {
                return this._RecoveryInProgress; 
            }

            set
            {
                this._RecoveryInProgress = value;
                this.OnPropertyChanged("RecoveryInProgress");
            }
        }

        public decimal? ULR
        {
            get
            {
                return this._ULR; 
            }

            set
            {
                this._ULR = value;
                this.OnPropertyChanged("ULR");
            }
        }

        public decimal? TotalPaymentsPaid
        {
            get 
            {
                return this._TotalPaymentsPaid; 
            }

            set
            {
                this._TotalPaymentsPaid = value;
                this.OnPropertyChanged("TotalPaymentsPaid");
            }
        }

        public decimal? RecoveriesCompleted
        {
            get
            {
                return this._RecoveriesCompleted; 
            }

            set
            {
                this._RecoveriesCompleted = value;
                this.OnPropertyChanged("RecoveriesCompleted");
            }
        }

        public decimal? ULRCompleted
        {
            get 
            {
                return this._ULRCompleted; 
            }

            set
            {
                this._ULRCompleted = value;
                this.OnPropertyChanged("ULRCompleted");
            }
        }

        public ObservableCollection<CodeRow> ClaimDetailFilter
        {
            get
            {
                if (this._ClaimDetailFilter.Count == 0)
                {
                    CodeRow list = new CodeRow();
                    list.Code = "1";
                    list.Description = "Active Claimants";
                    this._ClaimDetailFilter.Add(list);

                    list = new CodeRow();
                    list.Code = "2";
                    list.Description = "All Claimants";
                    this._ClaimDetailFilter.Add(list);
                }

                return this._ClaimDetailFilter;
            }
        }

        public ObservableCollection<CodeRow> ClaimTransactionFilter
        {
            get
            {
                if (this._ClaimTransactionFilter.Count == 0)
                {
                    CodeRow list = new CodeRow();
                    list.Code = "1";
                    list.Description = "Whole Claim";
                    this._ClaimTransactionFilter.Add(list);
                    list = new CodeRow();
                    list.Code = "2";
                    list.Description = "Selected Claimant";
                    this._ClaimTransactionFilter.Add(list);
                }

                return this._ClaimTransactionFilter;
            }
        }

        public ObservableCollection<CodeRow> ClaimAmountFilter
        {
            get
            {
                if (this._ClaimAmountFilter.Count == 0)
                {
                    CodeRow list = new CodeRow();
                    list.Code = "1";
                    list.Description = "AXA Amounts";
                    this._ClaimAmountFilter.Add(list);
                    list = new CodeRow();
                    list.Code = "2";
                    list.Description = "Gross Amounts";
                    this._ClaimAmountFilter.Add(list);
                }

                return this._ClaimAmountFilter;
            }
        }

        public string SelectedClaimDetailFilter
        {
            get
            {
                return this._SelectedClaimDetailFilter;
            }

            set
            {
                this._SelectedClaimDetailFilter = value;
                this.OnPropertyChanged("SelectedClaimDetailFilter");
                this.InvokeEvent(this.ReloadClaimants);
                this.InvokeEvent(this.ReloadTotals);
            }
        }

        public string SelectedClaimTransactionFilter
        {
            get
            {
                if (this._SelectedClaimTransactionFilter == "1")
                {
                    this.ClaimTotalTitle = String.Empty;
                }
                else
                {
                    this.ClaimTotalTitle = GeniusX.AXA.FrontendModules.Claims.Resources.StringResources.ClaimDetailTotals;
                }

                return this._SelectedClaimTransactionFilter;
            }

            set
            {
                this._SelectedClaimTransactionFilter = value;
                this.OnPropertyChanged("SelectedClaimTransactionFilter");
                this.InvokeEvent(this.ReloadTotals);
            }
        }

        public string SelectedClaimAmountFilter
        {
            get
            {
                return this._SelectedClaimAmountFilter;
            }

            set
            {
                this._SelectedClaimAmountFilter = value;
                this.OnPropertyChanged("SelectedClaimAmountFilter");
                this.InvokeEvent(this.ReloadTotals);
            }
        }

        public ClaimDetailDto SelectedClaimant
        {
            get
            {
                return this._selectedClaimDetail;
            }

            set
            {
                this._selectedClaimDetail = value;
                this.OnPropertyChanged("SelectedClaimant");
            }
        }

        public string ClaimTotalTitle
        {
            get
            {
                return this._claimTotalTitle;
            }

            set
            {
                this._claimTotalTitle = value;
                this.OnPropertyChanged("ClaimTotalTitle");
            }
        }

        public Field ExcessField
        {
            get
            {
                return (Field)this.HeaderData.CustomProperties["ExcessField"];
            }
        }

        public Field OutstandingEstimateField
        {
            get
            {
                return (Field)this.HeaderData.CustomProperties["OutstandingEstimateField"];
            }
        }

        public Field OutstandingRecoveryEstimateField
        {
            get
            {
                return (Field)this.HeaderData.CustomProperties["OutstandingRecoveryEstimateField"];
            }
        }

        public Field OutstandingULREstimateField
        {
            get
            {
                return (Field)this.HeaderData.CustomProperties["OutstandingULREstimateField"];
            }
        }

        public Field PaymentsInProgressField
        {
            get
            {
                return (Field)this.HeaderData.CustomProperties["PaymentsInProgressField"];
            }
        }

        public Field RecoveryInProgressField
        {
            get
            {
                return (Field)this.HeaderData.CustomProperties["RecoveryInProgressField"];
            }
        }

        public Field ULRField
        {
            get
            {
                return (Field)this.HeaderData.CustomProperties["ULRField"];
            }
        }

        public Field TotalPaymentsPaidField
        {
            get
            {
                return (Field)this.HeaderData.CustomProperties["TotalPaymentsPaidField"];
            }
        }

        public Field RecoveriesCompletedField
        {
            get
            {
                return (Field)this.HeaderData.CustomProperties["RecoveriesCompletedField"];
            }
        }

        public Field ULRCompletedField
        {
            get
            {
                return (Field)this.HeaderData.CustomProperties["ULRCompletedField"];
            }
        }

        public string CreatedByUserName
        {
            get
            {
                return this._createdByUserName;
            }

            set
            {
                this._createdByUserName = value;
                this.OnPropertyChanged("CreatedByUserName");
            }
        }

        public string TaskInitialUserName
        {
            get
            {
                return this._taskInitialUserName;
            }

            set
            {
                this._taskInitialUserName = value;
                this.OnPropertyChanged("TaskInitialUserName");
            }
        }

        public bool CanExecute
        {
            get
            {
                return this._canExecute;
            }

            set
            {
                this._canExecute = value;
                this.DuplicateClaimCheckCommand.RaiseCanExecuteChanged();
            }
        }

        public string ClientName
        {
            get
            {
                return this._clientName;
            }

            set
            {
                this._clientName = value;
            }   
        }

        public string DriverNodeTitle
        {
            get
            {
                return "Driver (" + this.GetNameInvolvemntList(StaticValues.NameInvolvementType.Driver).Count() + ")";
            }
        }

        public string ClaimantNodeTitle
        {
            get
            {
                // rename claimants node from Claimant to Claimants
                return "Claimants (" + this.GetNameInvolvemntList(StaticValues.NameInvolvementType.AdditionalClaimant).Count() + ")";
            }
        }

        #endregion
        #region Private properties

        internal decimal? MovementAmount
        {
            get { return this._MovementAmount; }
            set { this._MovementAmount = value; }
        }

        internal decimal? TransactionAmount
        {
            get { return this._TransactionAmount; }
            set { this._TransactionAmount = value; }
        }

        #endregion
        #region Private methods

        private void RefreshTotals(DtoBase claimDetail, bool loadScreen)
        {
            this.SelectedClaimant = claimDetail as ClaimDetailDto;
            // If filter is selected as selected claimant then recalcualte Totals only
            if (this.SelectedClaimTransactionFilter == "2")
            {
                this.InvokeEvent(this.ReloadTotals);
            }
        }

        private void OnSearchClick(ClaimHeaderDto header)
        {
            this.InvokeEvent(this.DuplicateClaimCheckClick, new CommandEventArgs<ClaimHeaderDto>(header));
        }

        private bool CanSearch(ClaimHeaderDto header)
        {
            return this._canExecute;
        }


        #region INACT Claim Reserve check
     

        public Visibility INACTReviewVisible
        {
            get
            {
                if (this.TransactionType == ClaimTransactionType.DisplayClaim || (this.ClaimDetailModel.SelectedClaimDetailDto != null && this.ClaimDetailModel.SelectedClaimDetailDto.ClaimDetailData != null && this.ClaimDetailModel.SelectedClaimDetailDto.ClaimDetailData.ClaimDetailInternalStatus != (short?)StaticValues.ClaimDetailInternalStatus.InProgress))
                {
                    return  Visibility.Collapsed;
                }

                return Visibility.Visible;
            }
        }

        private void DoINACTReviewCommand(NavigationMenuItemEventArgs args)
        {
            this.InvokeEvent(this.OnINACTReviewClick);
        }

        public bool CanINACTReviewClick(NavigationMenuItemEventArgs arg)
        {
            return true;
        }

        private void DoInactRecoveryReviewCommand(NavigationMenuItemEventArgs args)
        {
            this.InvokeEvent(this.OnInactRecoveryReviewClick);
        }

        public bool CanInactRecoveryReviewClick(NavigationMenuItemEventArgs arg)
        {
            return true;
        }


        #endregion

        private Dictionary<Guid, ClaimNameInvolvementDto> GetNILinkedToDriver(IEnumerable<ClaimNameInvolvementDto> drivers)
        {
            Dictionary<Guid, ClaimNameInvolvementDto> linkedDriverAndVehicles = new Dictionary<Guid, ClaimNameInvolvementDto>();
            foreach (ClaimNameInvolvementDto driver in drivers)
            {
                Guid driverGuidID = driver.ClaimInvolvement.Data.DataId;
                IEnumerable<ClaimInvolvementLinkDto> driverLinkedNI = this.GetLinkedClaimInvovlements(driverGuidID);

                if (driverLinkedNI != null)
                {
                    this.GetLinkedDriverandVehicleNI(driverLinkedNI, false, linkedDriverAndVehicles);
                }

                if (!linkedDriverAndVehicles.ContainsKey(driver.Data.DataId))
                {
                    linkedDriverAndVehicles.Add(driver.Data.DataId, driver);
                }
            }

            return linkedDriverAndVehicles;
        }

        private Dictionary<Guid, ClaimNameInvolvementDto> GetNonLinkedClaimantForADList(IEnumerable<ClaimNameInvolvementDto> claimants)
        {
            Dictionary<Guid, ClaimNameInvolvementDto> nonLinkedClaimants = new Dictionary<Guid, ClaimNameInvolvementDto>();
            foreach (ClaimNameInvolvementDto claimant in claimants)
            {
                Guid claimantGuidID = claimant.ClaimInvolvement.ClaimInvolvementData.DataId;
                IEnumerable<ClaimInvolvementLinkDto> linkedNI = this.GetLinkedClaimInvovlements(claimantGuidID);

                if (linkedNI != null)
                {
                    this.GetNonLinkedClaimantNI(linkedNI, false, nonLinkedClaimants);
                }

                if (!nonLinkedClaimants.ContainsKey(claimant.Data.DataId))
                {
                    nonLinkedClaimants.Add(claimant.Data.DataId, claimant);
                }
            }

            return nonLinkedClaimants;
        }

        private Dictionary<Guid, ClaimNameInvolvementDto> GetNonlinkedClaimantNIList(IEnumerable<ClaimNameInvolvementDto> claimants, Guid claimDetailID)
        {
            //// This Method will return us the List of all NI those are not attached with Selected claim Detail(on which payment is in process) in  NonLinkedClaimants list
            Dictionary<Guid, ClaimNameInvolvementDto> nonLinkedClaimants = new Dictionary<Guid, ClaimNameInvolvementDto>();
            foreach (ClaimNameInvolvementDto claimant in claimants)
            {
                bool IsClaimantExistonClaimDetail = false;
                ClaimDetailDto ClaimantClaimDetail = this.HeaderDto.ClaimDetails.Where(cd => cd.ClaimDetailToClaimInvolvementLinks != null && cd.ClaimDetailToClaimInvolvementLinks.Any(link => link.ClaimInvolvement != null && ((IClaimInvolvementData)link.ClaimInvolvement.Data).ClaimInvolvementID == ((IClaimInvolvementData)claimant.ClaimInvolvement.ClaimInvolvementData).ClaimInvolvementID)).FirstOrDefault();
                ClaimDetailDto cdt = this.HeaderDto.ClaimDetails.FirstOrDefault(cd => cd.Data.DataId == claimDetailID);
                if (cdt != null && cdt.ClaimDetailToClaimInvolvementLinks != null)
                {
                     IsClaimantExistonClaimDetail = cdt.ClaimDetailToClaimInvolvementLinks.Any(link => link.ClaimNameInvolvement != null && link.ClaimNameInvolvement.Data.DataId == claimant.Data.DataId);
                }

                if (ClaimantClaimDetail != null && ClaimantClaimDetail.Data.DataId != claimDetailID && !IsClaimantExistonClaimDetail)
                {
                    Guid claimantGuidID = claimant.ClaimInvolvement.ClaimInvolvementData.DataId;
                    IEnumerable<ClaimInvolvementLinkDto> linkedNI = this.GetLinkedClaimInvovlements(claimantGuidID);
                    if (linkedNI != null)
                    {
                        this.GetNonLinkedClaimantNI(linkedNI, false, nonLinkedClaimants);
                    }

                    if (!nonLinkedClaimants.ContainsKey(claimant.Data.DataId))
                    {
                        nonLinkedClaimants.Add(claimant.Data.DataId, claimant);
                    }
                }

                if (ClaimantClaimDetail == null)
                {
                    Guid claimantGuidID = claimant.ClaimInvolvement.ClaimInvolvementData.DataId;
                    IEnumerable<ClaimInvolvementLinkDto> linkedNI = this.GetLinkedClaimInvovlements(claimantGuidID);
                    if (linkedNI != null)
                    {
                        this.GetNonLinkedClaimantNI(linkedNI, false, nonLinkedClaimants);
                    }

                    if (!nonLinkedClaimants.ContainsKey(claimant.Data.DataId))
                    {
                        nonLinkedClaimants.Add(claimant.Data.DataId, claimant);
                    }
                }
            }

            return nonLinkedClaimants;
        }

        private Dictionary<Guid, ClaimNameInvolvementDto> GetLinkedClaimantNIList(IEnumerable<ClaimNameInvolvementDto> claimants, Guid claimDetailID)
        {
            //// This Method will return us the List of all NI those are attached with Selected claim Detail(on which payment is in process) in  LinkedClaimants list
            Dictionary<Guid, ClaimNameInvolvementDto> linkedClaimants = new Dictionary<Guid, ClaimNameInvolvementDto>();
            foreach (ClaimNameInvolvementDto claimant in claimants)
            {
                if (claimant.ClaimInvolvement != null)
                {
                    ClaimDetailDto ClaimantClaimDetail = this.HeaderDto.ClaimDetails.Where(cd => cd.ClaimDetailToClaimInvolvementLinks != null && cd.ClaimDetailToClaimInvolvementLinks.Any(link => link.ClaimInvolvement != null && ((IClaimInvolvementData)link.ClaimInvolvement.Data).ClaimInvolvementID == ((IClaimInvolvementData)claimant.ClaimInvolvement.ClaimInvolvementData).ClaimInvolvementID)).FirstOrDefault();
                    ClaimDetailDto cdt = this.HeaderDto.ClaimDetails.FirstOrDefault(cd => cd.Data.DataId == claimDetailID);
                    bool isClaimantExistonClaimDetail = cdt.ClaimDetailToClaimInvolvementLinks.Any(link => link.ClaimNameInvolvement != null && link.ClaimNameInvolvement.Data.DataId == claimant.Data.DataId);


                    if (isClaimantExistonClaimDetail)
                    {
                        Guid claimantGuidID = claimant.ClaimInvolvement.ClaimInvolvementData.DataId;
                        IEnumerable<ClaimInvolvementLinkDto> linkedNI = this.GetLinkedClaimInvovlements(claimantGuidID);
                        if (linkedNI != null)
                        {
                            this.GetLinkedClaimantNI(linkedNI, false, linkedClaimants);
                        }

                        if (!linkedClaimants.ContainsKey(claimant.Data.DataId))
                        {
                            linkedClaimants.Add(claimant.Data.DataId, claimant);
                        }

                        break;
                    }
                }
            }

            return linkedClaimants;
        }

        private void GetLinkedClaimantNI(IEnumerable<ClaimInvolvementLinkDto> linkedNI, bool IsFrom, Dictionary<Guid, ClaimNameInvolvementDto> linkedClaimants)
        {
            if (linkedNI == null)
            {
                return;
            }

            foreach (ClaimInvolvementLinkDto a in linkedNI)
            {
                Guid chkLinkedClaimantNITo = (a.Data as IClaimInvolvementLinkData).ClaimInvolvementToDataId;
                Guid chkLinkedClaimantNIFrom = (a.Data as IClaimInvolvementLinkData).ClaimInvolvementFromDataId;

                var ni = this.HeaderDto.ClaimNameInvolvements.Where(e => e.ClaimNameInvolvment != null && (e.Data.DataId == (a.Data as IClaimInvolvementLinkData).ClaimInvolvementToDataId)).Select(f => f.ClaimNameInvolvment).ToDictionary(f => f.Data.DataId, f => f).FirstOrDefault();

                if (IsFrom)
                {
                    ni = this.HeaderDto.ClaimNameInvolvements.Where(e => e.ClaimNameInvolvment != null && (e.Data.DataId == (a.Data as IClaimInvolvementLinkData).ClaimInvolvementFromDataId)).Select(f => f.ClaimNameInvolvment).ToDictionary(f => f.Data.DataId, f => f).FirstOrDefault();
                }


                if (!linkedClaimants.ContainsKey(ni.Key))
                {
                    if (ni.Value != null)
                    {
                        linkedClaimants.Add(ni.Key, ni.Value);
                    }

                    IEnumerable<ClaimInvolvementLinkDto> b = this.GetLinkedClaimInvolvements(a, chkLinkedClaimantNITo, IsFrom, chkLinkedClaimantNIFrom);
                    if (b.Count() > 0)
                    {
                        this.GetLinkedClaimantNI(b, true, linkedClaimants);
                    }
                }
            }
        }

        private void GetNonLinkedClaimantNI(IEnumerable<ClaimInvolvementLinkDto> linkedNI, bool isFrom, Dictionary<Guid, ClaimNameInvolvementDto> nonLinkedClaimants)
        {
            if (linkedNI != null)
            {
                foreach (ClaimInvolvementLinkDto a in linkedNI)
                {
                    Guid chkLinkedClaimantNITo = (a.Data as IClaimInvolvementLinkData).ClaimInvolvementToDataId;
                    Guid chkLinkedClaimantNIFrom = (a.Data as IClaimInvolvementLinkData).ClaimInvolvementFromDataId;

                    var ni = this.HeaderDto.ClaimNameInvolvements.Where(e => e.ClaimNameInvolvment != null && (e.Data.DataId == (a.Data as IClaimInvolvementLinkData).ClaimInvolvementToDataId)).Select(f => f.ClaimNameInvolvment).ToDictionary(f => f.Data.DataId, f => f).FirstOrDefault();

                    if (isFrom)
                    {
                        ni = this.HeaderDto.ClaimNameInvolvements.Where(e => e.ClaimNameInvolvment != null && (e.Data.DataId == (a.Data as IClaimInvolvementLinkData).ClaimInvolvementFromDataId)).Select(f => f.ClaimNameInvolvment).ToDictionary(f => f.Data.DataId, f => f).FirstOrDefault();
                    }

                    if (!nonLinkedClaimants.ContainsKey(ni.Key))
                    {
                        if (ni.Value != null)
                        {
                            nonLinkedClaimants.Add(ni.Key, ni.Value);
                        }

                        IEnumerable<ClaimInvolvementLinkDto> b = this.GetLinkedClaimInvolvements(a, chkLinkedClaimantNITo, isFrom, chkLinkedClaimantNIFrom);
                        if (b.Count() > 0)
                        {
                            this.GetNonLinkedClaimantNI(b, true, nonLinkedClaimants);
                        }
                    }
                }
            }
        }

        private void GetLinkedDriverandVehicleNI(IEnumerable<ClaimInvolvementLinkDto> linkedNIs, bool isFrom, Dictionary<Guid, ClaimNameInvolvementDto> linkedDriverAndVehicles)
        {
            if (linkedNIs == null)
            {
                return;
            }

            foreach (ClaimInvolvementLinkDto linkedNI in linkedNIs)
            {
                Guid chkLinkedVehicleClaimantNITo = (linkedNI.Data as IClaimInvolvementLinkData).ClaimInvolvementToDataId;
                Guid chkLinkedVehicleClaimantNIFrom = (linkedNI.Data as IClaimInvolvementLinkData).ClaimInvolvementFromDataId;
                var ni = this.HeaderDto.ClaimNameInvolvements.Where(inv => inv.ClaimNameInvolvment != null && (inv.Data.DataId == (linkedNI.Data as IClaimInvolvementLinkData).ClaimInvolvementFromDataId)).Select(f => f.ClaimNameInvolvment).ToDictionary(f => f.Data.DataId, f => f).FirstOrDefault();

                if (!linkedDriverAndVehicles.ContainsKey(ni.Key))
                {
                    if (ni.Value != null)
                    {
                        linkedDriverAndVehicles.Add(ni.Key, ni.Value);
                    }

                    IEnumerable<ClaimInvolvementLinkDto> b = this.GetLinkedClaimInvolvements(linkedNI, chkLinkedVehicleClaimantNITo, isFrom, chkLinkedVehicleClaimantNIFrom);
                    if (b.Count() > 0)
                    {
                        this.GetLinkedDriverandVehicleNI(b, true, linkedDriverAndVehicles);
                    }
                }
            }
        }

        private List<ClaimInvolvementLinkDto> GetLinkedClaimInvovlements(Guid claimInvovlementDataId)
        {
            List<ClaimInvolvementLinkDto> list = new List<ClaimInvolvementLinkDto>();
            if (claimInvovlementDataId != Guid.Empty && !this.HeaderDto.ClaimInvolvementLinks.IsNullOrEmpty())
            {
                list = this.HeaderDto.ClaimInvolvementLinks.Where(invLink => ((IClaimInvolvementLinkData)invLink.Data).ClaimInvolvementFromDataId == claimInvovlementDataId).ToList();
            }

            return list;
        }

        private List<ClaimInvolvementLinkDto> GetLinkedClaimInvolvements(ClaimInvolvementLinkDto Dto, Guid claimInvovlementDataId, bool IsFrom, Guid claimInvovlementDataIdFrom)
        {
            List<ClaimInvolvementLinkDto> list = new List<ClaimInvolvementLinkDto>();
            if (claimInvovlementDataId != null && !this.HeaderDto.ClaimInvolvementLinks.IsNullOrEmpty())
            {
                if (IsFrom)
                {
                    list = this.HeaderDto.ClaimInvolvementLinks.Where(b => (b.Data as IClaimInvolvementLinkData).ClaimInvolvementFromDataId == claimInvovlementDataIdFrom && (b.Data as IClaimInvolvementLinkData).ClaimInvolvementToDataId == claimInvovlementDataId).ToList();
                }
                else
                {
                    list = this.HeaderDto.ClaimInvolvementLinks.Where(b => (b.Data as IClaimInvolvementLinkData).ClaimInvolvementToDataId == claimInvovlementDataId).ToList();
                }
            }

            return list;
        }

        private Dictionary<Guid, ClaimNameInvolvementDto> GetClaimHeaderNameInvolvement(ClaimHeaderDto claimHeader)
        {
            //// This Method provide us all the ClaimNameInvolvements 
            IEnumerable<ClaimNameInvolvementDto> ni = claimHeader.ClaimInvolvements
                .Where(c => c.Data != null && ((IClaimInvolvementData)c.Data).ClaimInvolvementType == (short)StaticValues.LinkableComponentType.NameInvolvement).Where(a => a.ClaimNameInvolvements != null)
                .SelectMany(b => b.ClaimNameInvolvements).Where(a => a.Data != null && ((IClaimNameInvolvementData)a.Data).NameID != null && ((IClaimNameInvolvementData)a.Data).NameInvolvementMaintenanceStatus == (short)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest);
            return ni.ToDictionary(n => n.Data.DataId, n => n);
        }


        private IEnumerable<ClaimNameInvolvementDto> GetNameInvolvemntList(StaticValues.NameInvolvementType type)
        {
            //// Get Driver list
            return this.HeaderDto.ClaimInvolvements
                           .Where(c => c.Data != null && (c.Data as IClaimInvolvementData).ClaimInvolvementType == (short)StaticValues.LinkableComponentType.NameInvolvement).Where(a => a.ClaimNameInvolvements != null)
                           .SelectMany(b => b.ClaimNameInvolvements).Where(a => a.Data != null && (a.Data as IClaimNameInvolvementData).NameInvolvementMaintenanceStatus == (short)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest &&
                           (a.Data as IClaimNameInvolvementData).NameInvolvementType == (short)type).Cast<ClaimNameInvolvementDto>();
        }

        #endregion
        
        public void OnCoverageVerification(NavigationMenuItemEventArgs arg)
        {
            if (this.CustomCoverageVerification != null)
            {
                this.InvokeEvent(this.CustomCoverageVerification);
            }
        }

        public override ClaimDetailModel ClaimDetailModel
        {
            get
            {
                return this.axaClaimDetailModel;
            }
        }

       
        protected override void AddFieldsToDictionry()
        {
            if (this.IsBaseFieldsMetadta)
            {
                base.AddFieldsToDictionry();
            }
        }

        protected override ReservePaymentToolBarModel GetReservePaymentToolBarModel()
        {
            return new AXAReservePaymentToolBarModel(this);
        }

        protected override DocumentModel GetDocumentModel()
        {
            return new AXADocumentModel();
        }

        /// <summary>
        /// returns the AXAEventModel.
        /// </summary>
        /// <returns>Type of EventModel</returns>
        protected override EventModel GetEventModel()
        {
            return new AXAEventModel();
        }

        protected override ClaimInsuredObjectLinkModel AddNewClaimInsuredObjectLinkModel()
        {
            return new AXAClaimInsuredObjectLinkModel();
        }

        protected override ClaimLitigationLinkModel AddNewClaimLitigationLinkModel()
        {
            return new AXAClaimLitigationLinkModel();
        }

        protected override ClaimRecoveryModel AddNewClaimRecoveryModel()
        {
            return new AXAClaimRecoveryModel();
        }

        protected override ClaimIOModel AddNewClaimIOModel()
        {
            return new AXAClaimIOModel();
        }

        protected override ClaimLitigationModel AddNewClaimLitigationModel()
        {
            return new AXAClaimLitigationModel();
        }

        protected override ClaimDetailLinkModel AddNewClaimDetailLinkModel()
        {
            return new AXAClaimDetailLinkModel(this);
        }

        public override ObservableCollection<ClaimDetailDto> GetSortedClaimDetailDtos(ObservableCollection<ClaimDetailDto> claimDetailDtos)
        {
            return claimDetailDtos.OrderBy(a => (a.Data as IClaimDetailData).ClaimDetailSortOrder)
                .ThenBy(a => int.Parse((a.Data as IClaimDetailData).ClaimDetailReference)).ToObservableCollection();
        }

        public override List<ClaimNameInvolvementDto> GetNameNIListForAddressee()
        {
            //// This method is used to filter and fetch the name involvemnts for the payemnts
            //// Fetch the name involvements linked with driver and claimants and based on the claim Detail type set the list of valid Addressess's
            //// We are using mainly three dictionary to hold the NI's linked with Driver , linked with claimant and not linked claimnt
            //// if the Transaction source is not payment the return the NI's as per core functionality
            if (this.ClaimTransactionSource == (short)StaticValues.ClaimTransactionSource.Payment)
            {
                Dictionary<Guid, ClaimNameInvolvementDto> linkedClaimants = new Dictionary<Guid, ClaimNameInvolvementDto>();
                Dictionary<Guid, ClaimNameInvolvementDto> nonLinkedClaimants = new Dictionary<Guid, ClaimNameInvolvementDto>();
                Dictionary<Guid, ClaimNameInvolvementDto> linkedDriverAndVehicles = new Dictionary<Guid, ClaimNameInvolvementDto>();
                Dictionary<Guid, ClaimNameInvolvementDto> validClaimNameInvolvements = new Dictionary<Guid, ClaimNameInvolvementDto>();
                Dictionary<Guid, ClaimNameInvolvementDto> headerNIs = this.GetClaimHeaderNameInvolvement(this.HeaderDto);
                Dictionary<Guid, ClaimNameInvolvementDto> claimNameInvolvements = base.GetNameNIListForPayment().ToDictionary(p => p.Data.DataId, p => p);
                IEnumerable<ClaimNameInvolvementDto> drivers = this.GetNameInvolvemntList(StaticValues.NameInvolvementType.Driver);
                IEnumerable<ClaimNameInvolvementDto> claimants = this.GetNameInvolvemntList(StaticValues.NameInvolvementType.AdditionalClaimant);
                
                Guid claimDetailID = (this.HeaderDto.InProgressClaimTransactionHeaders.First().ClaimTransactionGroups.First().Data as ClaimTransactionGroupData).ClaimDetailDataID;
                ClaimDetailDto claimDetail = this.HeaderDto.ClaimDetails.Where(cd => cd.Data.DataId == claimDetailID).FirstOrDefault();

                if (claimDetail.ClaimDetailData.ClaimDetailTypeCode == AXAClaimConstants.CLAIMDETAILTYPE_AD)
                {
                    if (drivers != null)
                    {
                        linkedDriverAndVehicles = this.GetNILinkedToDriver(drivers);
                    }

                    if (claimants != null)
                    {
                        nonLinkedClaimants = this.GetNonLinkedClaimantForADList(claimants); 
                    }

                    headerNIs = headerNIs.Except(nonLinkedClaimants).ToDictionary(p => p.Key, p => p.Value);
                    claimNameInvolvements = claimNameInvolvements.Where(c => c.Value.ClaimNameInvolvementData.NameInvolvementType != (short)StaticValues.NameInvolvementType.AdditionalClaimant).ToDictionary(c => c.Key, c => c.Value);        
                    validClaimNameInvolvements = claimNameInvolvements.Except(nonLinkedClaimants).ToDictionary(p => p.Key, p => p.Value);
                    headerNIs = headerNIs.Where(c => c.Value.ClaimNameInvolvementData.NameInvolvementType != (short)StaticValues.NameInvolvementType.AdditionalClaimant).ToDictionary(c => c.Key, c => c.Value);

                    foreach (ClaimNameInvolvementDto cd in linkedDriverAndVehicles.Values)
                    {
                        if (!validClaimNameInvolvements.ContainsKey(cd.Data.DataId))
                        {
                            validClaimNameInvolvements.Add(cd.Data.DataId, cd);
                        }
                    }

                    foreach (ClaimNameInvolvementDto cd in headerNIs.Values)
                    {
                        if (!validClaimNameInvolvements.ContainsKey(cd.Data.DataId))
                        {
                            validClaimNameInvolvements.Add(cd.Data.DataId, cd);
                        }
                    }

                    
                    if (validClaimNameInvolvements != null)
                    {
                        //// Filter the NI on which payment is permitted and status is latest
                        validClaimNameInvolvements = validClaimNameInvolvements.Where(ni => (ni.Value.Data as IClaimNameInvolvementData).IsPaymentPermitted.GetValueOrDefault() && (ni.Value.Data as IClaimNameInvolvementData).NameInvolvementMaintenanceStatus == (short)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest).ToDictionary(p => p.Key, p => p.Value); 
                    }
                }
                else
                {
                    if (drivers != null)
                    {
                        linkedDriverAndVehicles = this.GetNILinkedToDriver(drivers);
                    }

                    if (claimants != null)
                    {
                        nonLinkedClaimants = this.GetNonlinkedClaimantNIList(claimants, claimDetailID);
                        linkedClaimants = this.GetLinkedClaimantNIList(claimants, claimDetailID);
                    }

                    headerNIs = headerNIs.Except(nonLinkedClaimants).ToDictionary(p => p.Key, p => p.Value);
                    headerNIs = headerNIs.Except(linkedDriverAndVehicles).ToDictionary(p => p.Key, p => p.Value);
                    claimNameInvolvements = claimNameInvolvements.Where(c => c.Value.ClaimNameInvolvementData.NameInvolvementType != (short)StaticValues.NameInvolvementType.AdditionalClaimant).ToDictionary(c => c.Key, c => c.Value);        
                    validClaimNameInvolvements = claimNameInvolvements.Except(nonLinkedClaimants).ToDictionary(p => p.Key, p => p.Value);
                    validClaimNameInvolvements = validClaimNameInvolvements.Except(linkedDriverAndVehicles).ToDictionary(p => p.Key, p => p.Value);
                    headerNIs = headerNIs.Where(c => c.Value.ClaimNameInvolvementData.NameInvolvementType != (short)StaticValues.NameInvolvementType.AdditionalClaimant).ToDictionary(c => c.Key, c => c.Value);
                    foreach (ClaimNameInvolvementDto cd in linkedClaimants.Values)
                    {
                        if (!validClaimNameInvolvements.ContainsKey(cd.Data.DataId))
                        {
                            validClaimNameInvolvements.Add(cd.Data.DataId, cd);
                        }
                    }

                    foreach (ClaimNameInvolvementDto cd in headerNIs.Values)
                    {
                        if (!validClaimNameInvolvements.ContainsKey(cd.Data.DataId))
                        {
                            validClaimNameInvolvements.Add(cd.Data.DataId, cd);
                        }
                    }
                    
                    if (validClaimNameInvolvements != null)
                    {
                        //// Filter the NI on which payment is permitted and status is latest
                        validClaimNameInvolvements = validClaimNameInvolvements.Where(ni => (ni.Value.Data as IClaimNameInvolvementData).IsPaymentPermitted.GetValueOrDefault() && (ni.Value.Data as IClaimNameInvolvementData).NameInvolvementMaintenanceStatus == (short)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest).ToDictionary(p => p.Key, p => p.Value); 
                    }
                }

                return validClaimNameInvolvements.Values.ToList();
            }
            else
            {
                return base.GetNameNIListForAddressee();
            }
        }


        public override List<ClaimNameInvolvementDto> GetNameNIListForPayment()
        {
            //// This method is used to filter and fetch the name involvemnts for the payemnts
            //// Fetch the name involvements linked with driver and claimants and based on the claim Detail type set the list of valid payee's
            //// We are using mainly three dictionary to hold the NI's linked with Driver , linked with claimant and not linked claimnt
            //// if the Transaction source is not payment the return the NI's as per core functionality
            if (this.ClaimTransactionSource == (short)StaticValues.ClaimTransactionSource.Payment)
            {
                Dictionary<Guid, ClaimNameInvolvementDto> linkedClaimants = new Dictionary<Guid, ClaimNameInvolvementDto>();
                Dictionary<Guid, ClaimNameInvolvementDto> nonLinkedClaimants = new Dictionary<Guid, ClaimNameInvolvementDto>();
                Dictionary<Guid, ClaimNameInvolvementDto> linkedDriverAndVehicles = new Dictionary<Guid, ClaimNameInvolvementDto>();
                Dictionary<Guid, ClaimNameInvolvementDto> validClaimNameInvolvements = new Dictionary<Guid, ClaimNameInvolvementDto>();
                //// Get all the ClaimNameInvolvements of From Header
                Dictionary<Guid, ClaimNameInvolvementDto> headerNIs = this.GetClaimHeaderNameInvolvement(this.HeaderDto);
                Dictionary<Guid, ClaimNameInvolvementDto> claimNameInvolvements = base.GetNameNIListForPayment().ToDictionary(p => p.Data.DataId, p => p);
               
             
               
                IEnumerable<ClaimNameInvolvementDto> drivers = this.GetNameInvolvemntList(StaticValues.NameInvolvementType.Driver);
                IEnumerable<ClaimNameInvolvementDto> claimants = this.GetNameInvolvemntList(StaticValues.NameInvolvementType.AdditionalClaimant);
               
                ////Get the Claim Detail Id on which current payment is added
                Guid claimDetailID = (this.HeaderDto.InProgressClaimTransactionHeaders.First().ClaimTransactionGroups.First().Data as ClaimTransactionGroupData).ClaimDetailDataID;
                ClaimDetailDto claimDetail = this.HeaderDto.ClaimDetails.Where(cd => cd.Data.DataId == claimDetailID).FirstOrDefault();
                //// For AD Claim Detail Type
                if (claimDetail.ClaimDetailData.ClaimDetailTypeCode == AXAClaimConstants.CLAIMDETAILTYPE_AD)
                {
                    if (drivers != null)
                    {   //// Get all the Name Involvement linked with Driver
                        linkedDriverAndVehicles = this.GetNILinkedToDriver(drivers);
                    }

                    if (claimants != null)
                    {   //// Get all the Name Involvement linked with All Claimants and store them in NonLinkedClaimants list
                        nonLinkedClaimants = this.GetNonLinkedClaimantForADList(claimants); 
                    }

                    headerNIs = headerNIs.Except(nonLinkedClaimants).ToDictionary(p => p.Key , p => p.Value);
                    claimNameInvolvements = claimNameInvolvements.Where(c => c.Value.ClaimNameInvolvementData.NameInvolvementType != (short)StaticValues.NameInvolvementType.AdditionalClaimant).ToDictionary(c => c.Key, c => c.Value);        
                    validClaimNameInvolvements = claimNameInvolvements.Except(nonLinkedClaimants).ToDictionary(p => p.Key, p => p.Value);
                    headerNIs = headerNIs.Where(c => c.Value.ClaimNameInvolvementData.NameInvolvementType != (short)StaticValues.NameInvolvementType.AdditionalClaimant).ToDictionary(c => c.Key, c => c.Value);
                    
                    foreach (ClaimNameInvolvementDto cd in linkedDriverAndVehicles.Values)
                    {
                        if (!validClaimNameInvolvements.ContainsKey(cd.Data.DataId))
                        {
                            validClaimNameInvolvements.Add(cd.Data.DataId, cd);
                        }
                    }

                    foreach (ClaimNameInvolvementDto cd in headerNIs.Values)
                    {
                        if (!validClaimNameInvolvements.ContainsKey(cd.Data.DataId))
                        {
                            validClaimNameInvolvements.Add(cd.Data.DataId, cd);
                        }
                    }
                    
                                      
                    if (validClaimNameInvolvements != null)
                    {
                        //// Filter the NI on which payment is permitted and status is latest
                        validClaimNameInvolvements = validClaimNameInvolvements.Where(ni => (ni.Value.Data as IClaimNameInvolvementData).IsPaymentPermitted.GetValueOrDefault() && (ni.Value.Data as IClaimNameInvolvementData).NameInvolvementMaintenanceStatus == (short)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest).ToDictionary(p => p.Key, p => p.Value); 
                    }
                }
                else
                {
                    //// This Block will be executed for all ClaimDetails except  AD
                    if (drivers != null)
                    {
                        //// Get all the Name Involvement linked with Driver
                        linkedDriverAndVehicles = this.GetNILinkedToDriver(drivers);
                    }

                    if (claimants != null)
                    {
                        //// get all the NI's for other claimants 
                        nonLinkedClaimants = this.GetNonlinkedClaimantNIList(claimants, claimDetailID);
                        //// get all the NI's of the claimant on which payments is in progress
                        linkedClaimants = this.GetLinkedClaimantNIList(claimants, claimDetailID);
                    }

                    headerNIs = headerNIs.Except(nonLinkedClaimants).ToDictionary(p => p.Key, p => p.Value);
                    headerNIs = headerNIs.Except(linkedDriverAndVehicles).ToDictionary(p => p.Key, p => p.Value);
                    claimNameInvolvements = claimNameInvolvements.Where(c => c.Value.ClaimNameInvolvementData.NameInvolvementType != (short)StaticValues.NameInvolvementType.AdditionalClaimant).ToDictionary(c => c.Key, c => c.Value);        
                    validClaimNameInvolvements = claimNameInvolvements.Except(nonLinkedClaimants).ToDictionary(p => p.Key, p => p.Value);
                    validClaimNameInvolvements = validClaimNameInvolvements.Except(linkedDriverAndVehicles).ToDictionary(p => p.Key, p => p.Value);
                    headerNIs = headerNIs.Where(c => c.Value.ClaimNameInvolvementData.NameInvolvementType != (short)StaticValues.NameInvolvementType.AdditionalClaimant).ToDictionary(c => c.Key, c => c.Value);
                   
                    foreach (ClaimNameInvolvementDto cd in linkedClaimants.Values)
                    {
                        if (!validClaimNameInvolvements.ContainsKey(cd.Data.DataId))
                        {
                            validClaimNameInvolvements.Add(cd.Data.DataId, cd);
                        }
                    }

                    foreach (ClaimNameInvolvementDto cd in headerNIs.Values)
                    {
                        if (!validClaimNameInvolvements.ContainsKey(cd.Data.DataId))
                        {
                            validClaimNameInvolvements.Add(cd.Data.DataId, cd);
                        }
                    }
                    

                    if (validClaimNameInvolvements != null)
                    {
                        //// Filter the NI on which payment is permitted and status is latest
                        validClaimNameInvolvements = validClaimNameInvolvements.Where(ni => (ni.Value.Data as IClaimNameInvolvementData).IsPaymentPermitted.GetValueOrDefault() && (ni.Value.Data as IClaimNameInvolvementData).NameInvolvementMaintenanceStatus == (short)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest).ToDictionary(p => p.Key, p => p.Value); 
                    }
                }

                return validClaimNameInvolvements.Values.ToList();  
            }
            else
            {
                return base.GetNameNIListForPayment();
            }
        }

        public void RefreshProperty()
        {
            this.OnPropertyChanged("DriverNodeTitle");
            this.OnPropertyChanged("ClaimantNodeTitle");
        }
    }
}
