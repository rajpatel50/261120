using System;
using System.Linq;
using Xiap.Claims.BusinessComponent;
using Xiap.Claims.BusinessTransaction;
using Xiap.Framework.Common;
using Xiap.Framework.Logging;
using Xiap.Framework.Metadata;
using Xiap.Framework.Validation;
using Xiap.K2Integration.Task.BusinessComponent;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    public class TaskReassignEvent : IScheduledTask
    {
        private static readonly ILogger _Logger = LoggerFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Check we have the seven expected arguments passed in.  
        /// </summary>
        /// <param name="args"> list of string </param>
        /// <returns>ScheduledTaskResponse object</returns>
        public ScheduledTaskResponse Invoke(string[] args)
        {
            ArgumentCheck.ArgumentNullCheck(args[0], "args[0]");    // Serial Number from K2, a concatenation of ProcessInstanceID and ActivityDestinationInstanceID separated by an underscore
            ArgumentCheck.ArgumentNullCheck(args[1], "args[1]");    // Event Description
            ArgumentCheck.ArgumentNullCheck(args[2], "args[2]");    // Event Due Date
            ArgumentCheck.ArgumentNullCheck(args[3], "args[3]");    // Original Destination Name (User)
            ArgumentCheck.ArgumentNullCheck(args[4], "args[4]");    // Updated Destination Name (new user)
            ArgumentCheck.ArgumentNullCheck(args[5], "args[5]");    // Actioning User (UserID)
            ArgumentCheck.ArgumentNullCheck(args[6], "args[6]");    // Action Date

            if (_Logger.IsDebugEnabled)
            {
                _Logger.Debug(string.Format("TaskReassignEvent: Invoke({0})", args.Select(o => o.ToString())));
            }

            // Get the claim reference from the passed Process Instance ID
            string claimReference = this.GetClaimReference(args[0]);
            if (!string.IsNullOrEmpty(claimReference))
            {
                string eventDescription = args[1];
                DateTime eventDueDate = new DateTime(long.Parse(args[2]));
                string originalDestinationName = args[3];
                string updatedDestinationName = args[4];
                long actioningUser = Convert.ToInt64(args[5]);
                DateTime actionDate =  Convert.ToDateTime(args[6]);

                this.CreateEvent(claimReference, eventDueDate, eventDescription, originalDestinationName, updatedDestinationName, actioningUser, actionDate);
            }

            return ScheduledTaskResponse.CreateFromBool(true);
        }


        /// <summary>
        /// Truncate title if Length Greater than 50 (or supplied value)
        /// </summary>
        /// <param name="title">TITLE Name</param>
        /// <param name="length">Length of string by default it is 50</param>
        /// <returns>Result Collection</returns>
        private static string TruncateString(string title, int length = 50)
        {
            if (!string.IsNullOrEmpty(title) && title.Length > length)
            {
                title = string.Format("{0}", title.Substring(0, length));
            }

            return title;
        }

        /// <summary>
        /// extract process instanceid from serialnumber
        /// </summary>
        /// <param name="serialNumber">serial Number</param>
        /// <returns>instance id</returns>
        private static long ExtractProcessInstanceIDFromSerialNumber(string serialNumber)
        {
            // Find the position of the first underscore in the Serial Number
            var index = serialNumber.IndexOf('_');
            long processInstanceID;

            // If there wasn't an underscore then throw an exception.
            if (index == -1)
            {
                throw new ArgumentException("serialNumber " + serialNumber + " is malformed.  It must be a concatenation of ProcessInstanceID and ActivityDestinationInstanceID separated by an underscore");
            }

            // If we can't parse the first part of the serial number (before the underscore) to a long type, throw an exception
            if (!long.TryParse(serialNumber.Substring(0, index), out processInstanceID))
            {
                throw new ArgumentException("serialNumber " + serialNumber + " is malformed.  It must be a concatenation of ProcessInstanceID and ActivityDestinationInstanceID separated by an underscore");
            }

            // Return the process instance ID
            return processInstanceID;
        }

        /// <summary>
        /// Get claim Header Id from  process Instance ID
        /// </summary>
        /// <param name="serialNumber">serial Number</param>
        /// <returns>claim Header Id</returns>
        private string GetClaimReference(string serialNumber)
        {
            if (_Logger.IsDebugEnabled)
            {
                _Logger.Debug(string.Format("TaskReassignEvent: GetClaimReference({0})", serialNumber));
            }

            string claimReference = string.Empty;
            long? claimHeaderID;
            // Start an instance of the K2 link
            using (TaskEntities taskentities = TaskEntitiesFactory.GetTaskEntities())
            {
                // Get the Process Instance ID from the Serial Number
                long processInstanceID = ExtractProcessInstanceIDFromSerialNumber(serialNumber);
                if (_Logger.IsDebugEnabled)
                {
                    _Logger.Debug(string.Format("TaskReassignEvent: GetClaimReference() - processInstanceID = {0}", processInstanceID.ToString()));
                }
                
                // Get the claim Header ID via the Process Link on the database for Claim Header system components
                claimHeaderID = (from pl in taskentities.ProcessLink
                                 where pl.ProcessInstanceID == processInstanceID
                                 && pl.SystemComponentID == SystemComponentConstants.ClaimHeader
                                 select pl.ComponentID).FirstOrDefault();
            }

            // If we have a ClaimHeaderID then we get the claim reference from the Claim Header entity in GeniusX
            if (claimHeaderID != null)
            {
                if (_Logger.IsDebugEnabled)
                {
                    _Logger.Debug(string.Format("TaskReassignEvent: GetClaimReference() - claimHeaderID = {0}", claimHeaderID.Value.ToString()));
                }

                using (ClaimsEntities claimentities = ClaimsEntitiesFactory.GetClaimsEntities())
                {
                    claimReference = (from ch in claimentities.ClaimHeader
                                     where ch.ClaimHeaderID == claimHeaderID
                                     select ch.ClaimReference).FirstOrDefault();
                }
            }

            return claimReference;
        }

        /// <summary>
        /// Create the Task Reassignment Event
        /// </summary>
        /// <param name="claimReference">Claim Reference </param>
        /// <param name="eventDueDate"> event Due Date</param>
        /// <param name="eventDescription"> event Description</param>
        /// <param name="originalDestinationName">original Destination Name</param>
        /// <param name="updatedDestinationName">updated Destination Name</param>
        /// <param name="actioningUserID">actioning User ID</param>
        /// <param name="actionDate">action Date</param>
        private void CreateEvent(string claimReference, DateTime eventDueDate, string eventDescription, string originalDestinationName, string updatedDestinationName, long actioningUserID, DateTime actionDate)
        {
            if (_Logger.IsDebugEnabled)
            {
                _Logger.Debug(string.Format("TaskReassignEvent: CreateEvent({0}, {1}, {2}, {3}, {4}, {5})", claimReference, eventDueDate.ToString(), eventDescription, originalDestinationName, updatedDestinationName, actioningUserID.ToString()));
            }

            IClaimsBusinessTransaction transaction = null;
            try
            {
                // Create Task Reassignment event via a transaction on the claim
                transaction = ClaimsBusinessTransactionFactory.CreateEvent(claimReference, false, null, ClaimConstants.EVENT_TYPECODE_TASKRSGN, null);
                ClaimEvent claimEvent = (ClaimEvent)transaction.Component;
                if (claimEvent != null)
                {
                    // Put in extra detail assuming the event was completed succesfully.
                    claimEvent.CustomDate01 = eventDueDate;   // UI Label = Due Date At Reassignment; Task reasigned
                    claimEvent.CustomReference01 = TruncateString(eventDescription);   // UI Label = Task Type; Task reasigned
                    claimEvent.CustomReference02 = TruncateString(originalDestinationName);   // UI Label = Assigned From; Task reasigned
                    claimEvent.CustomReference03 = TruncateString(updatedDestinationName);   // UI Label = Assigned To; Task reasigned
                    claimEvent.CreatedByUserID = actioningUserID;
                    claimEvent.EventDate = actionDate;
                }

                // Complete the transaction
                transaction.Complete();

                if (_Logger.IsDebugEnabled)
                {
                    _Logger.Debug(string.Format("TaskReassignEvent: CreateEvent() - EventID = ", ((ClaimEvent)transaction.Component).ClaimEventID.ToString()));
                }
            }
            catch (Exception ex)
            {
                _Logger.Error(ex);
                if (transaction != null && transaction.Context != null)
                {
                    transaction.Cancel();
                }
            }
        }
    }
}

