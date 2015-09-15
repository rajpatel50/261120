using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Linq;
using System.Text;
using GeniusX.AXA.FrontendModules.Claims.ClaimSummaryCalculation;
using GeniusX.AXA.FrontendModules.Claims.Model;
using GeniusX.AXA.FrontendModules.Claims.Notifications;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Unity;
using Xiap.ClientServices.Facade.Common;
using Xiap.Framework.Data;
using Xiap.Framework.DataMapping;
using Xiap.Framework.Entity;
using Xiap.Framework.Logging;
using Xiap.Framework.Metadata;
using Xiap.Metadata.Data.Enums;
using XIAP.Frontend.CoreControls;
using XIAP.Frontend.Infrastructure;
using XIAP.FrontendModules.Infrastructure.NavTree;
using XIAP.Frontend.Infrastructure.Configuration;
using XIAP.Frontend.Infrastructure.DataMapping;
using XIAP.Frontend.Infrastructure.Names;
using XIAP.Frontend.Infrastructure.Navigation;
using XIAP.Frontend.Infrastructure.Notifications;
using XIAP.Frontend.Infrastructure.Rules;
using XIAP.Frontend.Infrastructure.Search;
using XIAP.FrontendModules.Application;
using XIAP.FrontendModules.Claims.Controller;
using XIAP.FrontendModules.Claims.Data;
using XIAP.FrontendModules.Claims.Enumerations;
using XIAP.FrontendModules.Claims.Model;
using XIAP.FrontendModules.Claims.Resources;
using XIAP.FrontendModules.Claims.Service;
using XIAP.FrontendModules.Claims.TreeviewResolver;
using XIAP.FrontendModules.Claims.ViewResolver;
using XIAP.FrontendModules.Common;
using XIAP.FrontendModules.Common.ClaimService;
using XIAP.FrontendModules.Common.Controller;
using XIAP.FrontendModules.Common.GenericDataSets;
using XIAP.FrontendModules.Common.SearchService;
using XIAP.FrontendModules.Search.Controller;
using XIAP.FrontendModules.Tasks.Service;
using XIAP.Frontend.CommonInfrastructure.Controller.CoverageVerification;

namespace GeniusX.AXA.FrontendModules.Claims.Controller
{
    public class AXAClaimController : ClaimController
    {
        #region Private variables
        private static readonly ILogger _Logger = LoggerFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private readonly ClaimsPayloadManager _payloadManager;
        private const string CLAIMS_AMOUNT_SEARCH_NAME = "[Claims].[ListClaimSummaryAmounts]";
        private const string CLAIMANTS = "Claimants";
	private const string LIABILITY_CLAIMANTS = "LiabClaimants";
        private const int NUMBER_OF_GENERIC_DATA_ITEM_NODES_TOBE_LOADED = 50;

        protected ISearchServiceHandler searchService;
	protected AXAGenericDataSetController AXAGenericDataSetController;
        private AXAClaimModel claimModel;
        protected AppModel appModel;
        private AXAGenericDataItemModel _gdiModel;
        private IClaimNameSearchHandler _nameSearchHandler;
        private IGenericDataSetBuilder _genericDataSetBuilder;
        private INameService _nameService;
        private AXAClaimAmountHistoryController claimAmountHistoryController;
        private ClaimPaymentRequestData claimPaymentRequest;
        private RetrievalManager retrievalManager;
        #endregion


        public AXAClaimController(IUnityContainer unityContainer, IClaimClientService claimClientService, IMetadataClientService metadataClientService, ApplicationModel applicationModel, AXAClaimModel claimModel, ClaimsPayloadManager claimPayloadManager, ClaimsRetrievalManager claimsRetrievalManager, IClaimNameSearchHandler claimNameSearchHandler, AppModel appModel, IClaimViewResolver viewResolver, AXAClaimAmountHistoryController claimAmountHistoryController, ISearchServiceHandler searchService, IClaimTreeviewResolver treeviewResolver, INameService nameService, IEventAggregator eventAggregator, IShellRulesHelper rulesHelper, IClaimsCodeLookupService claimscodelookupservice)
            : base(claimClientService, metadataClientService, claimModel, claimPayloadManager, claimsRetrievalManager, claimNameSearchHandler, appModel, viewResolver, claimAmountHistoryController, treeviewResolver, eventAggregator, rulesHelper, searchService, unityContainer, claimscodelookupservice)
        {
            this.appModel = appModel;
            this._gdiModel = Container.Resolve<AXAGenericDataItemModel>();
            this.claimModel = claimModel;
            this.claimModel.GenericDataItemModel = this._gdiModel;
            this._payloadManager = claimPayloadManager;
            this._nameSearchHandler = claimNameSearchHandler;
            this.claimModel.ReloadTotals += new EventHandler(this.Model_ReloadTotals);
            this.claimModel.ReloadClaimants += new EventHandler(this.ClaimModel_ReloadClaimants);
            this.claimModel.DuplicateClaimCheckClick += new EventHandler<XIAP.Frontend.Infrastructure.Events.CommandEventArgs<ClaimHeaderDto>>(this.ClaimModel_DuplicateClaimCheckClick);
            this.claimModel.CustomCoverageVerification += new EventHandler(this.ClaimModel_CustomCoverageVerification);
            this.claimModel.NameInvolvementModel.GetNameForNameInvolvement += new EventHandler(this.NameInvolvementModel_GetNameForNameInvolvement);
            this.searchService = searchService;
            this.claimModel.TargetHeaderStatusCodeChanged += new EventHandler(this.ClaimModel_TargetHeaderStatusCodeChanged);
            this._genericDataSetBuilder = new ClaimsDataBuilder(claimPayloadManager);
            this._gdsController = new AXAGenericDataSetController((ClaimControllerBase<ClaimModel>)this, claimModel, this._gdiModel, this.Container, metadataClientService, this._genericDataSetBuilder);
            this.SetNumberOfGenericDataItemNodesToBeLoaded();
            this._nameService = nameService;
            this.claimModel.ValidateClaim += new EventHandler(this.ClaimModel_ValidateClaim);
            this.claimAmountHistoryController = claimAmountHistoryController;
            this.claimAmountHistoryController.OnPaymentCancellation += new EventHandler(this.ClaimAmountHistoryController_OnPaymentCancellation);
            (this.claimModel.DocumentModel as AXADocumentModel).OnSaveClick += new EventHandler(this.AXAClaimController_OnSaveClick);
            this.retrievalManager = claimsRetrievalManager;
            (this.claimModel.DocumentModel as AXADocumentModel).RetrievalManager = claimsRetrievalManager;
            this.Model.HeaderPropertyChanged += new EventHandler<PropertyChangedEventArgs>(this.AXAClaimController_HeaderPropertyChanged);
            this.claimModel.OnINACTReviewClick += new EventHandler(this.ClaimModel_OnINACTReviewClick);
            this.claimModel.OnInactRecoveryReviewClick += new EventHandler(this.ClaimModel_OnInactRecoveryReviewClick);
        }

        private void ClaimModel_OnInactRecoveryReviewClick(object sender, EventArgs e)
        {
            Request req = this.CreateRequestForVirtualPlugin("AXAInactiveRecoveryClaimEventCreation");
            this.PayloadManager.SynchroniseData(this.claimModel.HeaderDto,
                req,
            response =>
            {
                XIAPMessageBox.Show(GeniusX.AXA.FrontendModules.Claims.Resources.StringResources.Recovery_Estimation_Review, GeniusX.AXA.FrontendModules.Claims.Resources.StringResources.Recovery_Estimation_Event_Created, XIAPMessageBox.Buttons.OK, XIAPMessageBox.Icons.Information, null);
                this.claimModel.IsBusy = false;
            },
                this.HandleError,
                true);
        }

        protected override ClaimEventController<ClaimModel> GetClaimEventController(IUnityContainer container, AppModel appModel, IEventAggregator eventAggregator)
        {
            return new AXAClaimEventController(this, this.Model, this.Model.EventModel, container, this.ClaimClientService, this.MetadataService, appModel, this.eventAggregator);
        }

        /// <summary>
        /// Creating Reserve Review Event
        /// </summary>
        /// <param name="sender">Object Sender</param>
        /// <param name="e">Event Args</param>
        private void ClaimModel_OnINACTReviewClick(object sender, EventArgs e)
        {
            Request req = this.CreateRequestForVirtualPlugin("AXAClaimEventCreation");
            this.PayloadManager.SynchroniseData(this.claimModel.HeaderDto,
                req,
            response =>
            {
                XIAPMessageBox.Show(GeniusX.AXA.FrontendModules.Claims.Resources.StringResources.Estimation_Reviewed, GeniusX.AXA.FrontendModules.Claims.Resources.StringResources.Estimation_Event_Created, XIAPMessageBox.Buttons.OK, XIAPMessageBox.Icons.Information, null);
                this.claimModel.IsBusy = false;
            },
                this.HandleError,
                true);
        }

        private Request CreateRequestForVirtualPlugin(String alias)
        {
            this.claimModel.IsBusy = true;
            this.claimModel.HeaderData.BusinessDataState = BusinessDataState.Modified;
            Payload payload = new Payload(null, this.claimModel.HeaderDto, null);
            Request req = new Request();
            req.VirtualProcessRequest = VirtualProcessRequestBuilder.BuildComponentVirtualProcessRequest(this.PayloadManager, this.claimModel.ClaimDetailModel.SelectedClaimDetailDto.Data.DataId, alias);
            req.VirtualProcessRequest.SkipComponentValidation = true;
            req.Payloads = new List<Payload>();
            req.Payloads.Add(payload);
            req.TransactionID = this.claimModel.TransactionId;
            return req;
        }


        /// <summary>
        /// Open the current node.
        /// </summary>
        /// <param name="currentNode">type of string</param>
        private void OpenCurrentNode(string currentNode)
        {
            if (currentNode == NavigationNodeNames.EventDetails)
            {
                if (this.claimModel.EventModel != null && this.claimModel.EventModel.SelectedEventDto != null)
                {
                    var axaEventModel = this.claimModel.EventModel as AXAEventModel;
                    axaEventModel.ClaimEventPropertyChanged(this.claimModel.EventModel.SelectedEventDto.EventData, new PropertyChangedEventArgs("CustomCode02"));
                }
            }
        }

        private void SetNumberOfGenericDataItemNodesToBeLoaded()
        {
            ConfigurationSetting configurationSetting = this.appModel.ShellConfiguration.ConfigurationSettings["NumberOfGenericDataItemNodesToBeLoaded"];

            if (configurationSetting != null && configurationSetting.SettingParmeters[0] != null && !string.IsNullOrWhiteSpace(configurationSetting.SettingParmeters[0].SettingValue))
            {
                this.claimModel.GenericDataItemModel.NumberOfGenericDataItemNodesToBeLoaded = int.Parse(configurationSetting.SettingParmeters[0].SettingValue);
            }

            if (configurationSetting == null || configurationSetting.SettingParmeters[0] == null || configurationSetting.SettingParmeters[0].SettingValue == null || this.claimModel.GenericDataItemModel.NumberOfGenericDataItemNodesToBeLoaded == 0)
            {
                this.claimModel.GenericDataItemModel.NumberOfGenericDataItemNodesToBeLoaded = NUMBER_OF_GENERIC_DATA_ITEM_NODES_TOBE_LOADED;
            }
        }

        private void AXAClaimController_HeaderPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Dictionary<Field, string> childfields = new Dictionary<Field, string>();

            if (e.PropertyName == this.Model.ClaimHeaderFields.ClaimHeaderAnalysisCode04.PropertyName)
            {
                childfields.Add(this.claimModel.ClaimHeaderFields.ClaimHeaderAnalysisCode05, this.Model.HeaderData.ClaimHeaderAnalysisCode04);
                childfields.Add(this.claimModel.ClaimHeaderFields.ClaimHeaderAnalysisCode06, null);
                this.SetFieldMandatory(childfields);
            }
            else if (e.PropertyName == this.Model.ClaimHeaderFields.ClaimHeaderAnalysisCode05.PropertyName)
            {
                childfields.Add(this.claimModel.ClaimHeaderFields.ClaimHeaderAnalysisCode06, this.Model.HeaderData.ClaimHeaderAnalysisCode05);
                this.SetFieldMandatory(childfields);
            }
            else if (e.PropertyName == this.Model.ClaimHeaderFields.ClaimHeaderAnalysisCode02.PropertyName)
            {
                childfields.Add(this.claimModel.ClaimHeaderFields.CustomCode01, this.Model.HeaderData.ClaimHeaderAnalysisCode02);
                childfields.Add(this.claimModel.ClaimHeaderFields.CustomCode02, null);
                childfields.Add(this.claimModel.ClaimHeaderFields.CustomCode03, null);
                this.SetFieldMandatory(childfields);
                if (this.claimModel.HeaderDto.ClaimDetails.IsNullOrEmpty() == false)
                {
                    this.MakeInjuryAndDescEmpty();
                }
            }
            else if (e.PropertyName == this.Model.ClaimHeaderFields.CustomCode01.PropertyName)
            {
                childfields.Add(this.claimModel.ClaimHeaderFields.CustomCode02, this.Model.HeaderData.CustomCode01);
                childfields.Add(this.claimModel.ClaimHeaderFields.CustomCode03, null);
                this.SetFieldMandatory(childfields);
            }
            else if (e.PropertyName == this.Model.ClaimHeaderFields.CustomCode02.PropertyName)
            {
                childfields.Add(this.claimModel.ClaimHeaderFields.CustomCode03, this.Model.HeaderData.CustomCode02);
                this.SetFieldMandatory(childfields);
            }
            else if (e.PropertyName == this.claimModel.ClaimHeaderFields.CustomReference01.PropertyName)
            {
                ClaimNameInvolvementDto claimNameInvolvementDto = this.claimModel.HeaderDto.ClaimNameInvolvements.SelectMany(c => c.ClaimNameInvolvements).Cast<ClaimNameInvolvementDto>().Where(a => a != null && a.ClaimNameInvolvementData.NameID != null
                                                                   && a.ClaimNameInvolvementData.NameInvolvementType == (short)StaticValues.NameInvolvementType.MajorInsured
                                                                   && a.ClaimNameInvolvementData.NameInvolvementMaintenanceStatus == (short)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest).FirstOrDefault();

                if (claimNameInvolvementDto != null && claimNameInvolvementDto.ClaimNameInvolvementData != null)
                    {
                        claimNameInvolvementDto.ClaimNameInvolvementData.CustomReference01 = this.claimModel.HeaderData.CustomReference01;
                    }
            }

            if (e.PropertyName == this.claimModel.ClaimHeaderFields.DateOfLossFrom.PropertyName)
            {
                this.EmptyDivisionAndRelatedFields();
                this.Model.InvokeOnRefreshBindings(this.Model.HeaderData, this.Model.ClaimHeaderFields.ClaimHeaderAnalysisCode04);
                this.UpdateDivisionAndRelatedFields();
                this.Model.InvokeOnRefreshBindings(this.Model.HeaderData, this.Model.ClaimHeaderFields.ClaimHeaderAnalysisCode04);
            }
        }

        /// <summary>
        /// Nullifies the CustomCode01(i.e.Injury Area) and CustomCode02(i.e. Injury Description) in Each ClaimDetail.
        /// </summary>
        private void MakeInjuryAndDescEmpty()
        {
            var claimDetails = this.claimModel.HeaderDto.ClaimDetails.ToList();
            foreach (var claimData in claimDetails)
            {
                ClaimDetailData claimdetaildata = claimData.ClaimDetailData as ClaimDetailData;
                claimdetaildata.CustomCode01 = null;
                claimdetaildata.CustomCode02 = null;
            }
        }

        private void SetFieldMandatory(Dictionary<Field, string> childFields)
        {
            Dictionary<string, Field> fields = this.MetadataManager.GetOriginalMetaDataFromCache(this.claimModel.HeaderData);
            List<Field> updatedFields = new List<Field>();
            int count = 0;
            foreach (Field childField in childFields.Keys)
            {
                if (fields.ContainsKey(childField.PropertyName) == false)
                {
                    // skip if the field is not present in the Fields collection, i.e. not in use.
                    count++;
                    continue;
                }

                Field field = fields[childField.PropertyName];
                string groupCode = childFields.Where(f => f.Key == childField).FirstOrDefault().Value;

                if (string.IsNullOrWhiteSpace(groupCode))
                {
                    if (fields != null && fields.ContainsKey(field.PropertyName) && fields[field.PropertyName].LookupParameters != null)
                    {
                        field.LookupParameters.GroupCode = "XEMPTY";
                        fields[field.PropertyName].Mandatory = false;
                        updatedFields.Add(field);
                        count++;
                        if (count == childFields.Count())
                        {
                            this.UpdateFieldsCache(fields, updatedFields);
                            count = 0;
                        }
                    }
                }
                else
                {
                    field.LookupParameters.GroupCode = groupCode;
                    if (fields != null)
                    {
                        CodeLookupService.GetCodeList(field.LookupDefinitionKey, 
			    field.LookupParameters, 
			    response =>
                        {
                            if (fields != null && fields.ContainsKey(field.PropertyName) && fields[field.PropertyName].LookupParameters != null)
                            {
                                fields[field.PropertyName].Mandatory = !response.IsNullOrEmpty();
                                updatedFields.Add(field);
                                count++;
                                if (count == childFields.Count())
                                {
                                    this.UpdateFieldsCache(fields, updatedFields);
                                    count = 0;
                                }
                            }
                        });
                    }
                }
            }

            if (count == childFields.Count())
            {
                this.UpdateFieldsCache(fields, updatedFields);
            }
        }

        private void UpdateFieldsCache(Dictionary<string, Field> fields, List<Field> updatedFields)
        {
            this.MetadataManager.UpdateCache(new MetadataPayload() { ComponentID = this.Model.HeaderData.DataId, DataType = "ClaimHeader", DefinitionKey = this.Model.HeaderData.DefinitionKey, Fields = fields });
            updatedFields.ForEach(field => this.Model.InvokeOnRefreshBindings(this.Model.HeaderData, field));
        }


        private void AXAClaimController_OnSaveClick(object sender, EventArgs e)
        {
            this.Save();
        }

        protected override void OnSaveComplete()
        {
            this.SetPaymentAuthorisationTaskNotification();
            // rerun the Claim Amount History transaction only when Claim is saved with a financial transaction
            if (this.claimModel.LatestSavedClaimTransactionHeaderData != null)
            {
                this.claimModel.ClaimAmountSearchRows.Clear();
                this.GetSearchParameters();
            }

	    this.RefreshDocuments();

            base.OnSaveComplete();

            if (this.Navigator.CurrentNode.NodeName == NavigationNodeNames.EventDetails)
            {
                if (this.claimModel.EventModel != null && this.claimModel.EventModel.SelectedEventDto != null && string.IsNullOrEmpty(this.claimModel.EventModel.UserIdentity))
                {
                    // passing selectedeventdto to resolve the error during edit event
                    this.Navigator.Open(NavigationNodeNames.EventDetails, this.claimModel.EventModel.SelectedEventDto);
                }
            }
        }

	private void RefreshDocuments()
        {
            if (this.claimModel.DocumentModel == null)
            {
                return;
            }

            if (this.claimModel.DocumentModel.SelectedDocument != null)
            {
                this.claimModel.DocumentModel.OnRefreshDocument(null);
            }
            }

        protected override ClaimDocumentController<ClaimModel> GetClaimDocumentController(IUnityContainer container, AppModel appModel)
        {
            if (this.Model != null)
            {
                return new AXAClaimDocumentController(this, this.Model.DocumentModel as AXADocumentModel, this.RetrievalManager as ClaimsRetrievalManager, this.ClaimClientService, this.MetadataService, appModel, container, this.Model.DocumentModel.DocumentContainerModel);
            }

            return null;
        }

        protected override void OnFinishComplete()
        {
            this.SetPaymentAuthorisationTaskNotification();
            base.OnFinishComplete();
        }

        protected override void OnClaimLinkedRecoveriesAdd()
        {
            this.ValidateReserveLitigationHeaderStatus(base.OnClaimLinkedRecoveriesAdd);
        }

        protected override void OnClaimLinkedLitigationsAdd()
        {
            this.ValidateReserveLitigationHeaderStatus(base.OnClaimLinkedLitigationsAdd);
        }

        protected override void OnClaimLitigationAdd()
        {
            this.ValidateReserveLitigationHeaderStatus(base.OnClaimLitigationAdd);
        }

        private void ValidateReserveLitigationHeaderStatus(Action onValid)
        {
            if (this.Model.TargetStatusCode == AXAClaimConstants.CLAIM_STATUS_CLAIM_OPENED_UNCONFIRMED)
            {
                this.claimModel.IsBusy = false;
                XIAPMessageBox.Show("Error", GeniusX.AXA.FrontendModules.Claims.Resources.StringResources.ReserveLitigation_NotAllowed_On_UnconfirmedClaim, XIAPMessageBox.Buttons.OK, XIAPMessageBox.Icons.Error, null);

                return;
            }

            onValid.Invoke();
        }

        private void SetPaymentAuthorisationTaskNotification()
        {
            INotificationManager notificationManager = this.Container.Resolve<INotificationManager>();
            ITaskServiceHelper taskService = this.Container.Resolve<ITaskServiceHelper>();
            ClaimTransactionHeaderData claimTransactionHeaderData = this.claimModel.LatestSavedClaimTransactionHeaderData;

            if (claimTransactionHeaderData != null &&
                claimTransactionHeaderData.ClaimTransactionSource == (short)StaticValues.ClaimTransactionSource.Payment &&
                claimTransactionHeaderData.PaymentAuthorisationStatus != (short)StaticValues.PaymentAuthorisationStatus.PaymentAuthorised)
            {
                ConfigurationManager configurationManager = this.Container.Resolve<ConfigurationManager>();
                int checkInternval = Convert.ToInt32(configurationManager.GetValue(AXAClaimConstants.AUTHENTICATION_TASK_NOTIFICAION, new SettingParameter() { QualifierName = AXAClaimConstants.AUTHENTICATION_TASK_NOTIFICATION_CHECKINTERVAL }));
                int notificationAttempts = Convert.ToInt32(configurationManager.GetValue(AXAClaimConstants.AUTHENTICATION_TASK_NOTIFICAION, new SettingParameter() { QualifierName = AXAClaimConstants.AUTHENTICATION_TASK_NOTIFICATION_ATTEMPTS }));

                PaymentAuthorisationTaskNotificationRequest notificationRequest = new PaymentAuthorisationTaskNotificationRequest(taskService, claimTransactionHeaderData.ClaimTransactionHeaderID, notificationAttempts, this.claimModel.HeaderData.ClaimReference, this.claimPaymentRequest);
                notificationManager.AddNotificationRequest(notificationRequest, checkInternval);
            }
        }

        private void ClaimAmountHistoryController_OnPaymentCancellation(object sender, EventArgs e)
        {
            this.claimModel.ClaimAmountSearchRows.Clear();
            this.RetrieveHistoricalTransactionData();
        }

        private void ClaimModel_ValidateClaim(object sender, EventArgs e)
        {
            this.Model.RaiseExecuteChange();
            this.OpenCurrentNode(this.Navigator.CurrentNode.NodeName);
        }

        private void ClaimModel_TargetHeaderStatusCodeChanged(object sender, EventArgs e)
        {
            this.ValidateDuplicateCommand();
        }

        private ClaimNameInvolvementData GetClaimNameInvolvementByType(ObservableCollection<ClaimInvolvementDto> ClaimInvolvements, short nameInvolvementType)
        {
            if (ClaimInvolvements == null)
            {
                return null;
            }

            foreach (var claimNameInvolvements in ClaimInvolvements)
            {
                if (claimNameInvolvements.ClaimNameInvolvements == null)
                {
                    continue;
                }

                var claimNameInvolvementDto = claimNameInvolvements.ClaimNameInvolvements
                    .Where(cniInd => cniInd.ClaimNameInvolvementData.NameInvolvementMaintenanceStatus == (short)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest
                            && cniInd.ClaimNameInvolvementData.NameInvolvementType == nameInvolvementType).FirstOrDefault();
                if (claimNameInvolvementDto != null)
                {
                    return claimNameInvolvementDto.ClaimNameInvolvementData;
                }
            }

            return null;
        }

        private void SetGlobalSearchControllerArgs(ClaimHeaderDto claimHeaderDto, GlobalSearchControllerArgs args, Action<GlobalSearchControllerArgs> callbackArgs = null)
        {
            ObservableCollection<long> listOfNameIDs = new ObservableCollection<long>();
            var nameIdandNameNameInvolvementTypeCollection = new List<Tuple<long, short>>(); 

            // Get ClientID
            ClaimNameInvolvementData claimNameInvolvementData = this.GetClaimNameInvolvementByType(claimHeaderDto.ClaimInvolvements, (short)StaticValues.NameInvolvementType.MajorInsured);

            if (claimNameInvolvementData != null && claimNameInvolvementData.NameID != null)
            {
                args.SetProperty("ClientID", claimNameInvolvementData.NameID.Value);
            }

            // Get Driver
            claimNameInvolvementData = this.GetClaimNameInvolvementByType(claimHeaderDto.ClaimInvolvements, (short)StaticValues.NameInvolvementType.Driver);

            if (claimNameInvolvementData != null && claimNameInvolvementData.NameID != null)
            {
                listOfNameIDs.Add(claimNameInvolvementData.NameID.Value);
                nameIdandNameNameInvolvementTypeCollection.Add(new Tuple<long, short>(claimNameInvolvementData.NameID.Value, claimNameInvolvementData.NameInvolvementType.Value));
            }

            // Get All Additional claimant.
            var claimNameInvolvementDataList = this.GetAllClaimNameInvolvementDtoByType(claimHeaderDto.ClaimInvolvements, (short)StaticValues.NameInvolvementType.AdditionalClaimant);
            foreach (ClaimNameInvolvementData claimantNameInvolvementData in claimNameInvolvementDataList)
            {
                if (claimantNameInvolvementData.NameID.HasValue)
                {
                    listOfNameIDs.Add(claimantNameInvolvementData.NameID.Value);
                    nameIdandNameNameInvolvementTypeCollection.Add(new Tuple<long, short>(claimantNameInvolvementData.NameID.Value, claimantNameInvolvementData.NameInvolvementType.Value));
                }
            }

            if (listOfNameIDs.Count > 0)
            {
                string name = string.Empty;

                this._nameService.QueryNameDetails(listOfNameIDs, (data) =>
                {
                    if (data != null)
                    {
                        StringBuilder claimantsSurname = new StringBuilder();
                        data.ForEach(nameData =>
                            {
                                if (nameData.NameType == (int)XIAP.FrontendModules.Common.InsuranceDirectoryService.NameType.Company)
                                {
                                    name = nameData.CompanyDetailVersions.FirstOrDefault(c => c.IsLatestVersion == true).ListName;
                                }
                                else
                                {
                                    name = nameData.PersonDetailVersions.FirstOrDefault(c => c.IsLatestVersion == true).Surname;
                                }

                                List<short> nameInvolvementTypes = nameIdandNameNameInvolvementTypeCollection.Where(a => a.Item1 == nameData.NameID).Select(a => a.Item2).ToList();
                                foreach (var involvementType in nameInvolvementTypes)
                                {
                                    if (involvementType == (short)StaticValues.NameInvolvementType.AdditionalClaimant)
                                    {
                                        if (claimantsSurname.Length == 0)
                                        {
                                            claimantsSurname.Append(name);
                                        }
                                        else
                                        {
                                            claimantsSurname.Append("," + name);
                                        }
                                    }

                                    if (involvementType == (short)StaticValues.NameInvolvementType.Driver)
                                    {
                                        this.SetDriver(args, name, claimHeaderDto);
                                    }
                                }
                            });

                        if (claimantsSurname.Length == 0)
                        {
                            this.SetClaimant(args, null);
                        }
                        else
                        {
                            this.SetClaimant(args, claimantsSurname.ToString());
                        }
                    }

                    if (args.ContainsProperty("DriverSurname"))
                    {
                        List<ClaimIOVehicleDto> ioVehicleDtoLiteDataList = new List<ClaimIOVehicleDto>();
                        List<ClaimIOVehicleDto> ioVehicleDtoHeavyDataList = new List<ClaimIOVehicleDto>();
                        if (claimHeaderDto.ClaimIOInvolvements.IsNullOrEmpty() == false)
                        {
                            claimHeaderDto.ClaimIOInvolvements.ForEach(ioInv =>
                            {
                                if (ioInv.ClaimInsuredObject != null && ioInv.ClaimInsuredObject.ClaimIOVehicles.IsNullOrEmpty() == false)
                                {
                                    ioInv.ClaimInsuredObject.ClaimIOVehicles.ForEach(ioVehicle =>
                                    {
                                        if (ioVehicle.IsFull)
                                        {
                                            ioVehicleDtoHeavyDataList.Add(ioVehicle);
                                        }
                                        else
                                        {
                                            ioVehicleDtoLiteDataList.Add(ioVehicle);
                                        }
                                    });
                                }
                            });

                            if (ioVehicleDtoLiteDataList.IsNullOrEmpty())
                            {
                                this.SetRegistrationNbr(args, ioVehicleDtoHeavyDataList);
                                if (callbackArgs != null)
                                {
                                    callbackArgs(args);
                                }
                            }
                            else
                            {
                                int count = ioVehicleDtoLiteDataList.Count;
                                this.ValidateAndLoadHeavy(Guid.Empty,
                                    ioVehicleDtoLiteDataList,
                                    RetrievalType.Component,
                                    BusinessDataVariant.Full,
                                    (DtoBase response) =>
                                    {
                                        ioVehicleDtoHeavyDataList.Add((ClaimIOVehicleDto)response);
                                        count--;
                                        ioVehicleDtoLiteDataList.RemoveAt(count);
                                        if (ioVehicleDtoLiteDataList.IsNullOrEmpty())
                                        {
                                            this.SetRegistrationNbr(args, ioVehicleDtoHeavyDataList);
                                            if (callbackArgs != null)
                                            {
                                                callbackArgs(args);
                                            }
                                        }
                                    });
                            }
                        }
			else if (callbackArgs != null)
                        {
                            callbackArgs(args);
                        }
                    }
                    else if (callbackArgs != null)
                    {
                        callbackArgs(args);
                    }
                });
            }
            else
            {
                if (callbackArgs != null)
                {
                    callbackArgs(args);
                }
            }
        }

        private IEnumerable<ClaimNameInvolvementData> GetAllClaimNameInvolvementDtoByType(ObservableCollection<ClaimInvolvementDto> ClaimInvolvements, short nameInvolvementType)
        {
            if (ClaimInvolvements == null)
            {
                return null;
            }

            List<ClaimNameInvolvementData> claimantNIDataList = new List<ClaimNameInvolvementData>();
            foreach (var claimNameInvolvements in ClaimInvolvements)
            {
                if (claimNameInvolvements.ClaimNameInvolvements == null)
                {
                    continue;
                }

                var claimNameInvolvementDto = claimNameInvolvements.ClaimNameInvolvements
                    .Where(cniInd => cniInd.ClaimNameInvolvementData.NameInvolvementMaintenanceStatus == (short)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest
                            && cniInd.ClaimNameInvolvementData.NameInvolvementType == nameInvolvementType).FirstOrDefault();
                if (claimNameInvolvementDto != null)
                {
                    claimantNIDataList.Add(claimNameInvolvementDto.ClaimNameInvolvementData);
                }
            }

            return claimantNIDataList;
        }

        private void ClaimModel_DuplicateClaimCheckClick(object sender, XIAP.Frontend.Infrastructure.Events.CommandEventArgs<ClaimHeaderDto> e)
        {
            this.PrepareDuplicateClaimSearch(e.Dto as ClaimHeaderDto);
        }

        private void LoadNameInvolvementHeavyData(List<ClaimInvolvementDto> dtoList, ClaimHeaderDto claimHeaderDto)
        {
            int count = 0;
            this.ValidateAndLoadHeavy<ClaimInvolvementDto>(Guid.Empty,
                dtoList,
                        RetrievalType.WithChildHierarchy,
                        BusinessDataVariant.Full,
                        (DtoBase value) =>
                        {
                            count++;
                            if (count == dtoList.Count)
                            {
                                StartDuplicateClaimSearch(claimHeaderDto);
                            }
                        });
        }

        private void PrepareDuplicateClaimSearch(ClaimHeaderDto claimHeaderDto)
        {
            List<ClaimInvolvementDto> claimNIList = new List<ClaimInvolvementDto>();

            claimHeaderDto.ClaimInvolvements.ForEach(d =>
            {
                if (!d.IsFull)
                {
                    claimNIList.Add(d);
                }
            });

            if (claimNIList.Count > 0)
            {
                this.LoadNameInvolvementHeavyData(claimNIList, claimHeaderDto);
            }
            else
            {
                this.StartDuplicateClaimSearch(claimHeaderDto);
            }
        }

        private void StartDuplicateClaimSearch(ClaimHeaderDto claimHeaderDto)
        {
            GlobalSearchControllerArgs args = new GlobalSearchControllerArgs();
            args.SearchCriteria = new XIAP.FrontendModules.Infrastructure.Search.UiSearchCriteria();

            this.SetGlobalSearchControllerArgs(claimHeaderDto, 
		 args, 
		 (returnArg) =>
            {
                ClaimHeaderData claimHeaderData = claimHeaderDto.Data as ClaimHeaderData;
                returnArg.SetProperty("DateOfLoss", DateTime.Parse(claimHeaderData.DateOfLossFrom.Value.Date.ToShortDateString()));
                this.StartGraph(returnArg, claimHeaderData);
            });
        }

        private void SetClaimant(GlobalSearchControllerArgs args, string claimaint)
        {
            args.SetProperty("ClaimantSurname", claimaint);
        }

        private void SetRegistrationNbr(GlobalSearchControllerArgs args, List<ClaimIOVehicleDto> vehicleDtoHeavyDataList)
        {
            StringBuilder DclRegistrationNbr = new StringBuilder();
            vehicleDtoHeavyDataList.ForEach(ioVehicleData =>
                    {
                        if (ioVehicleData.ClaimIOVehicleData != null)
                        {
                            if (DclRegistrationNbr.Length == 0)
                            {
                                DclRegistrationNbr.Append(ioVehicleData.ClaimIOVehicleData.RegistrationNumber);
                            }
                            else
                            {
                                DclRegistrationNbr.Append("," + ioVehicleData.ClaimIOVehicleData.RegistrationNumber);
                            }
                        }
                    });

            if (DclRegistrationNbr.Length == 0)
            {
                args.SetProperty("DclRegistrationNbr", null);
            }
            else
            {
                args.SetProperty("DclRegistrationNbr", DclRegistrationNbr.ToString());
            }
        }

        private void SetDriver(GlobalSearchControllerArgs args, string driver, ClaimHeaderDto claimHeaderDto)
        {
            args.SetProperty("DriverSurname", driver);
        }

        private void StartGraph(GlobalSearchControllerArgs args, ClaimHeaderData claimHeaderData)
        {
            args.SetProperty("ClientReference", claimHeaderData.CustomReference01);
            args.SetProperty("OutsourceReference", claimHeaderData.CustomReference02);

            // For Search Tab Title
            args.SetProperty("ClaimTitle", claimHeaderData.ClaimTitle);
            args.SetProperty("ProductCode", this.claimModel.ProductCode);
            args.SetProperty("ClaimReference", claimHeaderData.ClaimReference);

            TaskArgumentsHolder argsHolder = new TaskArgumentsHolder(null, args);
            NavigationManager manager = this.Container.Resolve<NavigationManager>();

            // Starting the search graph.
            manager.Start("DuplicateClaimSearchGraph", argsHolder);
        }

        private void SetClaimPaymentRequest()
        {
            if (this.claimModel.HeaderDto != null && this.claimModel.HeaderDto.InProgressClaimTransactionHeaders != null)
            {
                ClaimTransactionHeaderDto claimTransHeaderDto = this.claimModel.HeaderDto.InProgressClaimTransactionHeaders.FirstOrDefault();
                if (claimTransHeaderDto != null && claimTransHeaderDto.ClaimTransactionToPaymentRequestLinks != null)
                {
                    ClaimTransactionToPaymentRequestLinkDto claimTransToPaymentReqDto = claimTransHeaderDto.ClaimTransactionToPaymentRequestLinks.FirstOrDefault();
                    if (claimTransToPaymentReqDto != null && claimTransToPaymentReqDto.ClaimPaymentRequest != null)
                    {
                        this.claimPaymentRequest = claimTransToPaymentReqDto.ClaimPaymentRequest.Data as ClaimPaymentRequestData;
                    }
                }
            }
        }

        protected override void MainClaimHandlerName_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "NameID")
            {
                base.MainClaimHandlerName_PropertyChanged(sender, e);
                (this.Model.EventModel as AXAEventModel).DefaultTaskInitialUserID = null;
            }
        }

        protected override void MajorInsuredName_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "NameID")
            {
                this.Model.NameInvolvementModel.MajorInsuredName.NameInvolvementType = (short)StaticValues.NameInvolvementType.MajorInsured;
                this.AddOrUpdateInvolvement(this.Model.HeaderDto,
                    this.Model.NameInvolvementModel.MajorInsuredName, (cni) =>
                    {
                        // prevent navigation after added client
                        this.UpdateDivisionAndRelatedFields();
                        this.SetClientDetails();
                        this.Model.InvokeOnRefreshBindings(this.Model.HeaderData, this.Model.ClaimHeaderFields.ClaimHeaderAnalysisCode04);
                        ValidateDuplicateCommand();
                    }, true,
                    false);
            }
        }

        // This method updates the division fields depending upon groupcode and dateoflossfrom
        private void UpdateDivisionAndRelatedFields()
        {
            var insurd = this.Model.HeaderDto.ClaimNameInvolvements.Where(a => a.ClaimNameInvolvment != null &&
                a.ClaimNameInvolvment.ClaimNameInvolvementData.NameInvolvementType == (short)StaticValues.NameInvolvementType.MajorInsured &&
                a.ClaimNameInvolvment.ClaimNameInvolvementData.NameInvolvementMaintenanceStatus == (short)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest).FirstOrDefault();

            
            if (insurd != null)
            {
                DateTime? dateOfLossFrom = this.Model.HeaderData.DateOfLossFrom;
                string nameInvolvementUsageCustomValue = insurd.ClaimNameInvolvment.ClaimNameInvolvementData.NameInvolvementUsageCustomValue;
                if (nameInvolvementUsageCustomValue != null && this.Model.ClaimHeaderFields != null && this.Model.ClaimHeaderFields.ClaimHeaderAnalysisCode04 != null && this.Model.ClaimHeaderFields.ClaimHeaderAnalysisCode04.LookupParameters != null)
                {
                    this.Model.ClaimHeaderFields.ClaimHeaderAnalysisCode04.LookupParameters.GroupCode = nameInvolvementUsageCustomValue;
                    this.Model.ClaimHeaderFields.ClaimHeaderAnalysisCode04.LookupParameters.EffectiveDate = dateOfLossFrom;
                    // Below change is to avoid the performance issue so that not to reload the dynamic screen
                    Dictionary<string, Field> fields = this.MetadataManager.GetOriginalMetaDataFromCache(this.Model.HeaderData);
                    if (fields != null)
                    {
                        if (fields.ContainsKey("ClaimHeaderAnalysisCode04") && fields["ClaimHeaderAnalysisCode04"].LookupParameters != null)
                        {
                            fields["ClaimHeaderAnalysisCode04"].LookupParameters.GroupCode = nameInvolvementUsageCustomValue;
                            fields["ClaimHeaderAnalysisCode04"].LookupParameters.EffectiveDate = dateOfLossFrom;
                            this.MetadataManager.UpdateCache(new MetadataPayload() { ComponentID = this.Model.HeaderData.DataId, DataType = "ClaimHeader", DefinitionKey = this.Model.HeaderData.DefinitionKey, Fields = fields });
                        }
                    }
                }
            }
            else
            {
                this.EmptyDivisionAndRelatedFields();
            }
        }

        // This method makes Division,Sub Division and Cost Center fields empty
        private void EmptyDivisionAndRelatedFields()
        {
            this.Model.HeaderData.ClaimHeaderAnalysisCode04 = null;
            if (this.Model.ClaimHeaderFields != null && this.Model.ClaimHeaderFields.ClaimHeaderAnalysisCode04 != null && this.Model.ClaimHeaderFields.ClaimHeaderAnalysisCode04.LookupParameters != null)
            {
                this.Model.ClaimHeaderFields.ClaimHeaderAnalysisCode04.LookupParameters.GroupCode = AXAClaimConstants.EmptyGroupCode;
            }

            this.Model.HeaderData.ClaimHeaderAnalysisCode05 = null;
            if (this.Model.ClaimHeaderFields != null && this.Model.ClaimHeaderFields.ClaimHeaderAnalysisCode05 != null && this.Model.ClaimHeaderFields.ClaimHeaderAnalysisCode05.LookupParameters != null)
            {
                this.Model.ClaimHeaderFields.ClaimHeaderAnalysisCode05.LookupParameters.GroupCode = AXAClaimConstants.EmptyGroupCode;
            }

            this.Model.HeaderData.ClaimHeaderAnalysisCode06 = null;
            if (this.Model.ClaimHeaderFields != null && this.Model.ClaimHeaderFields.ClaimHeaderAnalysisCode06 != null && this.Model.ClaimHeaderFields.ClaimHeaderAnalysisCode06.LookupParameters != null)
            {
                this.Model.ClaimHeaderFields.ClaimHeaderAnalysisCode06.LookupParameters.GroupCode = AXAClaimConstants.EmptyGroupCode;
            }
        }

        protected override void ProcessSave(bool finishAndResume)
        {
            if (this.InvokeDuplicateSearch())
            {
                this.ClaimValidate(
                (r) =>
                {
                    // Need not to save on cancel, until user comes back and click 'ok' button ( confirmed from Raj ).
                    if (r.Status == BusinessTransactionStatus.Valid)
                    {
                        this.claimModel.IsBusy = false;
                        XIAPMessageBox.Show(GeniusX.AXA.FrontendModules.Claims.Resources.StringResources.SaveWithWarningDialogTitle,
                        GeniusX.AXA.FrontendModules.Claims.Resources.StringResources.SaveWithWarningDialogText,
                        XIAPMessageBox.Buttons.OkCancel,
                        new XIAPMessageBoxClosing(delegate(object msender, ExitCode exit)
                        {
                            if (exit == ExitCode.Cancel)
                            {
                                // show duplicate claim graph.
                                this.PrepareDuplicateClaimSearch(this.Model.HeaderDto);
                            }
                            else
                            {
                                this.SetClaimPaymentRequest();
                                base.ProcessSave(finishAndResume);
                                this.ValidateDuplicateCommand(true);
                            }
                            
                            return true;
                        }));
                    }
                    else if (r.Status == BusinessTransactionStatus.Warnings)
                    {
                        this.SetClaimPaymentRequest();
                        base.ProcessSave(finishAndResume);
                    }
                });
            }
            else
            {
                this.SetClaimPaymentRequest();
                base.ProcessSave(finishAndResume);
            }
        }
           
        protected override void NavigateToFirstNodeOnStart()
        {
            if (this.Model.TransactionType == ClaimTransactionType.CreateClaim)
            {
                this.DisplayClaimHeader(this.Model.HeaderDto);
            }
            else if (this.claimsControllerArgs != null && this.claimsControllerArgs.ClaimDetailData != null && this.claimsControllerArgs.ClaimEventData != null && (this.claimsControllerArgs.ClaimEventData as ClaimEventData).EventTypeCode == AXAClaimConstants.INACT_EVENTTYPE_CODE)
            {
                ClaimDetailDto claimDetailDto = new ClaimDetailDto();
                claimDetailDto.Data = this.claimsControllerArgs.ClaimDetailData as ClaimDetailData;
                string nodename = "CurrentReserve";
                // UK-JIRA 1371 & SN 300217256: Inactivity Review Task needs to be updated to point to correct Claim Detail
                Guid dataId = this.Model.HeaderDto.ClaimDetails.Where(x => x.ClaimDetailData.ClaimDetailID == this.claimsControllerArgs.ClaimDetailData.ClaimDetailID).FirstOrDefault().Data.DataId;
                this.Model.TreeModel.SetSelectedTreeNode(nodename, dataId);
                this.SetReserveAndPaymentVisibility(false, this.claimModel.IsEditable);
            }
            else if (this.claimsControllerArgs != null && this.claimsControllerArgs.ClaimDetailData != null && this.claimsControllerArgs.ClaimEventData != null && (this.claimsControllerArgs.ClaimEventData as ClaimEventData).EventTypeCode == AXAClaimConstants.REC_INACT_EVENTTYPE_CODE)
            {
                ClaimDetailDto claimDetailDto = new ClaimDetailDto();
                claimDetailDto.Data = this.claimsControllerArgs.ClaimDetailData as ClaimDetailData;
                string nodename = "CurrentRecoveryReserve";
                // UK-JIRA 1371 & SN 300217256: Recovery Inactivity Task needs to be updated to point to correct Claim Detail
                Guid dataId = this.Model.HeaderDto.ClaimDetails.Where(x => x.ClaimDetailData.ClaimDetailID == this.claimsControllerArgs.ClaimDetailData.ClaimDetailID).FirstOrDefault().Data.DataId;
                this.Model.TreeModel.SetSelectedTreeNode(nodename, dataId);
                this.SetReserveAndPaymentVisibility(false, this.claimModel.IsEditable);
            }
            else if (this.claimsControllerArgs != null && this.claimsControllerArgs.ClaimTransactionHeaderInfo == null && this.claimsControllerArgs.ClaimDetailData == null && this.claimsControllerArgs.ClaimEventData != null && ((this.claimsControllerArgs.ClaimEventData as ClaimEventData).EventTypeCode == AXAClaimConstants.INACT_EVENTTYPE_CODE || (this.claimsControllerArgs.ClaimEventData as ClaimEventData).EventTypeCode == AXAClaimConstants.REC_INACT_EVENTTYPE_CODE))
            {
                this.BaseNavigateToFirstNodeOnStart();
            }
            else
            {
                this.AssignClaimantDetails();
                base.NavigateToFirstNodeOnStart();
            }
        }

        protected void DisplayClaimHeader(ClaimHeaderDto claimHeaderDto)
        {
            this.Model.TreeModel.SetSelectedTreeNode(NavigationNodeNames.ClaimHeader, claimHeaderDto.Data.DataId);
            ActionKey key = this.claimModel.ClaimTreeModel.TreeLookup.Keys.Where(k => k.Identifier == this.Model.HeaderDto.Data.DataId && k.NodeName == this.Navigator.CurrentNode.NodeName).FirstOrDefault();
            if (key != null && key.NodeInfo != null)
            {
                key.NodeInfo.Sync(key.NodeInfo.Context);
            }
        }
  

        private bool SearchDriverOrAdditionalClaimant(ClaimNameInvolvementDto claimNameInvolvementDto)
        {
            if ((claimNameInvolvementDto.ClaimNameInvolvementData.NameInvolvementType == (short)StaticValues.NameInvolvementType.AdditionalClaimant
                || claimNameInvolvementDto.ClaimNameInvolvementData.NameInvolvementType == (short)StaticValues.NameInvolvementType.Driver)
                    && claimNameInvolvementDto.ClaimNameInvolvementData.NameInvolvementMaintenanceStatus == (short?)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest)
            {
                return true;
            }

            return false;
        }

        private void NameInvolvementModel_GetNameForNameInvolvement(object sender, EventArgs e)
        {
            Guid nameGuid = this.claimModel.NameInvolvementModel.SelectedClaimNameInvolvementData.DataId;
            var claimLinkedDetails = this.claimModel.HeaderDto.ClaimDetails.Where(cd => cd.ClaimDetailToClaimInvolvementLinks.Any(x => x.ClaimInvolvement != null && (x.ClaimInvolvement.Data as ClaimInvolvementData).ClaimInvolvementType == 1
                    && (x.ClaimInvolvement.ClaimNameInvolvment.Data.DataId == nameGuid
                    && x.ClaimInvolvement.ClaimNameInvolvements.Any(b => this.SearchDriverOrAdditionalClaimant(b)))));

            if (this.claimModel.NameInvolvementModel.SelectedClaimNameInvolvementData != null && this.claimModel.NameInvolvementModel.SelectedClaimNameInvolvementData.NameId.HasValue == true)
            {
                if (claimLinkedDetails != null && claimLinkedDetails.Count() > 0)
                {
                    this._nameSearchHandler.QueryNameDetails(this.claimModel.NameInvolvementModel.SelectedClaimNameInvolvementData.NameId.Value,
                        (ClaimNameData nameData) =>
                        {
                            foreach (ClaimDetailDto cd in claimLinkedDetails)
                            {
                                ClaimDetailData cdData = cd.Data as ClaimDetailData;
                                if (!string.IsNullOrEmpty(nameData.ListName))
                                {
                                    cdData.ClaimDetailTitle = cdData.ClaimDetailTypeCode + " - " + nameData.ListName;
                                }
                                else
                                {
                                    cdData.ClaimDetailTitle = cdData.ClaimDetailTypeCode;
                                }

                                cdData.NotifyPropertyChange("ClaimDetailTreeNodeTitle");
                            }
                        });
                }
            }
            else if (this.claimModel.NameInvolvementModel.SelectedClaimNameInvolvementData.NameId.HasValue == false)
            {
                foreach (ClaimDetailDto cd in claimLinkedDetails)
                {
                    ClaimDetailData cliamdetailData = cd.Data as ClaimDetailData;
                    cliamdetailData.ClaimDetailTitle = cliamdetailData.ClaimDetailTypeCode + " - " + XIAP.Frontend.CoreControls.Resources.StringResources.TreeUntitled;
                    cliamdetailData.NotifyPropertyChange("ClaimDetailTreeNodeTitle");
                }
            }

            if (this.claimModel.NameInvolvementModel.SelectedClaimNameInvolvementData != null && this.claimModel.NameInvolvementModel.SelectedClaimNameInvolvementData.NameUsageTypeCode == AXAClaimConstants.NAMEUSAGETYPECODE_MAJORINSURED)
            {
                this.SynchroniseClientSpecficData();
            }
        }

        private SearchCriteria BuildUserSearchCriteria(SearchParameters parameters)
        {
            SearchCriteria searchCriteria = new SearchCriteria();
            searchCriteria.IsPagingEnabled = false;
            searchCriteria.SortOrderList = new System.Collections.Generic.Dictionary<string, bool>();
            parameters.SetCriteriaValue("UserSearchIdentity", ((ClaimHeaderData)this.claimModel.HeaderDto.Data).CustomReference04);
            parameters.SetCriteriaValue("XIAPStartDate", Xiap.Framework.XiapConstants.StartOfTime);
            parameters.SetCriteriaValue("XIAPEndDate", Xiap.Framework.XiapConstants.EndOfTime);
            searchCriteria.ValueList = parameters.SearchCriteriaValueList;
            return searchCriteria;
        }

        public override bool TryGetCustomField(BusinessData data, string fieldName, out Field field)
        {
            if (this.claimModel.HeaderData.CustomProperties == null)
            {
                this.claimModel.HeaderData.CustomProperties = new Dictionary<string, object>();
            }

            switch (fieldName)
            {
                case "CustomProperties[AXA_CustomCode01UI]":
                    {
                        CodeLookupService.GetCodeList(
                         this.claimModel.ClaimHeaderFields.CustomCode01.LookupDefinitionKey,
                         this.claimModel.ClaimHeaderFields.CustomCode01.LookupParameters,
                         (ObservableCollection<CodeRow> collection) =>
                         {
                             if (this.claimModel != null)
                             {
                                 var codeRow = collection.FirstOrDefault(c => c.Code == this.claimModel.HeaderData.CustomCode01);
                                 this.claimModel.HeaderData.CustomProperties["AXA_CustomCode01UI"] = string.Empty;
                                 if (codeRow != null)
                                 {
                                     this.claimModel.HeaderData.CustomProperties["AXA_CustomCode01UI"] = codeRow.Description;
                                 }
                             }
                         });

                        field = (Field)this.claimModel.ClaimHeaderFields.CustomCode01.Clone();
                        field.Readonly = true;
                        field.PropertyName = "AXA_CustomCode01UI";
                    }

                    break;
                case "CustomProperties[AXA_CustomReference04UI]":
                    {
                        this.claimModel.HeaderData.CustomProperties["AXA_CustomReference04UI"] = string.Empty;

                        if (!string.IsNullOrEmpty(((ClaimHeaderData)this.claimModel.HeaderDto.Data).CustomReference04))
                        {
                            this.claimModel.IsBusy = true;
                            this.searchService.GetSearchParameters("ReassignUserSearch", (SearchParameters parameters) =>
                            {
                                this.searchService.InvokeSearch(this.appModel.UserProfile.Culture,
                    this.appModel.UserProfile.LanguageId,
                    parameters.SearchName,
                    this.BuildUserSearchCriteria(parameters),
                    searchData =>
                    {
                        if (searchData.SearchResultRowList.Count > 0)
                        {
                            var row = searchData.SearchResultRowList[0];
                            row.Columns.ForEach((column) =>
                            {
                                if (column.ColumnName == "UserDescription")
                                {
                                    this.claimModel.HeaderData.CustomProperties["AXA_CustomReference04UI"] = column.Value.ToString();
                                }
                            });
                        }
                        else
                        {
                            this.claimModel.HeaderData.CustomProperties["AXA_CustomReference04UI"] = ((ClaimHeaderData)this.claimModel.HeaderDto.Data).CustomReference04;
                        }

                        this.claimModel.HeaderData.InvokePropertyChanged("CustomProperties");
                        this.claimModel.IsBusy = false;
                    }, this.HandleAsyncError);
                            });
                        }

                        field = (Field)this.claimModel.ClaimHeaderFields.CustomReference04.Clone();
                        field.PropertyName = "AXA_CustomReference04UI";
                    }

                    break;
                case "CustomProperties[ClaimHandlerNameUI]":
                    {
                        var claimNameInvolvements = this.claimModel.HeaderDto.ClaimInvolvements.Where(c => c.IsFull && c.ClaimInvolvementData != null && c.ClaimInvolvementData.ClaimInvolvementType == (short)StaticValues.LinkableComponentType.NameInvolvement).SelectMany(b => b.ClaimNameInvolvements).ToList();
                        if (claimNameInvolvements != null)
                        {
                            var mainClaimHandler = claimNameInvolvements.FirstOrDefault(c => c.ClaimNameInvolvementData.NameInvolvementType == (short)StaticValues.NameInvolvementType_ClaimNameInvolvement.MainClaimHandler && c.ClaimNameInvolvementData.NameInvolvementMaintenanceStatus == (short)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest);

                            if (mainClaimHandler != null)
                            {
                                field = new Field();
                                field.TypeInfo = new DataTypeInfo(DataTypeEnum.String);
                                field.Readonly = true;
                                field.Visible = true;
                                field.PropertyName = "ClaimHandlerNameUI";
                                this.claimModel.HeaderData.CustomProperties["ClaimHandlerNameUI"] = mainClaimHandler.ClaimNameInvolvementData.ListName;
                            }
                            else
                            {
                                this.claimModel.HeaderData.CustomProperties["ClaimHandlerNameUI"] = String.Empty;
                            }
                        }
                    }

                    break;
                case "CustomProperties[AXA_ClaimHeaderAnalysisCode05UI]":
                    {
                        CodeLookupService.GetCodeList(
                         this.claimModel.ClaimHeaderFields.ClaimHeaderAnalysisCode05.LookupDefinitionKey,
                         this.claimModel.ClaimHeaderFields.ClaimHeaderAnalysisCode05.LookupParameters,
                         (ObservableCollection<CodeRow> collection) =>
                         {
                             if (this.claimModel != null)
                             {
                                 var codeRow = collection.FirstOrDefault(c => c.Code == this.claimModel.HeaderData.ClaimHeaderAnalysisCode05);
                                 this.claimModel.HeaderData.CustomProperties["AXA_ClaimHeaderAnalysisCode05UI"] = string.Empty;
                                 if (codeRow != null)
                                 {
                                     this.claimModel.HeaderData.CustomProperties["AXA_ClaimHeaderAnalysisCode05UI"] = codeRow.Description;
                                 }
                             }
                         });

                        field = (Field)this.claimModel.ClaimHeaderFields.ClaimHeaderAnalysisCode05.Clone();
                        field.Readonly = true;
                        field.PropertyName = "AXA_ClaimHeaderAnalysisCode05UI";
                    }

                    break;
                case "CustomProperties[AXA_ClaimHeaderAnalysisCode06UI]":
                    {
                        CodeLookupService.GetCodeList(
                         this.claimModel.ClaimHeaderFields.ClaimHeaderAnalysisCode06.LookupDefinitionKey,
                         this.claimModel.ClaimHeaderFields.ClaimHeaderAnalysisCode06.LookupParameters,
                         (ObservableCollection<CodeRow> collection) =>
                         {
                             if (this.claimModel != null)
                             {
                                 var codeRow = collection.FirstOrDefault(c => c.Code == this.claimModel.HeaderData.ClaimHeaderAnalysisCode06);
                                 this.claimModel.HeaderData.CustomProperties["AXA_ClaimHeaderAnalysisCode06UI"] = string.Empty;
                                 if (codeRow != null)
                                 {
                                     this.claimModel.HeaderData.CustomProperties["AXA_ClaimHeaderAnalysisCode06UI"] = codeRow.Description;
                                 }
                             }
                         });

                        field = (Field)this.claimModel.ClaimHeaderFields.ClaimHeaderAnalysisCode06.Clone();
                        field.Readonly = true;
                        field.PropertyName = "AXA_ClaimHeaderAnalysisCode06UI";
                    }

                    break;
                case "CustomProperties[AXA_ClaimHeaderAnalysisCode09UI]":
                    {
                        CodeLookupService.GetCodeList(
                         this.claimModel.ClaimHeaderFields.ClaimHeaderAnalysisCode09.LookupDefinitionKey,
                         this.claimModel.ClaimHeaderFields.ClaimHeaderAnalysisCode09.LookupParameters,
                         (ObservableCollection<CodeRow> collection) =>
                         {
                             if (this.claimModel != null)
                             {
                                 var codeRow = collection.FirstOrDefault(c => c.Code == this.claimModel.HeaderData.ClaimHeaderAnalysisCode09);
                                 this.claimModel.SelectedIOVersionDetailData.CustomProperties["AXA_ClaimHeaderAnalysisCode09UI"] = string.Empty;
                                 if (codeRow != null)
                                 {
                                     this.claimModel.SelectedIOVersionDetailData.CustomProperties["AXA_ClaimHeaderAnalysisCode09UI"] = codeRow.Description;
                                 }
                             }
                         });

                        field = (Field)this.claimModel.ClaimHeaderFields.ClaimHeaderAnalysisCode09.Clone();
                        field.Readonly = true;
                        field.PropertyName = "AXA_ClaimHeaderAnalysisCode09UI";
                    }

                    break;
            }

            return base.TryGetCustomField(data, fieldName, out field);
        }

        private void SynchroniseClientSpecficData()
        {
            this._payloadManager.SynchroniseData(this.claimModel.HeaderDto,
                this.claimModel.TransactionId, (Response response) =>
                {
                    response.Payloads.ForEach((p) =>
                    {
                        if (p.Relationship == "GenericDataItems" && p.DtoGraph.Data.BusinessDataState == BusinessDataState.Deleted)
                        {
                         }
                    });
                    if (this.claimModel.GenericDataItemModel.CollectionChanged != null)
                    {
                        this.claimModel.GenericDataItemModel.CollectionChanged("GenericDataItems", Guid.Empty);
                    }

                    this.claimModel.IsBusy = false;
                });
        }

        private void ClaimModel_ReloadClaimants(object sender, EventArgs e)
        {
            this.DetermineComponentsToLoad();
            this.Model.InvokeOnRefreshBindings(null, null); 
            this.Model.RaiseExecuteChange();  // Re-load the claimaints.
        }

        private void ClaimModel_CustomCoverageVerification(object sender, EventArgs e)
        {
            this.OnCoverageVerification(this.claimModel.HeaderDto, this.ReloadHeaderMetadata);
        }

        private void ReloadHeaderMetadata()
        {
            this.MetadataManager.ClearCache(new[] { this.claimModel.HeaderData });
            this.MetadataManager.GetMetaData(new[] { this.Model.HeaderData }, this.Model.TransactionId, (a, b) => this.Model.InvokeOnRefreshBindings(null, null));
            this.CoverageVerificationCompleted();
        }

        private void CoverageVerificationCompleted()
        {
            this.SetClientProtected();
        }

        private void SetClientProtected()
        {
            if (this.Model.HeaderData.PolicyHeaderID != null && this.Model.NameInvolvementModel != null && this.Model.NameInvolvementModel.MajorInsuredFields != null && this.Model.NameInvolvementModel.MajorInsuredFields.NameID != null)
            {
                this.Model.NameInvolvementModel.MajorInsuredFields.NameID.Readonly = true;
                this.Model.NameInvolvementModel.MajorInsuredFields.SetField("NameID", this.Model.NameInvolvementModel.MajorInsuredFields.NameID);
            }
        }

        private void Search()
        {
            // only perform the search when there is financial transaction exists
            if (this.claimModel.HasHistoricalFinancialTransactions)
            {
                this.claimModel.IsBusy = true;
                this.searchService.InvokeSearch(this.appModel.UserProfile.Culture, this.appModel.UserProfile.LanguageId, this.claimModel.SearchParameters.SearchName, this.BuildSearchCriteria(), this.SearchCompleted, this.HandleAsyncError);
            }
        }

        // Builds the search criteria.
        private SearchCriteria BuildSearchCriteria()
        {
            SearchCriteria searchCriteria = new SearchCriteria();
            searchCriteria.IsPagingEnabled = true;
            searchCriteria.SortOrderList = new System.Collections.Generic.Dictionary<string, bool>();
            searchCriteria.SortOrderList.Add("LstClaimDetailID", true);

            SearchParameters parameters = this.claimModel.SearchParameters;

            parameters.SetCriteriaValue("LstClaimHeaderID", this.claimModel.HeaderData.ClaimHeaderID.ToString());

            searchCriteria.ValueList = parameters.SearchCriteriaValueList;
            if (_Logger.IsDebugEnabled)
            {
                _Logger.Debug(string.Format("Retrieving the search parameters for claim {0}", this.claimModel.HeaderData.ClaimReference));
            }

            return searchCriteria;
        }

        protected void SearchCompleted(SearchData searchData)
        {
            if (this.claimModel != null)
            {
                // Build the search results into claim financial amounts.
                this.BuildClaimAmountSearchRowCollection(searchData);
                this.claimModel.SearchData = searchData;
                this.SetTotalFields();
                this.claimModel.IsBusy = false;
            }
        }

        private void BuildClaimAmountSearchRowCollection(SearchData searchData)
        {
            if (searchData.SearchResultRowList != null)
            {
                foreach (SearchResultRow row in searchData.SearchResultRowList)
                {
                    ClaimFinancialAmount resultsRow = new ClaimFinancialAmount(row);
                    this.claimModel.ClaimAmountSearchRows.Add(resultsRow);
                }
            }
        }

        private bool HandleAsyncError(AsyncCompletedEventArgs args)
        {
            this.claimModel.IsBusy = false;
            return false;
        }

        private bool InvokeDuplicateSearch()
        {
            AppModel appModel = this.Container.Resolve<AppModel>();
            // Get the valid claim status from shell config to invoke the search.
            var confSetting = appModel.ShellConfiguration.ConfigurationSettings["AutomaticDuplicateSearch"];

            if (this.Model.TransactionType == ClaimTransactionType.CreateClaim
                && this.Model.CurrentStatusCode == AXAClaimConstants.CLAIM_STATUS_CLAIM_OPENED)
            {
                return true;
            }

            foreach (SettingParameter parameter in confSetting.SettingParmeters)
            {
                if (parameter.QualifierName == "ClaimStatus" && parameter.QualifierValue == this.Model.CurrentStatusCode &&
                    parameter.SettingValue.Contains(this.claimModel.TargetStatusCode))
                {
                    return true;
                }
            }

            return false;
        }

        protected override void OnCVPolicySearchFinish(ControllerArgs args)
        {
            CoverageVerificationControllerArgs coverageVerificationArgs = args as CoverageVerificationControllerArgs;
            if (coverageVerificationArgs.PolicyReference == null && this._verifyCoverage == false)
            {
                coverageVerificationArgs.CoverageVerificationComplete = null;
            }

            base.OnCVPolicySearchFinish(coverageVerificationArgs);
        }

        private void Model_ReloadTotals(object sender, EventArgs e)
        {
            if (this.claimModel.SelectedClaimTransactionFilter == "2" && this.claimModel.SelectedClaimant == null)
            {
                // TODO: Select the first claimaint.
            }

            this.SetTotalFields(); // Re-evaluate the values.
        }

        protected override void OnCoverageVerification(ClaimHeaderDto claimHeaderDto, Action complete)
        {
            this._payloadManager.VirtualProcess = VirtualProcessRequestBuilder.BuildComponentVirtualProcessRequest(this._payloadManager, claimHeaderDto.Data.DataId, "CoverageVerificationPolicyAttachmentValidation");
            this.Model.IsBusy = true;
            this._payloadManager.SynchroniseData(claimHeaderDto,
                this.Model.TransactionId,
                (Response r) =>
            {
                    this.Model.IsBusy = false;
                    if (r.Status == BusinessTransactionStatus.Valid && r.Messages.IsNullOrEmpty())
                {
                        base.OnCoverageVerification(claimHeaderDto, complete);
                }
                });
            }

        protected override void SelectTreeNode(object sender, XIAP.Frontend.Infrastructure.NavTree.TreeNodeEventArgs e)
        {
            this.AXAClearSelectedDto();
            base.SelectTreeNode(sender, e);
            this.SetIOLinkAddMenuOption(e.TreeNodeData.DataObject);
            this.SetLitigationLinkAddMenuOption(e.TreeNodeData.DataObject);
        }

        private void AXAClearSelectedDto()
        {
            if (this.claimModel != null)
            {
                this.claimModel.SelectedDto = null;
            }
        }

        #region Private methods

        private void SetTotalFields()
        {
            List<short> AmountType = new List<short>();
            List<short?> AuthorisationStatus = new List<short?>();

            IEnumerable<ClaimFinancialAmount> transactionFilteredAmounts = this.claimModel.ClaimAmountSearchRows;

            if (transactionFilteredAmounts.Count() > 0)
            {
                this.claimModel.AXAManagedShare = (decimal?)this.Model.HeaderData.CustomProperties["ShareOfWholePercent"] + (decimal?)this.Model.HeaderData.CustomProperties["CoinsuranceOfWholePercent"];
                this.claimModel.AXAShare = (decimal?)this.Model.HeaderData.CustomProperties["ShareOfWholePercent"];
            }

            // If transaction filter is selected as Claimaint then add data for the selected.
            if (this.claimModel.SelectedClaimTransactionFilter == "2" && this.claimModel.SelectedClaimant != null)
            {
                transactionFilteredAmounts = this.claimModel.ClaimAmountSearchRows.Where(ca => ca.ClaimDetailID == this.claimModel.SelectedClaimant.ClaimDetailData.ClaimDetailID);
            }

            // items filtered by the SelectedClaimAmountFilter
            IEnumerable<ClaimFinancialAmount> amountFilteredAmounts = transactionFilteredAmounts;


            // If amounts filter is selected as Gross Amounts then filter the data for the selected.
            if (this.claimModel.SelectedClaimAmountFilter == "2")
            {
                amountFilteredAmounts = transactionFilteredAmounts.Where(ca => ca.MovementDeductibleType != 2 && ca.MovementDeductibleType != 3);
            }
           
            // Total Claim Loss
            this.claimModel.TotalClaimLoss = amountFilteredAmounts.Sum(cfa => this.ShareShownCalculation(cfa.OrderShareCoinsurance, (decimal?)this.Model.HeaderData.CustomProperties["ShareOfWholePercent"], (decimal?)this.Model.HeaderData.CustomProperties["CoinsuranceOfWholePercent"], cfa.TotalClaimLoss)).GetValueOrDefault() * -1;

            // OutstandingEstimate
            this.claimModel.OutstandingEstimate = Math.Abs(amountFilteredAmounts.Sum(cfa => this.ShareShownCalculation(cfa.OrderShareCoinsurance, (decimal?)this.Model.HeaderData.CustomProperties["ShareOfWholePercent"], (decimal?)this.Model.HeaderData.CustomProperties["CoinsuranceOfWholePercent"], cfa.OutstandingEstimates).GetValueOrDefault()));

            // PaymentsInProgress
            this.claimModel.PaymentsInProgress=Math.Abs(amountFilteredAmounts.Sum(cfa => this.ShareShownCalculation(cfa.OrderShareCoinsurance, (decimal?)this.Model.HeaderData.CustomProperties["ShareOfWholePercent"], (decimal?)this.Model.HeaderData.CustomProperties["CoinsuranceOfWholePercent"],cfa.PaymentsInProgress).GetValueOrDefault()));

            // TotalPaymentsPaid
            this.claimModel.TotalPaymentsPaid=Math.Abs(amountFilteredAmounts.Sum(cfa => this.ShareShownCalculation(cfa.OrderShareCoinsurance, (decimal?)this.Model.HeaderData.CustomProperties["ShareOfWholePercent"], (decimal?)this.Model.HeaderData.CustomProperties["CoinsuranceOfWholePercent"],cfa.TotalPaymentsPaid).GetValueOrDefault()));

            // OutstandingRecoveryEstimate
            this.claimModel.OutstandingRecoveryEstimate=Math.Abs(amountFilteredAmounts.Sum(cfa => this.ShareShownCalculation(cfa.OrderShareCoinsurance, (decimal?)this.Model.HeaderData.CustomProperties["ShareOfWholePercent"], (decimal?)this.Model.HeaderData.CustomProperties["CoinsuranceOfWholePercent"],cfa.OutstandingRecoveryEstimates).GetValueOrDefault()));

            // OutstandingULREstimate
            // If transaction filter is selected as Claimaint then add data for the selected.
            this.claimModel.OutstandingULREstimate = Math.Abs(transactionFilteredAmounts.Sum(cfa=>cfa.OutstandingULREstimate).Value);

            // ULRCompleted
            // If transaction filter is selected as Claimaint then add data for the selected.
            this.claimModel.ULRCompleted = Math.Abs(transactionFilteredAmounts.Sum(cfa => cfa.ULRCompleted).Value);
            
            // ULR
            // If transaction filter is selected as Claimaint then add data for the selected.                     
            this.claimModel.ULR = Math.Abs(transactionFilteredAmounts.Sum(cfa => cfa.ULRInProgress).Value);
            
            // RecoveriesCompleted          
            this.claimModel.RecoveriesCompleted = Math.Abs(amountFilteredAmounts.Sum(cfa => this.ShareShownCalculation(cfa.OrderShareCoinsurance, (decimal?)this.Model.HeaderData.CustomProperties["ShareOfWholePercent"], (decimal?)this.Model.HeaderData.CustomProperties["CoinsuranceOfWholePercent"], cfa.RecoveriesCompleted).GetValueOrDefault()));
            
            // RecoveryInProgress           
            this.claimModel.RecoveryInProgress = Math.Abs(amountFilteredAmounts.Sum(cfa => this.ShareShownCalculation(cfa.OrderShareCoinsurance, (decimal?)this.Model.HeaderData.CustomProperties["ShareOfWholePercent"], (decimal?)this.Model.HeaderData.CustomProperties["CoinsuranceOfWholePercent"], cfa.RecoveriesInProgress).GetValueOrDefault()));

            this.claimModel.TransactionAmount = 0.00m;
            this.claimModel.MovementAmount = 0.00m;
        }

        private decimal? ShareShownCalculation(short? orderShareCoinsuranceValue, decimal? shareOfWholePercent, decimal? coinsuranceOfWholePercent, decimal? amount)
            {
            if (this.claimModel.IsAmountShownFilterVisible == Visibility.Visible || (shareOfWholePercent == 0 && coinsuranceOfWholePercent == 0))
            {
                return amount;
            }
            else
            {
                if (this.claimModel.SelectedShareShown == "2" && orderShareCoinsuranceValue.GetValueOrDefault() == 1)
                {
                    amount = (amount * shareOfWholePercent) / (shareOfWholePercent + coinsuranceOfWholePercent);
            }
                else if (this.claimModel.SelectedShareShown == "3" && orderShareCoinsuranceValue.GetValueOrDefault() == 1)
            {
                    amount = (amount * 100) / (shareOfWholePercent + coinsuranceOfWholePercent);
            }
                else if (this.claimModel.SelectedShareShown == "3" && orderShareCoinsuranceValue.GetValueOrDefault() == 2)
            {
                    amount = (amount * 100) / shareOfWholePercent;
                }
            }

            return amount;
        }

        private void SetProductClaimDefinition(ProductClaimDefinitionItem prodDef)
        {
            this.RetrieveHistoricalTransactionData();
            this.claimModel.IsBusy = false;
        }


        private void SetIOLinkAddMenuOption(DtoBase dataobject)
        {
            AXAClaimInsuredObjectLinkModel claimIOLinkModel = this.claimModel.ClaimInsuredObjectLinkModel as AXAClaimInsuredObjectLinkModel;

            if (claimIOLinkModel != null)
            {
                if (dataobject is ClaimNameInvolvementDto)
                {
                    claimIOLinkModel.AllowAdd = true;
                }
                else
                {
                    claimIOLinkModel.AllowAdd = false;
                }

                claimIOLinkModel.VehicleClaimIOLinksMenuItems.ForEach(c => (c.MenuItemCommand as DelegateCommandBase).RaiseCanExecuteChanged());
            }
        }

        private void SetLitigationLinkAddMenuOption(DtoBase dataobject)
        {
            AXAClaimLitigationLinkModel claimLitigationLinkModel = this.claimModel.ClaimLitigationLinkModel as AXAClaimLitigationLinkModel;

            if (claimLitigationLinkModel != null)
            {
                if (dataobject is ClaimNameInvolvementDto)
                {
                    claimLitigationLinkModel.AllowAdd = true;
                }
                else
                {
                    claimLitigationLinkModel.AllowAdd = false;
                }

                claimLitigationLinkModel.ClaimLitigationLinksMenuItems.ForEach(c => (c.MenuItemCommand as DelegateCommandBase).RaiseCanExecuteChanged());
            }
        }

        #endregion

        #region Public methods

        // Get the search criteria for [Claims].[ListClaimSummaryAmounts]
        public void GetSearchParameters()
        {
            if (this.claimModel.SearchParameters == null)
            {
                this.searchService.GetSearchParameters(CLAIMS_AMOUNT_SEARCH_NAME,
                    (SearchParameters r) =>
                    {
                        if (this.claimModel.SearchParameters == null)
                        {
                            this.claimModel.SearchParameters = r;
                        }

                        this.RetrieveHistoricalTransactionData();
                    });
            }
            else
            {
                this.RetrieveHistoricalTransactionData();
            }
        }    

        protected override void SetNameInvolvementData(Action callback = null)
        {
            base.SetNameInvolvementData(this.SetClientDetails);
        }

        private void SetClientDetails()
        {
            string clientName = string.Empty;

            var claimNameInvolvements = this.claimModel.HeaderDto.ClaimInvolvements.Where(c => c.IsFull && c.ClaimInvolvementData != null && c.ClaimInvolvementData.ClaimInvolvementType == (short)StaticValues.LinkableComponentType.NameInvolvement).SelectMany(b => b.ClaimNameInvolvements).ToList();
            if (claimNameInvolvements != null)
            {
                foreach (ClaimNameInvolvementDto cniData in claimNameInvolvements)
                {
                    if (cniData.ClaimNameInvolvementData.NameInvolvementType == (short)StaticValues.NameInvolvementType_ClaimNameInvolvement.MajorInsured &&
                            cniData.ClaimNameInvolvementData.NameInvolvementMaintenanceStatus == (short)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest)
                    {
                        clientName = cniData.ClaimNameInvolvementData.ListName;
                    }
                }
            }

            this.claimModel.ClientName = clientName;
        }


        private void SetNames(ActionKey actionKey)
        {
            ClaimEventData claimEventData = actionKey.NodeInfo.DataObject.Data as ClaimEventData;

            if (claimEventData.CreatedByUserID != null)
            {
                this.claimModel.CreatedByUserName = claimEventData.CreatedByUserName;
            }

            if (claimEventData.TaskInitialUserID != null)
            {
                IMetadataClientService metaDataService = ApplicationModel.Instance.Container.Resolve<IMetadataClientService>();
                metaDataService.QueryNameDetails(claimEventData.TaskInitialUserID.Value, (task) =>
                {
                    this.claimModel.TaskInitialUserName = task.UserDescription;
                }, null);
            }
        }

        protected override void DetermineComponentsToLoad()
        {
            base.DetermineComponentsToLoad();

            AppModel model = this.Container.Resolve<AppModel>();
            Module module = model.ShellConfiguration.Modules.FirstOrDefault(e => e.ModuleName == "Claims");
            string definition = (this.claimModel.ProductCode == AXAClaimConstants.MOTOR_CLAIM_PRODUCT) ? CLAIMANTS : LIABILITY_CLAIMANTS;
            var screens = module.ScreenDefinitions;

            screens.Where(s => s.Definition == definition).ForEach((s) =>
            {
                if (this.claimModel.SelectedClaimDetailFilter == "1")
                {
                    if (s.RootComponent.Contains("ClaimDetailInternalStatus=2"))
                    {
                        s.RootComponent = s.RootComponent.Replace("[ClaimDetailInternalStatus=1 or ClaimDetailInternalStatus=2]", "[ClaimDetailInternalStatus=1]");
                    }
                }
                else
                {
                    if (s.RootComponent.Contains("ClaimDetailInternalStatus=1"))
                    {
                        s.RootComponent = s.RootComponent.Replace("[ClaimDetailInternalStatus=1]", "[ClaimDetailInternalStatus=1 or ClaimDetailInternalStatus=2]");
                    }
                }
            });

            if (!this._componentsToLoad.DataExpressions.Contains("/Claim/ClaimHeader/ClaimDetail/GenericDataSet/GenericDataItems"))
            {
                this._componentsToLoad.DataExpressions.Add("/Claim/ClaimHeader/ClaimDetail/GenericDataSet/GenericDataItems");
            }

            if (!this._componentsToLoad.DataExpressions.Contains("/Claim/ClaimHeader/GenericDataSet/GenericDataItems"))
            {
                this._componentsToLoad.DataExpressions.Add("/Claim/ClaimHeader/GenericDataSet/GenericDataItems");
            }

            if (!this._componentsToLoad.DataExpressions.Contains("/Claim/ClaimHeader/ClaimDetail/ClaimDetailToClaimInvolvementLink"))
            {
                this._componentsToLoad.DataExpressions.Add("/Claim/ClaimHeader/ClaimDetail/ClaimDetailToClaimInvolvementLink");
            }
        }

        private void ValidateDuplicateCommand(bool disableDuplicateCommand = false)
        {
            ////Do not display the duplicate check button if DateofLoss and Client and if current claim header status is 
            ////'Claim Opened – Unconfirmed' CON.

            if (this.claimModel.HeaderData == null)
            {
                return;
            }

            if (this.Model.TransactionType != ClaimTransactionType.CreateClaim
                && (string.IsNullOrWhiteSpace(this.Model.CurrentStatusCode) || this.Model.CurrentStatusCode != AXAClaimConstants.CLAIM_STATUS_CLAIM_OPENED_UNCONFIRMED))
            {
                this.DisableDuplicateCheckCommand();
                return;
            }

            if (this.claimModel.HeaderData.DateOfLossFrom.HasValue == false)
            {
                this.DisableDuplicateCheckCommand();
                return;
            }

            if (this.claimModel.HeaderDto.ClaimInvolvements == null)
            {
                this.DisableDuplicateCheckCommand();
                return;
            }

            if (this.claimModel.HeaderDto.ClaimInvolvements.Where(cni => this.IsMajorInsuredFound(cni.ClaimNameInvolvements) == true).Count() == 0)
            {
                this.DisableDuplicateCheckCommand();
                return;
            }

            if (disableDuplicateCommand)
            {
                this.DisableDuplicateCheckCommand();
                return;
            }

            // Enable the duplicate check command.
            this.EnableDuplicateCheckCommand();
        }

        private void DisableDuplicateCheckCommand()
        {
            this.claimModel.CanExecute = false;
            this.claimModel.RaiseExecuteChange();
        }

        private void EnableDuplicateCheckCommand()
        {
            this.claimModel.CanExecute = true;
            this.claimModel.RaiseExecuteChange();
        }

        private bool IsMajorInsuredFound(ObservableCollection<ClaimNameInvolvementDto> list)
        {
            if (list != null && list.Where(cniInd => cniInd.ClaimNameInvolvementData.NameInvolvementMaintenanceStatus == (short)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest &&
                            cniInd.ClaimNameInvolvementData.NameInvolvementType == (short)StaticValues.NameInvolvementType.MajorInsured).Count() > 0)
            {
                return true;
            }

            return false;
        }

        protected override void StartTransactionComplete(ClaimResponse claimResponse)
        {
            base.StartTransactionComplete(claimResponse);

            this.GetSearchParameters();
            ClaimHeaderData headerData = (ClaimHeaderData)this.Model.HeaderDto.Data;
            this.SetClaimDetailFilterOnClaimDisplayTransaction();
            if (this.Model.HeaderDto.CheckIsEditable())
            {
                this.ValidateDuplicateCommand();
                if (this.Model.WizardMode == WizardModeEnum.Copy)
                {
                    this.SetClientProtected();
                }
            }

            var claimNameInvolvements = claimResponse.ClaimHeaderDto.ClaimInvolvements.Where(ci => ci.ClaimNameInvolvements != null).SelectMany(ci => ci.ClaimNameInvolvements).ToList();

            if (claimNameInvolvements != null)
            {
                foreach (ClaimNameInvolvementDto cni in claimNameInvolvements)
                {
                    IClaimNameInvolvementData cnid = cni.Data as IClaimNameInvolvementData;
                    if (cnid.NameID.HasValue)
                    {
                        NameSearchCache.NameDataCache.Remove(cnid.NameID.Value);
                        Cache.Current.Remove(cnid.NameID.Value);
                    }
                }
            }
        }

        private void SetClaimDetailFilterOnClaimDisplayTransaction()
        {
            ClaimHeaderData headerData = (ClaimHeaderData)this.Model.HeaderDto.Data;
            if (headerData.ClaimHeaderInternalStatus.HasValue && this.Model.TransactionType == ClaimTransactionType.DisplayClaim && headerData.ClaimHeaderInternalStatus.Value == (short)StaticValues.ClaimHeaderInternalStatus.Finalized)
            {
                this.claimModel.SelectedClaimDetailFilter = null;
                this.claimModel.SelectedClaimDetailFilter = "2";
            }
        }

        private void RetrieveHistoricalTransactionData()
        {
            this.Search();
        }


        private void AssignClaimantDetails()
        {
            if (this.claimModel == null)
            {
                return;
            }

            ClaimHeaderDto claimHeaderDto = this.claimModel.HeaderDto;
            claimHeaderDto.ClaimDetails.ForEach(claimDetailDto =>
            {
                string listName = string.Empty;

                if (claimDetailDto.Data.CustomProperties.ContainsKey("ClaimantNameID"))
                {
                    long nameId = (long)claimDetailDto.Data.CustomProperties["ClaimantNameID"];
                    var nameInvolvment = claimHeaderDto.ClaimNameInvolvements.Where(a => a.ClaimNameInvolvment.ClaimNameInvolvementData.NameID == nameId).FirstOrDefault();
                    if (nameInvolvment != null)
                    {
                        listName = nameInvolvment.ClaimNameInvolvment.ClaimNameInvolvementData.ListName;
                    }
                }

                claimDetailDto.Data.CustomProperties["ClaimantListName"] = listName;
            });
        }       

        public override void Dispose()
        {
            this.claimAmountHistoryController.OnPaymentCancellation -= this.ClaimAmountHistoryController_OnPaymentCancellation;
            this.claimModel.ReloadTotals -= new EventHandler(this.Model_ReloadTotals);
            this.claimModel.ReloadClaimants -= new EventHandler(this.ClaimModel_ReloadClaimants);
            this.claimModel.CustomCoverageVerification -= new EventHandler(this.ClaimModel_CustomCoverageVerification);
            this.claimModel.ValidateClaim -= new EventHandler(this.ClaimModel_ValidateClaim);
            this.claimModel.DuplicateClaimCheckClick -= new EventHandler<XIAP.Frontend.Infrastructure.Events.CommandEventArgs<ClaimHeaderDto>>(this.ClaimModel_DuplicateClaimCheckClick);
            this.claimModel.NameInvolvementModel.GetNameForNameInvolvement -= new EventHandler(this.NameInvolvementModel_GetNameForNameInvolvement);
            this.claimModel.TargetHeaderStatusCodeChanged -= new EventHandler(this.ClaimModel_TargetHeaderStatusCodeChanged);
            this.claimModel.ClaimAmountSearchRows = null;
            this.claimModel.SearchParameters = null;
            this.claimModel = null;
            this._gdiModel = null;
            base.Dispose();
        }
        #endregion
    }
}

