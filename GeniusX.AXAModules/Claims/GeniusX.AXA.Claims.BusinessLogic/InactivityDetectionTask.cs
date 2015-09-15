using System;
using System.Collections.Generic;
using System.Linq;
using Xiap.Framework;
using Xiap.Framework.Common;
using Xiap.Framework.Data.Tasks;
using Xiap.Framework.Logging;
using Xiap.Framework.Messages;
using System.Configuration;
using System.Data;
using Xiap.Framework.DecisionTable;
using Xiap.Framework.Security;
using Xiap.Metadata.Data.Enums;
using System.Data.SqlClient;
// using Batchrun = Xiap.Framework.Security;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    public class InactivityDetectionTask : IScheduledTask
    {
        private static readonly ILogger _Logger = LoggerFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private const string InactivityReviewEventTypeCode = "InactivityReviewEventTypeCode";
        private const string RecoveryInactivityReviewEventTypeCode = "RecoveryInactivityReviewEventTypeCode";
        public delegate List<InactivityClaimDetail> GetInactiveClaimDetail();
        public GetInactiveClaimDetail GetInactiveClaimDetails = new GetInactiveClaimDetail(GetInactiveClaimDetailsSP);
        /// <summary>
        /// Load Claim Handler UserID. Throw an exception if no GeniusX User is found attached to the Claim Handler Name.
        /// </summary>
        /// <param name="NameID">Name ID</param>
        /// <param name="ClaimReference">Claim Reference</param>
        /// <returns>Long User ID</returns>
        private static long LoadClaimHandlerUserID(long? NameID, string ClaimReference)
        {
            if (!NameID.HasValue)
            {
                throw new InvalidOperationException(string.Format("Claim Handler has no associated name. claim reference: {0}", ClaimReference));
            }

            Xiap.Framework.Security.User user;
            if (!ClaimsBusinessLogicHelper.TryGetUserByNameID(NameID.Value, out user))
            {
                throw new InvalidOperationException(string.Format("No name attachd to name id {0}", NameID));
            }

            return user.UserId;
        }

        /// <summary>
        /// Returns various inactivity threshold dates from the decision table for given Claim Detail types
        /// </summary>
        /// <returns>Dates for the claim details</returns>
        private static Dictionary<string, DateTime[]> GetInactiveThresholdDates()
        {
            string InactivityThresholdDecisionTableName = "CLMINACTR";

            Dictionary<string, DateTime[]> threshholdLimits = new Dictionary<string, DateTime[]>();
            List<string> cdtypes = new List<string>();

            // Add the Claim Detail types to the list.
            cdtypes.Add("AD");
            cdtypes.Add("LIA");
            cdtypes.Add("TPI");
            cdtypes.Add("TPPD");
            cdtypes.Add("TPVD");
            cdtypes.Add("*Any");

            // Cycle through each Claim Detail
            foreach (string cdtype in cdtypes)
            {
                IDecisionTableComponent decisionTableComponent = null;
                List<object> componentconditionfield = new List<object>();
                componentconditionfield.Add(cdtype);
                // Set default inactivity thresholds (in days)
                int inactivityThresholdPeriodReserve = 90;
                int inactivityThresholdPeriodRecoveryReserve = 90;

                try
                {
                    // Read the InactivityThresholdDecisionTable version that's active now from the database
                    var helper = ObjectFactory.Resolve<IDecisionTableHelper>();
                    decisionTableComponent = helper.Call(InactivityThresholdDecisionTableName, DateTime.Today, cdtypes);
                    // Overwrite the default threshold values with ones found in the Decision table for the current claim detail
                    inactivityThresholdPeriodReserve = Convert.ToInt32(decisionTableComponent.Action1.ToString());
                    inactivityThresholdPeriodRecoveryReserve = Convert.ToInt32(decisionTableComponent.Action2.ToString());
                }
                catch (Exception ex)
                {
                    if (_Logger.IsInfoEnabled)
                    {
                        _Logger.Info("Unable to find the decision table " + InactivityThresholdDecisionTableName);
                        _Logger.Info(ex);
                    }
                }

                DateTime inactivityThresholdDateReserve = DateTime.Now.AddDays(-inactivityThresholdPeriodReserve);
                DateTime inactivityThresholdDateRecoveryReserve = DateTime.Now.AddDays(-inactivityThresholdPeriodRecoveryReserve);

                // Store the dates and add to the list we will return.
                DateTime[] ThDates = new DateTime[2];
                ThDates[0] = inactivityThresholdDateReserve;
                ThDates[1] = inactivityThresholdDateRecoveryReserve;

                threshholdLimits.Add(cdtype, ThDates);
            }

            return threshholdLimits;
        }

        /// <summary>
        /// Gets all inactive claim details based on the dates.
        /// </summary>
        /// <returns>Liat of Inactive ClaimDetails</returns>
        public static List<InactivityClaimDetail> GetInactiveClaimDetailsSP()
        {
            // Connect to the Database
            string ConnectionString = ConfigurationManager.ConnectionStrings["Config"].ConnectionString;

            int CommandTimeOut = ConfigurationManager.AppSettings["DefaultSQLCommandTimeout"] == null ? 30 : int.Parse(ConfigurationManager.AppSettings["DefaultSQLCommandTimeout"]);

            if (_Logger.IsInfoEnabled)
            {
                _Logger.Info("SQLCommandTimeout for Claim Inactivity has been set to: " + CommandTimeOut.ToString() + " seconds.");
            }

            // Get the dates for each claim detail
            Dictionary<string, DateTime[]> inactiveThresholdDates = GetInactiveThresholdDates();

            // Create our output list.
            List<InactivityClaimDetail> InactiveClaims = new List<InactivityClaimDetail>();

            SqlConnection conn = new SqlConnection(ConnectionString);

            using (var connection = new SqlConnection(ConnectionString))
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "[Claims].[AXAGetInactiveClaims]";
                    command.Parameters.Add("@ADDate", SqlDbType.DateTime);
                    command.Parameters.Add("@LIADate", SqlDbType.DateTime);
                    command.Parameters.Add("@TPIDate", SqlDbType.DateTime);
                    command.Parameters.Add("@TPPDDate", SqlDbType.DateTime);
                    command.Parameters.Add("@TPVDDate", SqlDbType.DateTime);
                    command.Parameters.Add("@ANYDate", SqlDbType.DateTime);

                    command.Parameters.Add("@Rec_ADDate", SqlDbType.DateTime);
                    command.Parameters.Add("@Rec_LIADate", SqlDbType.DateTime);
                    command.Parameters.Add("@Rec_TPIDate", SqlDbType.DateTime);
                    command.Parameters.Add("@Rec_TPPDDate", SqlDbType.DateTime);
                    command.Parameters.Add("@Rec_TPVDDate", SqlDbType.DateTime);
                    command.Parameters.Add("@Rec_ANYDate", SqlDbType.DateTime);

                    command.Parameters["@ADDate"].Value = (DateTime)inactiveThresholdDates["AD"][0];
                    command.Parameters["@LIADate"].Value = (DateTime)inactiveThresholdDates["LIA"][0];
                    command.Parameters["@TPIDate"].Value = (DateTime)inactiveThresholdDates["TPI"][0];
                    command.Parameters["@TPPDDate"].Value = (DateTime)inactiveThresholdDates["TPPD"][0];
                    command.Parameters["@TPVDDate"].Value = (DateTime)inactiveThresholdDates["TPVD"][0];
                    command.Parameters["@ANYDate"].Value = (DateTime)inactiveThresholdDates["*Any"][0];

                    command.Parameters["@Rec_ADDate"].Value = (DateTime)inactiveThresholdDates["AD"][1];
                    command.Parameters["@Rec_LIADate"].Value = (DateTime)inactiveThresholdDates["LIA"][1];
                    command.Parameters["@Rec_TPIDate"].Value = (DateTime)inactiveThresholdDates["TPI"][1];
                    command.Parameters["@Rec_TPPDDate"].Value = (DateTime)inactiveThresholdDates["TPPD"][1];
                    command.Parameters["@Rec_TPVDDate"].Value = (DateTime)inactiveThresholdDates["TPVD"][1];
                    command.Parameters["@Rec_ANYDate"].Value = (DateTime)inactiveThresholdDates["*Any"][1];

                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandTimeout = CommandTimeOut;
                    connection.Open();
                    var reader = command.ExecuteReader(CommandBehavior.CloseConnection);

                    while (reader.Read())
                    {
                        var InactivityClaimDetail = new InactivityClaimDetail
                        {
                            ClaimHeaderID = Convert.ToInt32(reader["ClaimHeaderID"]),
                            ClaimDetailID = Convert.ToInt32(reader["ClaimDetailID"]),
                            CustomCode20 = Convert.ToString(reader["CustomCode20"]),
                            ClaimReference = Convert.ToString(reader["ClaimReference"]),
                            NameID = Convert.ToInt32(reader["NameID"]),
                            ClaimDetailReference = Convert.ToString(reader["ClaimDetailReference"]),
                            ClaimTransactionSource = Convert.ToInt32(reader["ClaimTransactionSource"]),
                        };

                        InactiveClaims.Add(InactivityClaimDetail);
                    }
                }
            }

            return InactiveClaims;
        }
        /// <summary>
        /// Create Claim Detail Event of the given event type code, on the given claim detail.
        /// We load the task initial user from the ClaimHandler passed in and use the current user as the 'created by' userID
        /// </summary>
        /// <param name="ClaimReference">Claim Reference</param>
        /// <param name="ClaimDetailReference">Claim Detail Reference</param>
        /// <param name="eventTypeCode">Event Type Code</param>
        /// <param name="NameID">Name ID</param>
        public void CreateClaimDetailEvent(string ClaimReference, string ClaimDetailReference, string eventTypeCode, long? NameID)
        {
            long taskInitialUserID = LoadClaimHandlerUserID(NameID, ClaimReference);
            long CreatedByuserID = XiapSecurity.GetUser().UserID;
            ClaimsBusinessLogicHelper.CreateClaimDetailEvent(ClaimReference, ClaimDetailReference, eventTypeCode, CreatedByuserID, taskInitialUserID);
        }

        /// <summary>
        /// Create Claim Event on the given claim, of the given event type code.
        /// We load the task initial user from the ClaimHandler passed in and use the current user as the 'created by' userID
        /// </summary>
        /// <param name="ClaimReference">Claim Reference</param>
        /// <param name="eventTypeCode">Event Type Code</param>
        /// <param name="NameID">Name ID</param>
        public void CreateClaimEvent(string ClaimReference, string eventTypeCode, long? NameID)
        {
            long taskInitialUserID = LoadClaimHandlerUserID(NameID, ClaimReference);
            long CreatedByuserID = XiapSecurity.GetUser().UserID;
            ClaimsBusinessLogicHelper.CreateClaimEvent(ClaimReference, eventTypeCode, CreatedByuserID, taskInitialUserID);
		}
        /// <summary>
        /// called By Scheduler to create Inactivity Event -Event Type Code= "INACT" 
        /// </summary>
        /// <param name="args">Event Args </param>
        /// <returns>ScheduledTaskResponse object</returns>
        public ScheduledTaskResponse Invoke(string[] args)
        {
            if (_Logger.IsDebugEnabled)
            {
                _Logger.Debug("Invoke()");
            }

            bool retVal = true;
            var taskService = ObjectFactory.Resolve<ITaskService>();

            // Get a list of the inactive Claim Details.
            List<InactivityClaimDetail> inactiveClaimDetails = this.GetInactiveClaimDetails();

            List<InactivityClaimDetail> claimDetails = new List<InactivityClaimDetail>();
            try
            {
                // We may have multiple inactive details per claim header so get a separate list of only ClaimHeaders
                // Then cycle through each header.
                List<string> claimHeaders = inactiveClaimDetails.Select(y => y.ClaimReference).Distinct().ToList();
                foreach (string claimReference in claimHeaders)
                {
                    // Get only claim details that are attached to Reserve claim transactions against the current claim reference
                    List<InactivityClaimDetail> inactiveReserveClaimDetails =  inactiveClaimDetails.Where(x => x.ClaimReference == claimReference && x.ClaimTransactionSource == (int)StaticValues.ClaimTransactionSource.Reserve).ToList<InactivityClaimDetail>();
                    if (inactiveReserveClaimDetails != null && inactiveReserveClaimDetails.Count > 0)
                    {
                        // Collect common Claim Header items stored against every inactive claim detail by getting info from the first.
                        InactivityClaimDetail claimInfo = inactiveReserveClaimDetails.First();
                        long? claimheaderid = claimInfo.ClaimHeaderID;
                        long? NameID = claimInfo.NameID;                // Claim Handler NameID
                        string CustomCode20 = claimInfo.CustomCode20;   // Maximum Inactive Claims Details value
                        // Check if we need to raise an event.
                        this.ProcessInactiveClaimDetails(inactiveReserveClaimDetails, taskService, claimReference, CustomCode20, NameID, StaticValues.ClaimTransactionSource.Reserve);
                    }

                    // Get only claim details that are attached to RecoveryReserve claim transactions against the current claim reference
                    List<InactivityClaimDetail> inactiveRecoveryClaimDetails = inactiveClaimDetails.Where(x => x.ClaimReference == claimReference && x.ClaimTransactionSource == (int)StaticValues.ClaimTransactionSource.RecoveryReserve).ToList<InactivityClaimDetail>();
                    if (inactiveRecoveryClaimDetails != null && inactiveRecoveryClaimDetails.Count > 0)
                    {
                        // Collect common Claim Header items stored against every inactive claim detail by getting info from the first.
                        InactivityClaimDetail claimInfo = inactiveRecoveryClaimDetails.First();
                        long? claimheaderid = claimInfo.ClaimHeaderID;
                        long? NameID = claimInfo.NameID;                // Claim Handler NameID
                        string CustomCode20 = claimInfo.CustomCode20;   // Maximum Inactive Claims Details value
                        // Check if we need to raise an event.
                        this.ProcessInactiveClaimDetails(inactiveRecoveryClaimDetails, taskService, claimReference, CustomCode20, NameID, StaticValues.ClaimTransactionSource.RecoveryReserve);
                    }

                    if (_Logger.IsInfoEnabled)
                    {
                        _Logger.Info("Inactive Claim Reference : " + claimReference);
                    }
                }
            }
            catch (Exception ex)
            {
                _Logger.Error(ex);
                retVal = false;
            }

            return ScheduledTaskResponse.CreateFromBool(retVal);
        }

        /// <summary>
        /// Process Inactive ClaimDetails
        /// </summary>
        /// <param name="claimDetails">Claim Details</param>
        /// <param name="taskService">Task Service</param>
        /// <param name="ClaimReference">Claim Reference</param>
        /// <param name="CustomCode20"> Inactivity ClaimDetail.CustomCode20</param>
        /// <param name="NameID">Name ID</param>
        /// <param name="ClaimTransactionSource">Claim Transaction Source</param>
        private void ProcessInactiveClaimDetails(List<InactivityClaimDetail> claimDetails, ITaskService taskService, string ClaimReference, string CustomCode20, long? NameID, StaticValues.ClaimTransactionSource ClaimTransactionSource)
        {
            if (claimDetails == null || claimDetails.Count == 0)
            {
                return;
            }
            
            // Get the event type code from the Application Config
            string inactivityEventTypeCode = string.Empty;
            if (ClaimTransactionSource == StaticValues.ClaimTransactionSource.Reserve)
            {
                inactivityEventTypeCode = ClaimsBusinessLogicHelper.ResolveMandatoryConfig<string>(InactivityReviewEventTypeCode);
            }
            else
            {
                inactivityEventTypeCode = ClaimsBusinessLogicHelper.ResolveMandatoryConfig<string>(RecoveryInactivityReviewEventTypeCode);
            }

            // The number of Inactive Claim Details we are allowed to have in total.
            int maximumActiveClaims;
            if (!int.TryParse(CustomCode20, out maximumActiveClaims))
            {
                throw new InvalidOperationException(MessageServiceFactory.GetMessageBody(ClaimConstants.NoMaximumClaimDetailConfigured, claimDetails.First<InactivityClaimDetail>().ClaimHeaderID));
            }

            // if claim detail count is greater than max inactive claim details create claim header event instead
            if (claimDetails.Count > maximumActiveClaims)
            {
                try
                {
                    this.CreateClaimEvent(ClaimReference, inactivityEventTypeCode, NameID);
                }
                catch (Exception ex)
                {
                    if (_Logger.IsInfoEnabled)
                    {
                        _Logger.Info("Error in processing Inactive Claim Reference : " + ClaimReference);
                        _Logger.Info(ex);
                    }
                }
            }
            else
            {
                // Raise an inactivity claim detail event against each claim detail that has been inactive for too long, each of these creating a task.
                foreach (InactivityClaimDetail claimDetail in claimDetails)
                {
                    try
                    {
                        this.CreateClaimDetailEvent(ClaimReference, claimDetail.ClaimDetailReference, inactivityEventTypeCode, NameID);
                    }
                    catch (Exception ex)
                    {
                        if (_Logger.IsInfoEnabled)
                        {
                            _Logger.Info("Error in processing Inactive Claim Reference : " + ClaimReference);
                            _Logger.Info(ex);
                        }
                    }
                }
            }
        }
    }
}
