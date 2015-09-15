using System;
using System.Collections.Generic;
using Xiap.Framework.Common;
using Xiap.Framework.Data.Tasks;
using Xiap.Framework.Logging;
using Xiap.Metadata.BusinessComponent;

namespace GeniusX.AXA.Claims.BusinessLogic
{
	public class QualityCheckTask : IScheduledTask
	{
		private static readonly ILogger _Logger = LoggerFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private const string QualityControlEventTypeCode = "QualityControlEventTypeCode";
		private const string QualityControlProcess = "QualityControlProcess";
		private const string ClaimUserRoleCode = "ClaimUserRoleCode";

        /// <summary>
        /// called By Scheduled to create Event -Event Type Code= "QUAL" 
        /// </summary>
        /// <param name="args">Event Args </param>
        /// <returns>ScheduledTaskResponse object </returns>
		public ScheduledTaskResponse Invoke(string[] args)
		{
			bool retVal = true;
			if (_Logger.IsDebugEnabled)
			{
				_Logger.Debug("Invoke()");
			}

			// Make sure all mandatory requirements are satisfied.
			string qualityControlEventTypeCode = ClaimsBusinessLogicHelper.ResolveMandatoryConfig<string>(QualityControlEventTypeCode);
			string processName = ClaimsBusinessLogicHelper.ResolveMandatoryConfig<string>(QualityControlProcess);
			string claimUserRoleCode = ClaimsBusinessLogicHelper.ResolveMandatoryConfig<string>(ClaimUserRoleCode);

			// Get all users in the 'Claim User' role code.
			List<User> users = ClaimsProcessHelper.GetUsersByRole(claimUserRoleCode);

			foreach (User user in users)
			{
                try
                {
                    // Get all the finished tasks within last 7 days per user.
                    List<ITaskProcess> processDetails = ClaimsProcessHelper.GetDistinctFinishedReviewTasksForUserByDateRance(user.UserIdentity, DateTime.Now.AddDays(-7).Date, DateTime.Now.Date);

                    // Generate a collection of distinct claim references from process folios
                    List<string> claimReferences = this.GetAllClaimReferencesFromFolio(processDetails);

                    string claimReference = string.Empty;
                    while (claimReferences.Count > 0)
                    {
                        /* 
                         * Randomly select a claim and see if the user is a main claim handler on that claim 
                         * and whether no quality control process has been initiated on it within last year.
                         * If the above conditions are all true, create a claim quality control event,
                         * else continue untill no claim is found to create a quality control event.
                        */
                        Random random = new Random();
                        claimReference = claimReferences[random.Next(0, claimReferences.Count - 1)];

                        if (ClaimsBusinessLogicHelper.IsUserMainClaimHandler(user.UserIdentity, claimReference)
                            && ClaimsProcessHelper.GetProcessCountByDate(processName, claimReference, DateTime.Now.AddDays(-365).Date, DateTime.Now.Date) == 0)
                        {
                            /*
                             * Claim Task Initial User will be the user's (claim handler's) mentor (stored in ClaimEvent.CustomReference01).
                             * If this is blank or null, it would be the user's manager.
                            */

                            long? taskInitialUserId = user.ManagerID;
                            // UI Label = Mentor
                            if (!string.IsNullOrEmpty(user.CustomReference01))
                            {
                                taskInitialUserId = Convert.ToInt64(user.CustomReference01);   // UI Label = Mentor
                            }

                            ClaimsBusinessLogicHelper.CreateClaimEvent(claimReference, qualityControlEventTypeCode, user.UserID, taskInitialUserId);
                            break;
                        }

                        claimReferences.Remove(claimReference);
                    }
                }
                catch (Exception ex)
                {
                    _Logger.Error(ex);
                    retVal = false;
                }
			}

			return ScheduledTaskResponse.CreateFromBool(retVal);
		}
		/// <summary>
       	/// Get Reference From Folio
        /// </summary>
       	/// <param name="processDetails"> ollection of Task Process Details</param>
        /// <returns>List of ClaimReferences</returns>
		private List<string> GetAllClaimReferencesFromFolio(List<ITaskProcess> processDetails)
		{
			List<string> claimReferences = new List<string>();

			if (processDetails != null && processDetails.Count > 0)
			{
				processDetails.ForEach(processDetail =>
					{
						string claimReference = ClaimsProcessHelper.GetClaimReferenceFromFolio(processDetail.Folio);
						if (!claimReferences.Contains(claimReference))
						{
							claimReferences.Add(claimReference);
						}
					});
			}

			return claimReferences;
		}
	}
}
