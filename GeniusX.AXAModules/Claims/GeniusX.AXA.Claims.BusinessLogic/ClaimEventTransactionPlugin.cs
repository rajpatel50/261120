using System;
using System.Collections.Generic;
using System.Linq;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.BusinessTransaction;
using Xiap.Framework.Common;
using Xiap.Framework.Logging;
using Xiap.Framework.ProcessHandling;
using Xiap.Metadata.BusinessComponent;
using Xiap.Metadata.Data.Enums;
using Xiap.Framework.Common.Product;
using ProductXML = Xiap.Metadata.Data.XML.ProductVersion;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    public class ClaimEventTransactionPlugin : ITransactionPlugin
    {
        private static readonly ILogger _Logger = LoggerFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Call on ClaimEventTransaction
        /// </summary>
        /// <param name="businessTransaction">Event Container</param>
        /// <param name="point">Point such as PostComplete,PreComplete </param>
        /// <param name="PluginId">PlugIN ID</param>
        /// <param name="parameters">Processing Trasnaction Parameters</param>
        /// <returns>return ProcessResultCollection which have Error msg If any validation failed.</returns>
        public ProcessResultsCollection ProcessTransaction(IBusinessTransaction businessTransaction, TransactionInvocationPoint point, int PluginId, params object[] parameters)
        {
            try
            {
                // If Transaction Invocation Point is 'PostComplete' or 'PreComplete', then check if the ClaimHeaderStatusCode has changed and trigger the 
                // corresponding virtual 'Process-handler' plug-ins
                if (point == TransactionInvocationPoint.PostComplete || point == TransactionInvocationPoint.PreComplete)
                {
                    IEventContainer eventContainer;
                    // Get the event container, either from the component or its parent.
                    if (businessTransaction.Component is IEventContainer)
                    {
                        eventContainer = (IEventContainer)businessTransaction.Component;
                    }
                    else
                    {
                        eventContainer = (IEventContainer)businessTransaction.Component.Parent;
                    }

                    if (point == TransactionInvocationPoint.PreComplete)
                    {
                        // Create event(s) based on the sources (Payment, Reserve, Recoveries) on the Claim Transaction Header
                        this.CreateEventOnTransaction(eventContainer);
                    }

                    // Check Newly added event and invoke the process handler if attached with the event.
                    foreach (IEvent ev in eventContainer.Events)
                    {
                        if (ev.IsNew)
                        {
                            this.InvokeVirtualProcess(ev, eventContainer, point);
                        }
                    }

                    // Check Deleted event and invoke the process handler if attached with the event.
                    foreach (IEvent ev in businessTransaction.Context.DeletedEvents)
                    {
                        this.InvokeVirtualProcess(ev, eventContainer, point);
                    }
                }
            }
            catch (Exception e)
            {
                _Logger.Error(e);
                throw;
            }

            return businessTransaction.Results;
        }

        /// <summary>
        /// Invoke the AXADefaultClaimEventValues virtual process against this event, via a plugin call.
        /// </summary>
        /// <param name="ev">  Event type object </param>
        /// <param name="eventContainer">Event Container</param>
        /// <param name="point">Points PostComplete,Precomplete</param>
        private void InvokeVirtualProcess(IEvent ev, IEventContainer eventContainer, Xiap.Framework.TransactionInvocationPoint point)
        {
            ProductXML.ProductEvent pdevent = ev.GetProduct();
            if (pdevent != null)
            {
                IBusinessComponent eventComponent = (IBusinessComponent)ev;
                string alias = "AXADefaultClaimEventValues";
                // invoke the 'process handler' plugin
                if (_Logger.IsInfoEnabled)
                {
                    _Logger.Info("Invoked Virtual plugin " + alias + point.ToString() + "for " + eventComponent.GetType().Name);
                }

                ProcessParameters processParameters = new ProcessParameters() { Alias = alias, TransactionInvocationPoint = point };
                ProcessHelper.HandleVirtualProcess(eventComponent, processParameters);
            }
        }

        /// <summary>
        /// Create event on transaction
        /// </summary>
        /// <param name="eventContainer">Event Container</param>
        private void CreateEventOnTransaction(IEventContainer eventContainer)
        {
            ClaimHeader clmHeader = null;
            // Get claim header depending on the EventContainer (area event was raised in)
            if (eventContainer is ClaimDetail)
            {
                clmHeader = ((ClaimDetail)eventContainer).Parent as ClaimHeader;
            }
            else
            {
                clmHeader = eventContainer as ClaimHeader;
            }

            if (clmHeader == null)
            {
                return;
            }

            ProductVersion product = clmHeader.GetProduct();

            // Check for Transaction headers in progress,
            // cycle through each of them. 
            // In each Claim Transaction header, cycle through all the Claim Transaction Groups
            if (clmHeader.InProgressClaimTransactionHeaders != null)
            {
                foreach (ClaimTransactionHeader clmTransHeader in clmHeader.InProgressClaimTransactionHeaders)
                {
                    foreach (ClaimTransactionGroup clmTransGroup in clmTransHeader.ClaimTransactionGroups)
                    {
                        // Create the event on the Claim Detail, based on whether the transaction is a Payment, Reserve or Recovery
                        switch (clmTransHeader.ClaimTransactionSource)
                        {
                            case (short)StaticValues.ClaimTransactionSource.Reserve:
                                this.CreateEvent(clmHeader,clmTransGroup.ClaimDetail, clmTransHeader, product, ClaimConstants.EVENT_TYPECODE_RESERVE);
                                break;

                            case (short)StaticValues.ClaimTransactionSource.Payment:
                                this.CreateEvent(clmHeader,clmTransGroup.ClaimDetail, clmTransHeader, product, ClaimConstants.EVENT_TYPECODE_PAYMENT);
                                break;

                            case (short)StaticValues.ClaimTransactionSource.RecoveryReserve:
                                this.CreateEvent(clmHeader,clmTransGroup.ClaimDetail, clmTransHeader, product, ClaimConstants.EVENT_TYPECODE_RECOVERYRESERVE);
                                break;

                            case (short)StaticValues.ClaimTransactionSource.RecoveryReceipt:
                                this.CreateEvent(clmHeader,clmTransGroup.ClaimDetail, clmTransHeader, product, ClaimConstants.EVENT_TYPECODE_RECOVERYRECEIPT);
                                break;
                        }
                    }
                }
            }
        } 
        /////// <summary>
        /////// Create Event on claimDetail
        /////// </summary>
        /////// <param name="clmDetail">Claim Detail</param>
        /////// <param name="clmTransHeader">Claim Transaction Header</param>
        /////// <param name="product">Product Version</param>
        /////// <param name="eventTypeCode">event Type Code</param>
        
        private void CreateEvent(ClaimHeader clmHeader,ClaimDetail clmDetail, ClaimTransactionHeader clmTransHeader, ProductVersion product, string eventTypeCode)
        {
            // find an event on the Product for this claim that matches the required event type
            bool EventExists= false;
            var productEvent = ProductService.GetProductEventQuery().GetProductEvents(clmTransHeader.ClaimHeader.ProductVersionID.GetValueOrDefault())
                       .Where(x => x.EventTypeCode == eventTypeCode).FirstOrDefault();
            if (eventTypeCode == ClaimConstants.EVENT_TYPECODE_RECOVERYRECEIPT)
            {
                EventExists = this.ChkIfEventExists(clmHeader, productEvent);
            }

            if (productEvent != null && !EventExists)
            {
                ClaimEvent claimEvent = null;
                // Get a list of all claim details on all Claim Transaction Groups on this ClaimTransactionHeader
                IEnumerable<ClaimDetail> claimDetails = clmTransHeader.ClaimTransactionGroups.Select(a => a.ClaimDetail).Distinct();
                if (claimDetails.Count() == 1)
                {
                    // We only have one claim detail so add the event to that claim detail
                        ClaimDetail claimDetail = claimDetails.First();
                        claimEvent = claimDetail.AddNewClaimEvent(productEvent.ProductEventID, true);
                }
                else
                {
                    // We have multiple claim details, so add the event at the Claim Transaction Header level
                    claimEvent = clmTransHeader.ClaimHeader.AddNewClaimEvent(productEvent.ProductEventID, true);
                }

                if (claimEvent.CustomText01Field.IsInUse == true)
                {
                    // Set the cancellation reason, CustomText01
                    claimEvent.CustomText01 = clmTransHeader.ClaimTransactionDescription;
                }
            }
        }

        private bool ChkIfEventExists(ClaimHeader clmHeader,ProductXML.ProductEvent productEvent)
        {
            bool EventExists = false;
            var result = from cEvent in clmHeader.Events
                         where cEvent.IsNew == true && productEvent.ProductEventID == cEvent.ProductEventID 
                         select cEvent;
            if (result.Count() > 0)
            {
                EventExists = true;            
            }

            return EventExists;             
        }
    }
}
