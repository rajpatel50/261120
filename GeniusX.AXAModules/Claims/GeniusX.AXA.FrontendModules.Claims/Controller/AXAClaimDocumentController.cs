using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using GeniusX.AXA.FrontendModules.Claims.Model;
using Microsoft.Practices.Unity;
using Xiap.ClientServices.Facade.Common;
using Xiap.Framework.Data;
using Xiap.Framework.Entity;
using Xiap.Metadata.Data.Enums;
using XIAP.Frontend.CommonInfrastructure.Controller.Documents;
using XIAP.Frontend.Infrastructure;
using XIAP.Frontend.Infrastructure.DataMapping;
using XIAP.FrontendModules.Claims.Controller;
using XIAP.FrontendModules.Claims.Model;
using XIAP.FrontendModules.Claims.Service;
using XIAP.FrontendModules.Common;
using XIAP.FrontendModules.Common.ClaimService;
using XIAP.FrontendModules.Common.Controller;
using XIAP.FrontendModules.Common.Documents;
using XIAP.FrontendModules.Claims.Data;

namespace GeniusX.AXA.FrontendModules.Claims.Controller
{
    public class AXAClaimDocumentController : ClaimDocumentController<ClaimModel>
    {
        private PayloadManager payloadManager;
        private bool isPrintAndFinalize;
        private AXADocumentModel axadocumentmodel;
        public AXAClaimDocumentController(AXAClaimController claimController, AXADocumentModel model, ClaimsRetrievalManager claimsRetrievalManager, IClaimClientService claimClientService, IMetadataClientService metadataService, AppModel appModel, IUnityContainer container, TransactionModelBase documentContainerModel)
            : base(claimController, model, claimsRetrievalManager, claimClientService, metadataService, appModel, container, documentContainerModel)
        {
            this.axadocumentmodel = model as AXADocumentModel;
            this.axadocumentmodel.OnFinaliseDocumentClick += new EventHandler<CommandDataEventArgs<IDocumentDetails>>(this.DocumentModel_OnFinaliseDocumentClick);
            this.axadocumentmodel.OnPrintAndFinaliseDocumentClick += new EventHandler<CommandDataEventArgs<IDocumentDetails>>(this.DocumentModel_OnPrintAndFinaliseDocumentClick);
            this.payloadManager = claimController.PayLoadManager;
            this.OnFinishCallback = this.OnCompleteDocumentTransaction;
        }

        private void DocumentModel_OnPrintAndFinaliseDocumentClick(object sender, CommandDataEventArgs<IDocumentDetails> e)
        {
            this.axadocumentmodel.IsPrintOrFinaliseClicked = true;
            this.isPrintAndFinalize = true;
            this.RecreateDocument(e.Data);
        }

        private void DocumentModel_OnFinaliseDocumentClick(object sender, CommandDataEventArgs<IDocumentDetails> e)
        {
            this.Model.HasCascadingCalls = true;
            this.Model.IsBusy = true;
            this.axadocumentmodel.IsPrintOrFinaliseClicked = true;
            this.isPrintAndFinalize = false;
            this.RecreateDocument(e.Data);
        }

        protected override void RetrieveCompleted(IDocumentDetails documentDto, DtoBase docdata)
        {
            if (this.axadocumentmodel.IsPrintOrFinaliseClicked && documentDto.IsDocumentResubmitted == true)
            {
                this.Model.IsBusy = true;
                this.axadocumentmodel.IsPrintOrFinaliseClicked = false;
                this.RetrieveData(docdata);
            }
        }

       
        public void RetrieveData(DtoBase docdata)
        {
            this.axadocumentmodel.RetrievalManager.RetrieveData(docdata,
                this.Model.TransactionId,
                RetrievalType.WithChild,
                BusinessDataVariant.Full,
                null,
                response =>
                {
                    var parent = this.Model.XIAPDocumentContainer;
                    this.payloadManager.SynchroniseRetrievedData(docdata, response.Dto.Single());
                    docdata.Data.ParentDto = parent as DtoBase;
                    this.SaveClaim(docdata);
                });
        }

        private void SaveClaim(DtoBase docData)
        {
            var documentTextSegment = (docData.Data as IDocumentDetails).DocumentTextSegments.Where(a => a.TextSegmentType == (short)StaticValues.TextSegmentType.GenerationData).FirstOrDefault();
            if (documentTextSegment != null)
            {
                if (this.isPrintAndFinalize)
                {
                    documentTextSegment.CustomBoolean04 = true;
                }
                else
                {
                    documentTextSegment.CustomBoolean05 = true;
                }

                var request = this.payloadManager.GetRequest(docData, this.Model.TransactionId);
                request.VirtualProcessRequest = VirtualProcessRequestBuilder.BuildComponentVirtualProcessRequest(this.payloadManager, docData.Data.DataId, "DocumentDetail");
                this.payloadManager.Validate(request, (response) => { OnSaveCompleted(response, docData); }, this.CustomHandleError);
            }
        }

        private void OnSaveCompleted(Response response, DtoBase docData)
        {
            this.payloadManager.VirtualProcess = null;
            IDocumentDetails document = this.RetrieveDocument(docData.Data.DataId);
            if (document != null)
            {
                if (!response.Messages.IsNullOrEmpty())
                {
                    if (this.WarningExists(response, document))
                    {
                        document.CurrentStatus = (short)StaticValues.DocumentStatus.DataEntryPending;
                    }
                    else
                    {
                        this.Save();
                    }
                }
                else
                {
                    this.Save();
                }
            }

            this.Model.IsBusy = false;
            this.Model.HasCascadingCalls = false;
        }

        private bool CustomHandleError(AsyncCompletedEventArgs args)
        {
            this.Model.IsBusy = false;
            this.Model.HasCascadingCalls = false;
            this.payloadManager.VirtualProcess = null;
            return this.HandleError(args);
        }

        private IDocumentDetails RetrieveDocument(Guid docId)
        {
            IDocumentDetails document = null;
            document = this.Model.XIAPDocuments.Where(a => a.DataId == docId).FirstOrDefault();
            return document;
        }

        private bool WarningExists(Response response, IDocumentDetails document)
        {
            bool _warningExists = false;
            var warnings = this.ExtractTextSegmentValidationMessages(response.Messages, document);
            if (!warnings.IsNullOrEmpty())
            {
                _warningExists = true;
            }

            return _warningExists;
        }

        private void Save()
        {
            if (this.axadocumentmodel.OnSaveCommand != null)
            {
                this.axadocumentmodel.OnSave();
            }
        }

        private ObservableCollection<MessageData> ExtractTextSegmentValidationMessages(IEnumerable<MessageData> messages, IDocumentDetails details)
        {
            var validationIds = new List<Guid> { details.DataId };
            var documentSegmentIds = details.DocumentTextSegments.Select(a => a.DataId);
            validationIds.AddRange(documentSegmentIds);
            validationIds.AddRange(details.DocumentAttachments.Select(a => a.DataId));
            validationIds.AddRange(details.DocumentRecipients.Select(a => a.DataId));

            var validationMessages = (from msg in messages
                                      join ids in validationIds on msg.ComponentDataId equals ids
                                      select msg)
                   .ToObservableCollection();

            foreach (var message in validationMessages)
            {
                if (message.Severity == Xiap.Framework.ProcessHandling.ErrorSeverity.None
                    && documentSegmentIds.Contains(message.ComponentDataId))
                {
                    message.Severity = Xiap.Framework.ProcessHandling.ErrorSeverity.Warning;
                }
            }

            return validationMessages;
        }

        private void OnCompleteDocumentTransaction(ControllerArgs args)
        {
            if (args != null)
            {
                var warningsExist = args.GetProperty<bool>("WarningsExist");
                if (warningsExist == false && (!(args is DocumentUploadControllerArgs) || !((DocumentUploadControllerArgs)args).Cancelled))
                {
                    this.Save();
                }

                this.Model.IsBusy = false;
                this.Model.HasCascadingCalls = false;
            }
        }

        protected override void OnUploadDocument(ControllerArgs args)
        {
            if (this.OnFinishCallback != null)
            {
                args.SetProperty("WarningsExist", false);
                this.OnFinishCallback(args);
            }

            base.OnUploadDocument(args);
        }
    }
}
