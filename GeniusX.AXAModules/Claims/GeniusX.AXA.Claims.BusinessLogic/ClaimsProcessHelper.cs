using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.Common;
using Xiap.Framework.Data.Tasks;
using Xiap.Framework.Messages;
using Xiap.Framework.ProcessHandling;
using Xiap.Framework.Validation;
using Xiap.Metadata.Data.Enums;
using Xiap.Metadata.BusinessComponent;
using K2ServiceClient.ProcessManagement;
using FrameworkSecurity = Xiap.Framework.Security;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    /// <summary>
    /// Static Helper class for K2 Task Process work.
    /// </summary>
	public static class ClaimsProcessHelper
	{
		public const string AXACLAIM_WORKFLOW_MAPPING = "//AXAClaimProcessHandlerFlowMaps/AXAClaimProcessHandlerFlowMap";

		public const string CLAIMHEADERREFERENCE = "ClaimHeaderReference";
		public const string CLAIMDETAILREFERENCE = "ClaimDetailReference";
		public const string CLAIMACTION = "ClaimAction";
		public const string TASKUSER = "TaskUser";
        public const string CLAIMEVENTID = "ClaimEventID";
		public const string CLAIM_REVIEW_TASKDUEDATE = "ReviewTaskDueDate";
        public const string CLAIM_CLOSE_ACTIONTAKEN = "Close Claim";

        /// <summary>
        /// Getting the name of claim Process on basis of workflow mapping.
        /// </summary>
        /// <param name="alias">string value</param>
        /// <returns>string value</returns>
		public static string GetClaimProcessName(string alias)
		{
			return ProcessHandlerConfigHelper.GetWorkflowName(AXACLAIM_WORKFLOW_MAPPING, alias);
		}

        /// <summary>
        /// Builds a Dictionary of key->value pairs, used in the creation of a Task Process on K2.
        /// </summary>
        /// <param name="header">component claim Header</param>
        /// <param name="pluginHelper">plugin helper</param>
        /// <returns>dictionary collection</returns>
		public static Dictionary<string, object> GenerateProcessData(ClaimHeader header, PluginHelper<IBusinessComponent> pluginHelper)
		{
			ArgumentCheck.ArgumentNullCheck(header, "ClaimHeader");
			ArgumentCheck.ArgumentNullCheck(pluginHelper.Component, "IBusinessComponent");

			Dictionary<string, object> data = new Dictionary<string, object>();

			ClaimEvent claimEvent = (ClaimEvent)pluginHelper.Component;

			data.Add(CLAIMHEADERREFERENCE, header.ClaimReference);

            // If we have a task initial user on the event, get this user's data and add them as the TaskUser in the Task data.
			if (claimEvent.TaskInitialUserID.HasValue)
			{
                FrameworkSecurity.User user = null;
				if (ClaimsBusinessLogicHelper.TryGetUserIdentityByUser(claimEvent.TaskInitialUserID.Value, out user))
				{
					data.Add(TASKUSER, user.UserIdentity);
				}
				else
				{
					pluginHelper.AddError(MessageConstants.USER_NOT_DEFINED, claimEvent.TaskInitialUserID.Value);
				}
			}

            data.Add(CLAIMEVENTID, claimEvent.ClaimEventID);
			data.Add(CLAIMDETAILREFERENCE, claimEvent.ClaimDetailReference);
			data.Add(CLAIM_REVIEW_TASKDUEDATE, claimEvent.TaskInitialDueDate);
			data.Add(CLAIMACTION, claimEvent.Context.TransactionType);
			return data;
		}

        /// <summary>
        /// Returns the Dictionary of K2 Task Process data as a string of format "{key}: {value},..."
        /// </summary>
        /// <param name="processData">dictionary collection</param>
        /// <returns>string value</returns>
		public static string GetProcessDataString(Dictionary<string, object> processData)
		{
			ArgumentCheck.ArgumentNullCheck(processData, "processData");
			StringBuilder dataString = new StringBuilder();
			foreach (string key in processData.Keys)
			{
				dataString.Append(key + ":" + processData[key] + ",");
			}

			return dataString.ToString();
		}

        /// <summary>
        /// Creates the Task folio name, including the given NameInvolvement's list name, if found.
        /// </summary>
        /// <param name="header">component claim header</param>
        /// <param name="claimNameInvolvement">claim name involvement</param>
        /// <returns>string value</returns>
        public static string GenerateFolio(ClaimHeader header, ClaimNameInvolvement claimNameInvolvement)
        {
            ArgumentCheck.ArgumentNullCheck(header, "header");

            string listName = string.Empty;
            if (claimNameInvolvement != null && claimNameInvolvement.NameID.HasValue != null)
            {
                listName = ClaimsBusinessLogicHelper.GetListName(claimNameInvolvement.NameID.Value);
            }

            return GenerateFolio(header.ClaimReference, listName);
        }

        /// <summary>
        /// Generates the folio using the claim reference and Insured list name (when available)
        /// </summary>
        /// <param name="claimReference">unique reference</param>
        /// <param name="insuredListName">string value</param>
        /// <returns>folio string value</returns>
		public static string GenerateFolio(string claimReference, string insuredListName)
		{
			ArgumentCheck.ArgumentNullCheck(claimReference, "claimReference");

            if (!string.IsNullOrEmpty(insuredListName))
			{
                return claimReference + "/" + insuredListName;
			}
			else
			{
				return claimReference;
			}
		}

        /// <summary>
        /// Returns a list of names for Review tasks.
        /// </summary>
        /// <returns>List of string value</returns>
		public static List<string> GetReviewProcessNames()
		{
			List<string> processNames = new List<string>();

            // Gets the Review Tassk Names from the application configuration.
			string autoReviewProcessName = ClaimsBusinessLogicHelper.ResolveMandatoryConfig<string>("AutoReviewProcessName");
			string manualReviewProcessName = ClaimsBusinessLogicHelper.ResolveMandatoryConfig<string>("ManualReviewProcessName");

			processNames.Add(autoReviewProcessName);
			processNames.Add(manualReviewProcessName);

			return processNames;
		}

        /// <summary>
        /// Gets the claim reference from the folio
        /// </summary>
        /// <param name="folio">string value</param>
        /// <returns>string value</returns>
		public static string GetClaimReferenceFromFolio(string folio)
		{
			ArgumentCheck.ArgumentNullCheck(folio, "folio");
			string retVal = folio;

            // If the folio had a List Name in it, it is of the form {ClaimReference} / {ListName}
			if (folio.Contains("/"))
			{
				retVal = folio.Split('/')[0];
			}

			return retVal;
		}

        /// <summary>
        /// Returns a list of users with the given RoleCode.
        /// </summary>
        /// <param name="roleCode">string value</param>
        /// <returns>List of type User</returns>
		public static List<User> GetUsersByRole(string roleCode)
		{
			ArgumentCheck.ArgumentNullCheck(roleCode, "roleCode");
            List<User> usersList = new List<User>();
            IEnumerable<User> users = ObjectFactory.Resolve<IMetadataQuery>().GetUsersByRole(roleCode);
            usersList.AddRange(users.ToList());

            return usersList;
		}

        /// <summary>
        /// Gets a list of Finished Review Tasks for the given user.
        /// </summary>
        /// <param name="userName">list of type ITaskProcess</param>
        /// <returns>List of ITaskProcess</returns>
		public static List<ITaskProcess> GetDistinctFinishedReviewTasksForUser(string userName)
		{
			List<string> processNames = new List<string>();

            // Gets the Review Tassk Names from the application configuration.
            string autoReviewProcessName = ClaimsBusinessLogicHelper.ResolveMandatoryConfig<string>("AutoReviewProcessName");
			string manualReviewProcessName = ClaimsBusinessLogicHelper.ResolveMandatoryConfig<string>("ManualReviewProcessName");

			processNames.Add(autoReviewProcessName);
			processNames.Add(manualReviewProcessName);
			
			ITaskService internalTaskService = ObjectFactory.Resolve<ITaskService>();
			return internalTaskService.GetFinishedTasksForUser(processNames, userName).Distinct<ITaskProcess>(new TaskProcessComparer()).ToList();
		}

        /// <summary>
        /// Gets a list of Finished Review Tasks for the given user within the given date range.
        /// </summary>
        /// <param name="userName">string value</param>
        /// <param name="startDate">dateTime value</param>
        /// <param name="endDate">DateTime value</param>
        /// <returns>List of ITaskProcess</returns>
		public static List<ITaskProcess> GetDistinctFinishedReviewTasksForUserByDateRance(string userName, DateTime startDate, DateTime endDate)
		{
			List<string> processNames = new List<string>();

            // Gets the Review Tassk Names from the application configuration.
            string autoReviewProcessName = ClaimsBusinessLogicHelper.ResolveMandatoryConfig<string>("AutoReviewProcessName");
			string manualReviewProcessName = ClaimsBusinessLogicHelper.ResolveMandatoryConfig<string>("ManualReviewProcessName");

			processNames.Add(autoReviewProcessName);
			processNames.Add(manualReviewProcessName);

			ITaskService internalTaskService = ObjectFactory.Resolve<ITaskService>();
			return internalTaskService.GetFinishedTasksForUserByDateRange(processNames, userName, startDate, endDate).Distinct<ITaskProcess>(new TaskProcessComparer()).ToList();
		}

        /// <summary>
        /// Close all tasks for the given claim header
        /// </summary>
        /// <param name="header">component header</param>
		public static void CloseAllTasks(ClaimHeader header)
		{
			string folio = header.ClaimReference;
            folio = folio + "%";    // Find all matching folios that begin with the header reference.
            string actioningUser = FrameworkSecurity.XiapSecurity.GetUser().UserIdentity;
            ProcessHandlerHelper.TerminateProcesses(new TerminateProcessRequest() { Folio = folio, ActioningUser = actioningUser, ActionTaken = CLAIM_CLOSE_ACTIONTAKEN });
		}

        /// <summary>
        /// Gets the count of task processes of the given name, on the basis of the given date range, for a given folio.
        /// </summary>
        /// <param name="processName">string value</param>
        /// <param name="folio">string value</param>
        /// <param name="startDate">DateTime value</param>
        /// <param name="endDate">DateTime value</param>
        /// <returns>int value</returns>
		public static int GetProcessCountByDate(string processName, string folio, DateTime startDate, DateTime endDate)
		{
			ArgumentCheck.ArgumentNullCheck(processName, "processName");
			ArgumentCheck.ArgumentNullCheck(folio, "folio");

			ITaskService internalTaskService = ObjectFactory.Resolve<ITaskService>();
			return internalTaskService.GetProcessCountByProcessNameAndDateRange(processName, folio, startDate, endDate);
		}

        /// <summary>
        /// Get latest name involvement on the claim header for the given Name Involvment Type, or returns null.
        /// </summary>
        /// <param name="claimHeader">header component</param>
        /// <param name="nameInvolvementType">type of name involvement</param>
        /// <returns>Claim Name Involvement</returns>
        public static ClaimNameInvolvement GetLatestNameInvolvement(ClaimHeader claimHeader, StaticValues.NameInvolvementType nameInvolvementType)
        {
            if (claimHeader.NameInvolvements == null)
            {
                return null;
            }

            ClaimNameInvolvement nameInvolvement = claimHeader.ClaimInvolvements
                .Where(x => x.ClaimNameInvolvements
                    .Where(y => y.NameInvolvementType == (short?)nameInvolvementType && y.NameInvolvementMaintenanceStatus == (short)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest).Count() > 0)
                        .SelectMany(a=>a.ClaimNameInvolvements)
                            .Where(x => x.NameInvolvementType == (short?)nameInvolvementType && x.NameInvolvementMaintenanceStatus == (short)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest).FirstOrDefault();
            
            if (nameInvolvement == null)
            {
                return null;
            }

            return nameInvolvement;
        }

        /// <summary>
        /// Class to compare tasks process id with process id.
        /// </summary>
		internal class TaskProcessComparer : IEqualityComparer<ITaskProcess>
		{
			public bool Equals(ITaskProcess x, ITaskProcess y)
			{
				return x.ProcessID == y.ProcessID;
			}

            /// <summary>
            /// Gets the hash code for the task.
            /// </summary>
            /// <param name="obj">object of process type</param>
            /// <returns>int value</returns>
			public int GetHashCode(ITaskProcess obj)
			{
				return obj.ProcessName.GetHashCode() ^ obj.Folio.GetHashCode();
			}
		}
	}
}
