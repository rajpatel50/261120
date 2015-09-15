using System;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.ProcessHandling;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    public class ClaimsEventPlugin : AbstractComponentPlugin
    {
        /// <summary>
        /// Set the Task Initial User and Due Date on the event as appliccable.
        /// </summary>
        /// <param name="component">Claim Event</param>
        /// <param name="point">Process Invocation Point</param>
        /// <param name="pluginId">unique plugin id</param>
        /// <param name="processParameters">Process parameters</param>
        /// <returns>collection of process results</returns>
        public override ProcessResultsCollection ProcessComponent(IBusinessComponent component, ProcessInvocationPoint point, int pluginId, ProcessParameters processParameters)
        {
            PluginHelper<IBusinessComponent> pluginHelper = new PluginHelper<IBusinessComponent>(point, (ClaimEvent)component, new ProcessResultsCollection());

            switch (pluginHelper.InvocationPoint)
            {
                case ProcessInvocationPoint.Created:
                case ProcessInvocationPoint.Virtual:
                    {
                        this.UpdateTaskInitialUser(pluginHelper);
                        break;
                    }

                case ProcessInvocationPoint.PreValidationDefaulting:
                    {
                        // if a new event is added.
                        ClaimEvent clmEvent = (ClaimEvent)pluginHelper.Component;
                        if (clmEvent.IsNew)
                        {
                            this.SetTaskInitialUserForEvents(pluginHelper, clmEvent);
                        }

                        break;
                    }
            }

            return pluginHelper.ProcessResults;
        }

        /// <summary>
        /// Method sets the default TaskInitialUserID and TaskInitialDueDate at the point of creation.
        /// </summary>
        /// <param name="pluginHelper">plugin helper of business component</param>
        private void UpdateTaskInitialUser(PluginHelper<IBusinessComponent> pluginHelper)
        {
            // Default the TaskInitialUserID on Event with the Main Claim Handler
            // only if the ClaimEvent is in the expected event list
            ClaimEvent claimEvent = (ClaimEvent)pluginHelper.Component;
            if (claimEvent.EventTypeCode.Equals(ClaimConstants.EVENT_CLAIM_CREATED_TYPECODE) || claimEvent.EventTypeCode.Equals(ClaimConstants.EVENT_INACTIVITY_REVIEW_TYPECODE) || claimEvent.EventTypeCode.Equals(ClaimConstants.EVENT_PAYMENT_CANCELLATION_TYPECODE)
                    || claimEvent.EventTypeCode.Equals(ClaimConstants.EVENT_PHONE_TYPECODE) || claimEvent.EventTypeCode.Equals(ClaimConstants.EVENT_POST_TYPECODE) || claimEvent.EventTypeCode.Equals(ClaimConstants.EVENT_REPORT_REVIEW_TYPECODE)
                    || claimEvent.EventTypeCode.Equals(ClaimConstants.EVENT_REVIEW_TYPECODE) || claimEvent.EventTypeCode.Equals(ClaimConstants.EVENT_RECOVERY_INACTIVITY_REVIEW_TYPECODE) || claimEvent.EventTypeCode.Equals(ClaimConstants.EVENT_REOPEN_CLAIM_TYPECODE))
            {
                this.SetTaskInitialUserForEvents(pluginHelper, claimEvent);
                // Default the Task Initial Due Date to 'now' only if it hasn't been set already.
                if (!claimEvent.TaskInitialDueDate.HasValue || claimEvent.TaskInitialDueDate.Value == XiapConstants.StartOfTime)
                {
                    claimEvent.TaskInitialDueDate = DateTime.Now.Date;
                }
            }
        }

        /// <summary>
        /// Method sets the Default TaskInitialUserID on validate.
        /// </summary>
        /// <param name="pluginHelper">plugin helper of business component</param>
        /// <param name="claimEvent">component of type claim event</param>
        private void SetTaskInitialUserForEvents(PluginHelper<IBusinessComponent> pluginHelper, ClaimEvent claimEvent)
        {
            // UI Label = Priority?; Post event
            // If the event type is a "POST" type and the Priority? value (CustomCode02) is "REC", set the TaskInitialUser to null.
            if (claimEvent.EventTypeCode == ClaimConstants.EVENT_POST_TYPECODE && claimEvent.CustomCode02 == ClaimConstants.EVENT_PRIORITY_REC)   
            {
                claimEvent.TaskInitialUserID = null;
            }
            else if (claimEvent.EventTypeCode.Equals(ClaimConstants.EVENT_CLAIM_CREATED_TYPECODE) || claimEvent.EventTypeCode.Equals(ClaimConstants.EVENT_INACTIVITY_REVIEW_TYPECODE) || claimEvent.EventTypeCode.Equals(ClaimConstants.EVENT_PAYMENT_CANCELLATION_TYPECODE)
                    || claimEvent.EventTypeCode.Equals(ClaimConstants.EVENT_PHONE_TYPECODE) || claimEvent.EventTypeCode.Equals(ClaimConstants.EVENT_POST_TYPECODE) || claimEvent.EventTypeCode.Equals(ClaimConstants.EVENT_REPORT_REVIEW_TYPECODE)
                    || claimEvent.EventTypeCode.Equals(ClaimConstants.EVENT_REVIEW_TYPECODE) || claimEvent.EventTypeCode.Equals(ClaimConstants.EVENT_RECOVERY_INACTIVITY_REVIEW_TYPECODE) || claimEvent.EventTypeCode.Equals(ClaimConstants.EVENT_REOPEN_CLAIM_TYPECODE))
            {
                // Otherwise, if the event type is in the allowed values and the TaskInitialUser isn't set
                // set the user to the Claim Handler.
                if (!claimEvent.TaskInitialUserID.HasValue || claimEvent.TaskInitialUserID.Value == 0)
                {
                    long? userID = ClaimsBusinessLogicHelper.GetMainClaimHandlerUserID(pluginHelper);
                    claimEvent.TaskInitialUserID = userID;
                }
            }
        }
    }
}
