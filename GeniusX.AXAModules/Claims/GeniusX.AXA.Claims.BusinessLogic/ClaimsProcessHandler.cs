using System;
using System.Collections.Generic;
using System.Linq;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.Common;
using Xiap.Framework.Logging;
using Xiap.Framework.Messages;
using Xiap.Framework.Metadata;
using Xiap.Framework.ProcessHandling;
using Xiap.Claims.BusinessTransaction;
using Xiap.Framework.Security;
using Xiap.Claims.BusinessLogic;
using Xiap.Framework.Common.Product;
using Xiap.Framework.Validation;
using System.Configuration;
using Xiap.Framework.DataMapping;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    /// <summary>
    /// This plug-in create new K2 process/task. It is used by all the events which requires Task to be created.
    /// </summary>
	public class ClaimsProcessHandler : AbstractComponentPlugin
	{
		private static readonly ILogger logger = LoggerFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private const string AXA_CLOSE_FINALIZE_CLAIM_PROCESS_HANDLER = "AXACloseFinalizeClaimProcessHandler";
        private const string AXA_REVIEW_COMPLETED_PROCESS_HANDLER = "AXAReviewCompletedProcessHandler";
        private const string AXA_PHONE_LOG_PROCESS_HANDLER = "AXAPhoneLogProcessHandler";
        // ClaimWakeUp Processing (1)
        private const string AXA_MANUAL_REVIEW_START_CLAIM_PROCESS_HANDLER = "AXAManualReviewStartClaimProcessHandler";
        // ClaimWakeUp Processing (1) Ends
        private const string AXA_PAYMENT_CANCELLATION_PROCESS_HANDLER = "AXAPaymentCancellationProcessHandler";
        public const string TECHNICALREFERRALPROCESSALIAS = "AXAReferralRequestedProcessHandler";
        public const string REFERRALREPLYDESTINATIONUSER = "ReferralReplyDestinationUser";
        private const string CLAIM_TRANSACTION_HEADER_ID = "ClaimTransactionHeaderID";
        private const string AXAFileUploadNotificationProcessHandler = "AXAFileUploadNotificationProcessHandler";
        private const string ClaimDocumentGroupReference = "DocumentGroupReference";
        private const string ClaimDocumentReference = "DocumentReference";
        private const string FileUploadNotificationTask = "File Upload Notification";
        private const string CompletedProductEventID = "CompletedProductEventID";
        private const string FileUploadNotificationCompletedEventType = "FileUploadNotificationCompletedEventType";
        private const string ClaimEventDocumentReferenceCustomReferenceField = "ClaimEvent_DocumentReference_CustomReferenceField";
        private const string ClaimEventDocumentGroupReferenceCustomReferenceField = "ClaimEvent_DocumentGroupReference_CustomReferenceField";
        private const string ClaimEventGenerateTaskCustomCodeField = "ClaimEvent_GenerateTask_CustomCodeField";
        private static PrimitivePropertyAccessor propAccessor = new PrimitivePropertyAccessor(typeof(ClaimEvent));

        /// <summary>
        /// start the process on Virtual invocation point.
        /// </summary>
        /// <param name="component">ClaimEvent component or ClaimHeader component</param>
        /// <param name="point">invocation point</param>
        /// <param name="pluginId">plugin id</param>
        /// <param name="processParameters">process parameters</param>
        /// <returns>collection of process results</returns>
		public override ProcessResultsCollection ProcessComponent(IBusinessComponent component, ProcessInvocationPoint point, int pluginId, ProcessParameters processParameters)
		{
			ClaimEvent claimEvent = null;
			ClaimHeader header = null;
			PluginHelper<IBusinessComponent> pluginHelper = null;

            // Get the ClaimHeader, depending on whether we have a ClaimEvent or a ClaimHeader Component
            // The pluginHelper will reflect the Business Component passed in.
			if (component.GetType() == typeof(ClaimEvent))
			{
				claimEvent = (ClaimEvent)component;
                // Get the Claim Header from the ClaimEvent, depending on where it's attached.
				if (claimEvent.Parent is ClaimDetail)
				{
					header = (ClaimHeader)claimEvent.Parent.Parent;
				}
				else
				{
					header = (ClaimHeader)claimEvent.Parent;
				}

				pluginHelper = new PluginHelper<IBusinessComponent>(point, claimEvent, new ProcessResultsCollection());
			}
			else
			{
				header = (ClaimHeader)component;
				pluginHelper = new PluginHelper<IBusinessComponent>(point, header, new ProcessResultsCollection());
			}

			try
			{
                // We are only processing on the Virtual invocation point.
				if (point == ProcessInvocationPoint.Virtual)
				{
					switch (processParameters.Alias)
					{
                        case AXA_CLOSE_FINALIZE_CLAIM_PROCESS_HANDLER:
                            // Close all tasks on Finalise.
							ClaimsProcessHelper.CloseAllTasks(header);
							break;
                        case AXA_REVIEW_COMPLETED_PROCESS_HANDLER:
							if (processParameters.TransactionInvocationPoint == TransactionInvocationPoint.PreComplete)
							{
                                // On pre-complete of a transaction only, process for the auto-review event.
								this.CheckAndCreateAutoReviewEvent(header, pluginHelper, processParameters.Alias);
							}

							break;
                        case AXA_PHONE_LOG_PROCESS_HANDLER:
                            // If the claim event "Task To Do" is set to '1' we have a PhoneLog and should process.
                            if (!string.IsNullOrWhiteSpace(claimEvent.CustomCode03) && claimEvent.CustomCode03 == "1")   
                            {
                                this.StartClaimProcess(header, pluginHelper, processParameters.Alias);
                            }

                            break;
                        // ClaimWakeUp Processing (2)
                        case AXA_MANUAL_REVIEW_START_CLAIM_PROCESS_HANDLER:
                            // On opening a review task on a closed migrated claim, we must be doing this to reopen that claim
                            // so we process the reopening using the ClaimReviewTaskProcessHandler, that calls out to the ClaimWakeUpService.
                            if (ClaimsBusinessLogicHelper.CheckMigratedCloseClaim(header))
                            {
                                ProcessHelper.HandleVirtualProcess(header, new ProcessParameters { Alias = "AXAClaimsReviewTaskProcessHandler" });
                            }
                            // We also need to run the default processing but leaving out the 'break;' comment to drop through
                            // is confusing for reading the code.
                            this.StartClaimProcess(header, pluginHelper, processParameters.Alias);
                            break;
                        case AXAFileUploadNotificationProcessHandler:
                            // CustomCode08 is based on Document.CustomCode08 "Generate task?" field
                            string generateTask = Convert.ToString(propAccessor.GetProperty(claimEvent, ConfigurationManager.AppSettings[ClaimEventGenerateTaskCustomCodeField]));
                            if (generateTask == "1")
                            {
                                this.StartClaimProcess(header, pluginHelper, processParameters.Alias);
                            }

                            break;
                        default:
							this.StartClaimProcess(header, pluginHelper, processParameters.Alias);
							break;
					}
				}
			}
			catch (Exception e)
			{
				logger.Error(e);
				logger.Error(e.InnerException);
				pluginHelper.AddError(MessageConstants.CLAIM_PROCESS_ERROR);
			}

			return pluginHelper.ProcessResults;
		}

        /// <summary>
        /// Creating a auto Review event
        /// </summary>
        /// <param name="header">claim header component</param>
        /// <param name="pluginHelper">Claim event plugin helper</param>
        /// <param name="p">string value</param>
		private void CheckAndCreateAutoReviewEvent(ClaimHeader header, PluginHelper<IBusinessComponent> pluginHelper, string p)
		{
			ClaimEvent claimEvent = (ClaimEvent)pluginHelper.Component;
            // Get the Product Event References for Auto-Review and Manual Review from the application configuration
			string autoReviewCompletePrdEvtRef = ClaimsBusinessLogicHelper.ResolveMandatoryConfig<string>("AutoReviewCompleteProductEventRef");
			string manualReviewCompletePrdEvtRef = ClaimsBusinessLogicHelper.ResolveMandatoryConfig<string>("ManualReviewCompleteProductEventRef");

			// Only process if this is an Auto-Review or Manual Review event.
			if (claimEvent.ProductEventReference != autoReviewCompletePrdEvtRef && claimEvent.ProductEventReference != manualReviewCompletePrdEvtRef)
			{
				return;
			}

			string folio = ClaimsProcessHelper.GenerateFolio(header, ClaimsBusinessLogicHelper.GetInsuredFromHeader(header));
            // Return a list of Review Process names.
			List<string> processNames = ClaimsProcessHelper.GetReviewProcessNames();

			bool createAutoReviewEvent = true;
            // Count the number of review tasks (determined from the processNames list) that are open for the given folio.
			Xiap.Framework.Data.Tasks.ITaskService internalTaskService = ObjectFactory.Resolve<Xiap.Framework.Data.Tasks.ITaskService>(XiapConstants.XIAP_DATASOURCE);
			Dictionary<string, int> processDetails = internalTaskService.GetTaskCountByProcessName(folio, processNames);

			if (processDetails.FirstOrDefault(a => a.Value > 0).Key != string.Empty)
			{
				if (logger.IsDebugEnabled)
				{
					processDetails.ToList<KeyValuePair<string, int>>().ForEach(a => logger.Debug("ProcessDetails: Name : " + a.Key + ", Count : " + a.Value.ToString()));
				}
                // If there are any review tasks in progress for the folio then don't create an auto-review event.
				createAutoReviewEvent = false;
			}

			if (logger.IsDebugEnabled)
			{
				logger.Debug("CreateAutoReviewEvent : " + createAutoReviewEvent);
			}

			if (createAutoReviewEvent)
			{
				string autoReviewProductEventRef = ClaimsBusinessLogicHelper.ResolveMandatoryConfig<string>("AutoReviewProductEventRef");

				if (logger.IsDebugEnabled)
				{
					logger.Debug("AutoReviewProductEventRef : " + autoReviewProductEventRef);
					logger.Debug("Creating Event with ProductRef : " + autoReviewProductEventRef);
				}

                // Create a new auto-review task against this claim
				ClaimEvent newAutoReviewClaimEvent = header.AddNewClaimEvent(autoReviewProductEventRef, header.ProductVersionID.Value);
				newAutoReviewClaimEvent.TaskInitialDueDate = claimEvent.TaskInitialDueDate;
				claimEvent.TaskInitialDueDate = DateTime.Now;

				if (logger.IsDebugEnabled)
				{
					logger.Debug("Created Event with ProductRef : " + autoReviewProductEventRef);
				}
			}
		}

        /// <summary>
        /// Starting the process of Claim
        /// </summary>
        /// <param name="header">claim header</param>
        /// <param name="pluginHelper">plugin helper</param>
        /// <param name="processAlias">process alias to start the process</param>
        private void StartClaimProcess(ClaimHeader header, PluginHelper<IBusinessComponent> pluginHelper, string processAlias)
        {
            if (pluginHelper.ProcessResults.Count == 0)
            {
                ClaimEvent claimEvent = (ClaimEvent)pluginHelper.Component;
                string processName = ClaimsProcessHelper.GetClaimProcessName(processAlias);
                string documentReference = Convert.ToString(propAccessor.GetProperty(claimEvent, ConfigurationManager.AppSettings[ClaimEventDocumentReferenceCustomReferenceField]));
                string documentGroupReference = Convert.ToString(propAccessor.GetProperty(claimEvent, ConfigurationManager.AppSettings[ClaimEventDocumentGroupReferenceCustomReferenceField]));
                          
                ClaimNameInvolvement cni;

                // Get the Major Insured name involvement differently depending on whether or not we are creating an event.
                if (claimEvent.Context.TransactionType == ClaimsProcessConstants.CREATEEVENT)
                {
                    cni = ClaimsBusinessLogicHelper.GetInsuredfromClaim(header.ClaimReference);
                }
                else
                {
                    cni = ClaimsBusinessLogicHelper.GetInsuredFromHeader(header);
                }

                string folio = string.Empty;
                if (processAlias == AXAFileUploadNotificationProcessHandler)
                {
                    // CustomReference02=> DocumentGroupReference / CustomReference01=> DocumentReference
                    folio = !string.IsNullOrWhiteSpace(documentGroupReference) ? header.ClaimReference + "/" + documentGroupReference : header.ClaimReference + "/" + documentReference;
                }
                else
                {
                    folio = ClaimsProcessHelper.GenerateFolio(header, cni);
                }

                // Build a dictionary of data for use in generating a task
                Dictionary<string, object> data = ClaimsProcessHelper.GenerateProcessData(header, pluginHelper);
                
                if (logger.IsInfoEnabled)
                {
                    string dataString = ClaimsProcessHelper.GetProcessDataString(data);
                    logger.Info("Folio:" + folio + ", Process Name:" + processName + ",Data:" + dataString);
                }

                if (logger.IsDebugEnabled)
                {
                    logger.Debug("ClaimEvent ProductEventReferece : " + claimEvent.ProductEventReference + ", CreateByUserId : " + claimEvent.CreatedByUserID);
                }

                switch (processAlias)
                {
                    // Check if we are doing a Payment Cancellation
                    case AXA_PAYMENT_CANCELLATION_PROCESS_HANDLER:
                        {
                            // Assuming that the last saved ClaimTransactionHeader would be the one which got cancelled. Because there cannot be multiple In progress transactions
                            ClaimTransactionHeader claimTransHeader = header.HistoricalClaimTransactionHeaders.OrderByDescending(o => o.ClaimTransactionHeaderID).FirstOrDefault();

                            if (claimTransHeader != null)
                            {
                                // Include the header ID in the task data and run the instantiate method.
                                data.Add(CLAIM_TRANSACTION_HEADER_ID, claimTransHeader.ClaimTransactionHeaderID);
                                this.InstantiateProcess(header, claimEvent, processName, folio, data);
                            }
                            else
                            {
                                pluginHelper.AddError(MessageConstants.CancelPayment_ClaimTransactionHeader_Invalid);
                            }
                        }

                        break;
                    case AXAFileUploadNotificationProcessHandler:
                        {
                            data.Add(ClaimDocumentReference, documentReference);
                            data.Add(ClaimDocumentGroupReference, documentGroupReference);
                            if (!ConfigurationManager.AppSettings.Keys.OfType<string>().Contains(FileUploadNotificationCompletedEventType))
                            {
                                throw new InvalidOperationException("File Upload Notification Completed Event Type is not Configured in App settings.");
                            }

                            string fileUploadNotificationCompletedEventType = ConfigurationManager.AppSettings[FileUploadNotificationCompletedEventType];
                            if (!string.IsNullOrWhiteSpace(fileUploadNotificationCompletedEventType))
                            {
                                var productEvent = ObjectFactory.Resolve<IProductEventQuery>().GetProductEvents(header.ProductVersionID.GetValueOrDefault()).SingleOrDefault(pe => pe.EventTypeCode == fileUploadNotificationCompletedEventType);
                                if (productEvent != null && productEvent.ProductEventID > 0)
                                {
                                    data.Add(CompletedProductEventID, productEvent.ProductEventID);
                                }
                                else
                                {
                                    throw new InvalidOperationException("File Upload Notification Completed Event Type is not linked on Product.");
                                }
                            }
                            else
                            {
                                throw new InvalidOperationException("File Upload Notification Completed Event Type is not configured.");
                            }

                            data.Add("TaskUser", ClaimsTaskAssignmentDecisionTable.GetTaskAssignmentUser(FileUploadNotificationTask, header).UserIdentity);
                            this.InstantiateProcess(header, claimEvent, processName, folio, data, documentReference);
                        }

                        break;
                    default:
                        {
                            // If we have a Created By User on the event that we are working with, process accordingly
                            if (claimEvent.CreatedByUserID.HasValue)
                            {
                                User user;
                                if (ClaimsBusinessLogicHelper.TryGetUserIdentityByUser(claimEvent.CreatedByUserID.Value, out user))
                                {
                                    // If we can find that user in GeniusX and we're doing a technical referral
                                    // then add them to the task data as the Referral Destination.
                                    if (processAlias == TECHNICALREFERRALPROCESSALIAS)
                                    {
                                        data.Add(REFERRALREPLYDESTINATIONUSER, user.UserIdentity);
                                    }

                                    this.InstantiateProcess(header, claimEvent, processName, folio, data);
                                }
                                else
                                {
                                    pluginHelper.AddError(MessageConstants.USER_NOT_DEFINED, claimEvent.CreatedByUserID.Value);
                                }
                            }
                            else
                            {
                                this.InstantiateProcess(header, claimEvent, processName, folio, data);
                            }

                            break;
                        }
                }
            }
            else
            {
                logger.Error("Unable to start process as there were errors in previous processing.");
            }
        }

        /// <summary>
        /// Starts a task using the core Process Instantiation after first setting up the appropriate ClaimHeaderLink data.
        /// </summary>
        /// <param name="header">claim header</param>
        /// <param name="claimEvent">claim event component</param>
        /// <param name="processName">string value</param>
        /// <param name="folio">string value</param>
        /// <param name="data">dictionary collection</param>
        /// <param name="documentReference">string value of Document Reference</param>
        private void InstantiateProcess(ClaimHeader header, ClaimEvent claimEvent, string processName, string folio, Dictionary<string, object> data, string documentReference = null)
        {
            List<LinkedComponentItem> claimHeaderLink = null;

            if (claimEvent == null || claimEvent.ClaimDetail == null)
            {
                claimHeaderLink = new List<LinkedComponentItem> { new LinkedComponentItem { SystemComponentId = SystemComponentConstants.ClaimHeader, ComponentId = header.ClaimHeaderID } };
            }
            else
            {
                claimHeaderLink = new List<LinkedComponentItem> { new LinkedComponentItem { SystemComponentId = SystemComponentConstants.ClaimHeader, ComponentId = header.ClaimHeaderID }, new LinkedComponentItem { SystemComponentId = SystemComponentConstants.ClaimDetail, ComponentId = claimEvent.ClaimDetail.ClaimDetailID } };
            }

            if (documentReference != null)
            {
                var document = header.ClaimDocuments.SingleOrDefault(d => d.DocumentReference == documentReference);
                if (document != null)
                {
                    claimHeaderLink.Add(new LinkedComponentItem() { SystemComponentId = SystemComponentConstants.Document, ComponentId = document.DocumentID });
                }
            }

            ProcessHandlerHelper.InstantiateProcess(processName, folio, data, claimHeaderLink.ToArray());
        }
	}
}
