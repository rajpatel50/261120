using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Xiap.Claims.BusinessComponent;
using Xiap.Claims.BusinessLogic.AuthorityCheck;
using Xiap.Claims.BusinessLogic.AuthorityCheck.Calculation;
using Xiap.Framework;
using Xiap.Framework.Common;
using Xiap.Framework.Data.Tasks;
using Xiap.Framework.Logging;
using Xiap.Framework.Metadata;
using Xiap.Framework.Validation;
using Xiap.Metadata.BusinessComponent;
using Xiap.Metadata.Data.Enums;
using CheckType = Xiap.Metadata.Data.Enums.StaticValues.ClaimFinancialAuthorityCheckType;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    public class PaymentAuthorisationUserResolution : IDataCollection
    {
        private static readonly ILogger _Logger = LoggerFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private const string AuthorisationProcessName = "AuthorisationProcessName";
        private const string PaymentAuthorisationActivityName = "PaymentAuthorisationActivityName";
        private const string BusinessSupportRole = "BusinessSupportRole";
        private IAXAClaimsQuery query;

        /// <summary>
        /// Resolves a user who can authorise the payment. If none found return business support role.
        /// </summary>
        /// <param name="parameters">claimTransactionHeaderId and creatorUserIdentity</param>
        /// <returns>Xml destination type and name</returns>
        public XmlElement GetData(IDictionary<string, object> parameters)
        {
            ArgumentCheck.ArgumentNullCheck(parameters, "parameters");

            long claimTransactionHeaderId = Convert.ToInt64(parameters["claimTransactionHeaderId"]);
            string creatorUserIdentity = Convert.ToString(parameters["creatorUserIdentity"]);

            var document = new XmlDocument();
            var element = document.CreateElement("Destination");

            string destinationName;
            StaticValues.DestinationType destinationType;
            // call to the Resolve User method to begin the attempt to find a user to authorise the amount.
            if (this.TryResolveUser(claimTransactionHeaderId, creatorUserIdentity, out destinationName))
            {
                destinationType = StaticValues.DestinationType.User;
            }
            else
            {
                // No user so we use the default Role from the application configuration
               destinationType = StaticValues.DestinationType.Role;
               destinationName = ClaimsBusinessLogicHelper.ResolveMandatoryConfig<string>(BusinessSupportRole);
            }

            if (_Logger.IsDebugEnabled)
            {
                _Logger.Debug(string.Format("GetData({0}, {1}) => {2} - {3}", claimTransactionHeaderId, creatorUserIdentity, destinationType, destinationName));
            }

            // Return as XML.
            element.SetAttribute("DestinationType", destinationType.ToString());
            element.SetAttribute("DestinationName", destinationName);

            return element;
        }
        
        /// <summary>
        /// Resolve the Event Destination for any K2 FinancialAuthorisation tasks for the given Claim Transaction Header ID
        /// </summary>
        /// <param name="processName">Process Name</param>
        /// <param name="componentId">Componet ID</param>
        /// <param name="eventDestination">output Event Destination</param>
        /// <returns>True / False</returns>
        private bool TryFindEventDestinationForProcess(string processName, long componentId, out IEventDestination eventDestination)
        {
            // Get a link to the TaskService (K2)
            var taskService = ObjectFactory.Resolve<ITaskService>();
            long systemComponentId = SystemComponentConstants.ClaimTransaction;
            if (_Logger.IsDebugEnabled)
            {
                _Logger.Debug(string.Format("FindProcessEvents({0}, {1}, {2})", processName, componentId, systemComponentId));
            }

            // Find all tasks for the Process Name (FinancialAuthorisation), the Claim Transaction Header ID for Claim Transactions (system Component)
            var processEvents = taskService.FindProcessEvents(processName, componentId, systemComponentId);
            if (processEvents == null || processEvents.Count() == 0)
            {
                if (_Logger.IsDebugEnabled)
                {
                    _Logger.Debug(string.Format("TryFindEventDestinationForProcess({0}, {1}) - no process event found", processName, componentId.ToString()));
                }

                // Return null if we have no tasks.
                eventDestination = null;
                return false;
            }
            else if (processEvents.Count() > 1)
            {
                if (_Logger.IsDebugEnabled)
                {
                    _Logger.Debug(string.Format("TryFindEventDestinationForProcess({0}, {1}) - Found more than one process event", processName, componentId.ToString()));
                }

                // Return null if we have multiple tasks
                eventDestination = null;
                return false;
            }

            // We have ONE task so get the Event Destination from it.
            var eventDestinations = taskService.GetEventDestinationsForProcessEvent(processEvents.Single().ProcessEventID);
            if (eventDestinations.Count() == 0)
            {
                if (_Logger.IsDebugEnabled)
                {
                    _Logger.Debug(string.Format("TryFindEventDestinationForProcess({0}, {1}) - No EventDestination found", processName, componentId.ToString()));
                }

                // No destination user so return null
                eventDestination = null;
                return false;
            }

            // Return the Event Destination
            eventDestination = eventDestinations.First();
            return true;
        }

        /// <summary>
        /// Try to get a user, either via an existing open payment authorisation, or via the random selection system.
        /// </summary>
        /// <param name="claimTransactionHeaderId">Claim Transaction HeaderId</param>
        /// <param name="creatorUserIdentity">Creator User Identity</param>
        /// <param name="userIdentity">output User Identity</param>
        /// <returns>True / False</returns>
        private bool TryResolveUser(long claimTransactionHeaderId, string creatorUserIdentity, out string userIdentity)
        {
            ArgumentCheck.ArgumentZeroCheck(claimTransactionHeaderId, "claimTransactionHeaderId");
            ArgumentCheck.ArgumentNullOrEmptyCheck(creatorUserIdentity, "creatorUserIdentity");
            this.query = ObjectFactory.Resolve<IAXAClaimsQuery>();

            if (_Logger.IsDebugEnabled)
            {
                _Logger.Debug(string.Format("TryResolveUser({0}, {1})", claimTransactionHeaderId, creatorUserIdentity));
            }

            // Get all the Claim Transaction Header data for the passed in CTH ID
            // Then find all the payment transactions on the Claim associated with the CTH
            var claimTransactionHeader = ObjectFactory.Resolve<IClaimsQuery>().GetClaimTransactionHeader(claimTransactionHeaderId);
            var claimAllPaymentTransactions = ObjectFactory.Resolve<IClaimsQuery>().GetAllClaimTransactionHeaders(claimTransactionHeader.ClaimHeader.ClaimHeaderID, StaticValues.ClaimTransactionSource.Payment);

            if (claimAllPaymentTransactions != null)
            {
                // We have one or more Payment Transactions, so order them by the most recent down to the earliest.
                claimAllPaymentTransactions = claimAllPaymentTransactions.OrderByDescending(o => o.CreatedDate);

                if (_Logger.IsDebugEnabled)
                {
                    _Logger.Debug(string.Format("GetAllClaimTransactionHeaders().Count = {0}", claimAllPaymentTransactions.Count()));
                }

                IEventDestination eventDestination;
                // Cycle through ever CTH in the payment transactions
                foreach (ClaimTransactionHeader clmTxnHeader in claimAllPaymentTransactions)
                {
                    if (_Logger.IsDebugEnabled)
                    {
                        _Logger.Debug(string.Format("ClaimTransactionHeader.PaymentAuthorisationStatus = {0}, ClaimTransactionHeader.PaymentAuthorisationStatus = {1}", clmTxnHeader.PaymentAuthorisationStatus, creatorUserIdentity));
                    }

                    // Check if this is an Unauthorised payment where a SINGLE Task in the Financial authorisation Process exists for it
                    if (clmTxnHeader.PaymentAuthorisationStatus == (short)StaticValues.PaymentAuthorisationStatus.PaymentUnauthorised 
                        && this.TryFindEventDestinationForProcess(ClaimConstants.FINANCIAL_AUTHORISATION_PROCESS, clmTxnHeader.ClaimTransactionHeaderID, out eventDestination))
                    {
                        if (!string.IsNullOrWhiteSpace(eventDestination.DestinationName) && eventDestination.DestinationType == (short)StaticValues.DestinationType.User)
                        {
                            // We have a Destination Name that is a user, not a role, so return it.
                            userIdentity = eventDestination.DestinationName;
                            if (_Logger.IsDebugEnabled)
                            {
                                _Logger.Debug(string.Format("EventDestination.DestinationName = {0}", userIdentity));
                            }

                            return true;
                        }
                        else
                        {
                            // No user found so return no identity
                            userIdentity = string.Empty;
                            return false;
                        }
                    }
                }
            }

            // There are no payment transactions, so we use a random process to select who will authorise this payment.
            // First: get a grade structure type from the Claim Transaction Header
            string gradeStructureType = this.GetGradeStructureType(claimTransactionHeader);
            if (!string.IsNullOrEmpty(gradeStructureType))
            {
                // Get all the greade codes for this structure type, ordered from lowest up to highest, but beginning with the 
                // same grade as the user who created this payment, since we assume their grade is the most likely match for the amount.
                var allGradeCodes = this.SelectAllGradeCodesForGradeType(creatorUserIdentity, gradeStructureType);
                // Cycle through all the grades
                foreach (string gradeCode in allGradeCodes)
                {
                    // Find all the users who are eligible in the current grade: not the creator and not anyone with too many outstanding tasks.
                    var eligibleUsers = this.ResolveAvailableAuthorisers(gradeCode, creatorUserIdentity).ToList();
                    // Create a random object to allow us to randomly select a user.
                    var random = new Random();

                    // While we still have users to check in this grade, determine if a randomly selected one will be able to authorise.
                    while (eligibleUsers.Count() > 0)
                    {
                        // Randomly select a remaining user
                        var candidateUser = eligibleUsers[random.Next(eligibleUsers.Count() - 1)];
                        if (this.CanUserAuthorisePayment(candidateUser, claimTransactionHeader))
                        {
                            // if they can authorise this payment prepare to use them
                            userIdentity = candidateUser.UserIdentity;
                            if (_Logger.IsDebugEnabled)
                            {
                                _Logger.Debug(string.Format("CandidateUser.UserIdentity = {0}", userIdentity));
                            }

                            bool result = false;
                            try
                            {
                                string taskRedirectUserIdentity = string.Empty;
                                // if the user is out of office continue looking, otherwise: RETURN THEM
                                result = this.query.IsUserOutOfOffice(candidateUser.UserID, out taskRedirectUserIdentity);
                                if (result)
                                {
                                    if (candidateUser.AvailabilityDatePattern.IsUserAvailabilityOverrideTaskRedirectAllowed.GetValueOrDefault(false) && !string.IsNullOrEmpty(taskRedirectUserIdentity))
                                    {
                                        userIdentity = taskRedirectUserIdentity;
                                        return true;
                                    }

                                    userIdentity = string.Empty;
                                }
                                else
                                {
                                    return true;
                                }
                            }
                            catch (Exception ex)
                            {
                                // We have had an issue with the attempt to check out of office.
                                // Throw an exception
                                userIdentity = string.Empty;
                                throw ex;
                            }
                        }

                        // That user didn't work out. Remove them from the list of users at the current grade and make another random selection
                        eligibleUsers.Remove(candidateUser);
                    }
                }
            }

            // No user found so return null.
            userIdentity = null;
            return false;
        }

        /// <summary>
        /// Try to  Resolve all available User
        /// </summary>
        /// <param name="selectedGradeCode">Selected Grade Code</param>
        /// <param name="currentUserIdentity">current User Identity</param>
        /// <returns>True / False</returns>
        private IEnumerable<User> ResolveAvailableAuthorisers(string selectedGradeCode, string currentUserIdentity)
        {
            // Get the Authorisation process "FinancialAuthorisation" and Payment Auth Task "Authorise Payment"
            var authorisationProcess = ClaimsBusinessLogicHelper.ResolveMandatoryConfig<string>(AuthorisationProcessName);
            var paymentAuthorisationTask = ClaimsBusinessLogicHelper.ResolveMandatoryConfig<string>(PaymentAuthorisationActivityName);
            
            // Find all users for the given grade, excluding the user who created the event (currentUserIdentity)
            IEnumerable<User> usersForSelection = ObjectFactory.Resolve<IMetadataQuery>().GetUsersByGradeCode(selectedGradeCode).Where(a => !currentUserIdentity.Equals(a.UserIdentity, StringComparison.OrdinalIgnoreCase));

            if (_Logger.IsDebugEnabled)
            {
                _Logger.Debug(string.Format("ResolveAvailableAuthorisers: usersForSelection.Count() - {0} => {1}", usersForSelection.Count(), string.Join(",", usersForSelection.Select(a => a.UserIdentity))));
            }

            var taskService = ObjectFactory.Resolve<ITaskService>();
            // Get the current list of all the destination names for any active Authorise Payment tasks under Financial Authorisation.
            var eventDestinationList = taskService.GetActiveEventDestinations(authorisationProcess, paymentAuthorisationTask, DateTime.Now.Date, DateTime.Now.Date.AddDays(1).AddTicks(-1))
                                        .Where(a => a.DestinationName != null && a.DestinationType == (short)StaticValues.DestinationType.User)
                                        .Select(a => a.DestinationName.ToUpper()).ToList();

            if (_Logger.IsDebugEnabled)
            {
                _Logger.Debug(string.Format("ResolveAvailableAuthorisers: eventDestinationList.Count(ActiveTask) - {0}", eventDestinationList.Count()));
            }

            // Get the destination name from all financial authorisations assigned to a User not a Role/Group that finish by tomorrow.
            var eventDestinationEndList = taskService.GetFinishedEventDestinationByDateRange(authorisationProcess, (short)StaticValues.DestinationType.User, DateTime.Now.Date, DateTime.Now.Date.AddDays(1).AddTicks(-1))
                                    .Select(a => a.DestinationName.ToUpper());

            if (_Logger.IsDebugEnabled)
            {
                _Logger.Debug(string.Format("ResolveAvailableAuthorisers: eventDestinationEndList.Count({0})", eventDestinationEndList.Count()));
            }

            // Add the ended financial authoristion destinations names to the list
            // then create a lookup on the list based on its own members.
            eventDestinationList.AddRange(eventDestinationEndList.ToList());
            var eventDestinationLookup = eventDestinationList.ToLookup(a => a);

            // Find all the users in our list of available users who have a limit on the number of authorisation requests they can make
            // and where the number times they appear on the list of names in the Event Destination Names exceeds that limit.
            var usersExceedingLimit = from users in usersForSelection 
                                      where users.CustomNumeric01.HasValue   // UI Label = Auth request Llimit
                                      && eventDestinationLookup.Contains(users.UserIdentity.ToUpper())
                                      && eventDestinationLookup[users.UserIdentity.ToUpper()].Count() >= users.CustomNumeric01.Value   // UI Label = Auth request Llimit
                                      select users;

            if (_Logger.IsDebugEnabled)
            {
                _Logger.Debug(string.Format("ResolveAvailableAuthorisers: usersExceedingLimit.Count() - {0} => {1}", usersExceedingLimit.Count(), string.Join(",", usersExceedingLimit.Select(a => a.UserIdentity))));
            }

            // Filter the available users by removing all the users who are over their auth request limit.
            usersForSelection = usersForSelection.Except(usersExceedingLimit);

            if (_Logger.IsDebugEnabled)
            {
                _Logger.Debug(string.Format("ResolveAvailableAuthorisers({0}, {1}) => {2}", selectedGradeCode, currentUserIdentity, string.Join(",", usersForSelection.Select(a => a.UserIdentity))));
            }

            // Return these users
            return usersForSelection;
        }

        /// <summary>
        /// Get all grades for the current grade type as well as the user's grade.
        /// Throw an exception if we have no User.
        /// </summary>
        /// <param name="creatorUserIdentity">Creator User Identity</param>
        /// <param name="gradeStructureType">Grade Structure Type</param>
        /// <returns>Collection of gradesCodes</returns>
        private IEnumerable<string> SelectAllGradeCodesForGradeType(string creatorUserIdentity, string gradeStructureType)
        {
            List<string> gradesCodes = new List<string>();
            var entities = ObjectFactory.Resolve<IMetadataQuery>();
            long userID = entities.GetUserIdByUserIdentity(creatorUserIdentity);

            // No valid user for this identity? Throw an exception
            if (userID == 0)
            {
                throw new InvalidOperationException(string.Format("No user for identitiy: {0}", creatorUserIdentity));
            }

            // Get the user's grade in this grade structure, if any, and add it to the grade list.
            var result = entities.GetUserGradeCode(userID, gradeStructureType, StaticValues.GradeType.Claims);
            if (result != null)
            {
                gradesCodes.Add(result);
            }

            // Get all the grades in the grade list and order them by grade level.
            var sortedCodes = entities.GetGradesForGradeStructure(gradeStructureType)
                        .OrderBy(a => a.GradeLevel ?? 0)
                        .Select(a => a.Code);

            // Append these to the list of grade codes.
            gradesCodes.AddRange(sortedCodes);

            if (_Logger.IsDebugEnabled)
            {
                _Logger.Debug(string.Format("SelectAllGradeCodesForGradeType({0}, {1}) => {2}", creatorUserIdentity, gradeStructureType, string.Join(",", gradesCodes)));
            }

            return gradesCodes;
        }

        /// <summary>
        /// Check if the user can Authorise this payment or not
        /// </summary>
        /// <param name="user"> System User </param>
        /// <param name="claimTransactionHeader"> Claim Transaction Header </param>
        /// <returns> True / False </returns>
        private bool CanUserAuthorisePayment(User user, ClaimTransactionHeader claimTransactionHeader)
        {
            // Can only authorise unauthorised items. Return false if it's not in that list.
            if (claimTransactionHeader.PaymentAuthorisationStatus == (short)StaticValues.PaymentAuthorisationStatus.PaymentAuthorised
                || claimTransactionHeader.PaymentAuthorisationStatus == (short)StaticValues.PaymentAuthorisationStatus.PaymentRejected
                || claimTransactionHeader.PaymentAuthorisationStatus == (short)StaticValues.PaymentAuthorisationStatus.NotApplicable)
            {
                if (_Logger.IsDebugEnabled)
                {
                    _Logger.Debug(string.Format("CanUserAuthorisePayment({0}, {1}) => invalid claim transaction header status", user.UserIdentity, claimTransactionHeader.ClaimTransactionHeaderID));
                }
                
                return false;
            }

            // Don't authorise a reserve if it's not pending a payment
            if (claimTransactionHeader.ReserveAuthorisationStatus == (short)StaticValues.ReserveAuthorisationStatus.ReserveUnauthorised
                && claimTransactionHeader.IsReservePendingPaymentAction == false)
            {
                if (_Logger.IsDebugEnabled)
                {
                    _Logger.Debug(string.Format("CanUserAuthorisePayment({0}, {1}) => invalid reserve status", user.UserIdentity, claimTransactionHeader.ClaimTransactionHeaderID));
                }
                
                return false;
            }

            var claimHeader = claimTransactionHeader.ClaimHeader;
            var product = claimHeader.GetProduct();
            var checkLog = new List<ClaimFinancialAuthorityLimitCheckResult>();

            // Check if the current user can do a manual authorisation based on the Transaction Payment Amount
            if (!this.CanPerformManualCheck(user, checkLog, product, CheckType.TransactionPaymentAmount, claimTransactionHeader))
            {
                if (_Logger.IsDebugEnabled)
                {
                    _Logger.Debug(string.Format("CanUserAuthorisePayment({0}, {1}) => No: TransactionPaymentAmount", user.UserIdentity, claimTransactionHeader.ClaimTransactionHeaderID));
                }
                
                return false;
            }

            // Check if total on the Claim Details prevents the user from manually authorising this amount.
            foreach (var claimTransactionGroup in claimTransactionHeader.ClaimTransactionGroups)
            {
                var claimDetail = claimTransactionGroup.ClaimDetail;
                if (!this.CanPerformManualCheck(user, checkLog, product, CheckType.TotalClaimDetailPaymentAmount, claimDetail, claimTransactionHeader))
                {
                    if (_Logger.IsDebugEnabled)
                    {
                        _Logger.Debug(string.Format("CanUserAuthorisePayment({0}, {1}) => No: TotalClaimDetailPaymentAmount", user.UserIdentity, claimTransactionHeader.ClaimTransactionHeaderID));
                    }
                    
                    return false;
                }
            }

            // Check if the total claim payment amount prevents the user from authorising this claim payment
            if (!this.CanPerformManualCheck(user, checkLog, product, CheckType.TotalClaimPaymentAmount, claimHeader, claimTransactionHeader))
            {
                if (_Logger.IsDebugEnabled)
                {
                    _Logger.Debug(string.Format("CanUserAuthorisePayment({0}, {1}) => No: TotalClaimPaymentAmount", user.UserIdentity, claimTransactionHeader.ClaimTransactionHeaderID));
                }
                
                return false;
            }

            // Check if the total incurred amount on the Claim Detail prevents the user from manually authorising the payment
            foreach (var claimTransactionGroup in claimTransactionHeader.ClaimTransactionGroups)
            {
                var claimDetail = claimTransactionGroup.ClaimDetail;
                if (!this.CanPerformManualCheck(user, checkLog, product, CheckType.TotalClaimDetailIncurredAmount, claimDetail, claimTransactionHeader))
                {
                    if (_Logger.IsDebugEnabled)
                    {
                        _Logger.Debug(string.Format("CanUserAuthorisePayment({0}, {1}) => No: TotalClaimDetailIncurredAmount", user.UserIdentity, claimTransactionHeader.ClaimTransactionHeaderID));
                    }
                    
                    return false;
                }
            }

            // Check if the total incurred amount on the claim prevents a manual authorisation of the payment by the user
            if (!this.CanPerformManualCheck(user, checkLog, product, CheckType.TotalClaimIncurredAmount, claimHeader, claimTransactionHeader))
            {
                if (_Logger.IsDebugEnabled)
                {
                    _Logger.Debug(string.Format("CanUserAuthorisePayment({0}, {1}) => No: TotalClaimIncurredAmount", user.UserIdentity, claimTransactionHeader.ClaimTransactionHeaderID));
                }
                
                return false;
            }

            // If any authorisation check wasn't active, rather than returning true or false
            // AND we don't allow manual authorisation without these checks, return false.
            if (checkLog.Count(a => a != ClaimFinancialAuthorityLimitCheckResult.NullValue) == 0
                && !product.ProductClaimDefinition.IsManualAuthorisationAlwaysAllowedIfNoChecksAreActive.GetValueOrDefault(false))
            {
                if (_Logger.IsDebugEnabled)
                {
                    _Logger.Debug(string.Format("CanUserAuthorisePayment({0}, {1}) => No checks configured", user.UserIdentity, claimTransactionHeader.ClaimTransactionHeaderID));
                }
                
                return false;
            }

            // Allowed to authorise
            return true;
        }

        /// <summary>
        /// Check if the user can Manually Authorise the payment 
        /// </summary>
        /// <param name="user"> System User </param>
        /// <param name="checkLog"> Collection of Claim Financial Authority Limit Check Result </param>
        /// <param name="product"> Product Version  </param>
        /// <param name="check"> Enum Claim Financial Authority Check Type</param>
        /// <param name="claimTransactionHeader">Claim Transaction Header</param>
        /// <returns> Bool True / False </returns>
        protected bool CanPerformManualCheck(User user, List<ClaimFinancialAuthorityLimitCheckResult> checkLog, ProductVersion product, StaticValues.ClaimFinancialAuthorityCheckType check, ClaimTransactionHeader claimTransactionHeader)
        {
            FinancialAuthorityCheckComponents components = new FinancialAuthorityCheckComponents(claimTransactionHeader.ClaimHeader, null, claimTransactionHeader);

            var claimTransactionArgs = new ClaimTransactionHeaderArgument(AmountDataSource.Historical, claimTransactionHeader);
            // Determine if the manual auth is possible.
            var result = ClaimFinancialAuthorityCheckUtil.PerformUserLimitCheck(user.UserID, product, StaticValues.ClaimFinancialAuthorityLimitType.LimitForManualAuthorisation, check, claimTransactionArgs, components, null);
            // Add the result to the checklog that was passed in, a list of results of the ClaimFinancialAuthorityChecks
            checkLog.Add(result);
            // Return true or false, appropriately
            return result == ClaimFinancialAuthorityLimitCheckResult.NullValue || result.IsAuthorised;
        }

        /// <summary>
        /// Check if the user can Manually Authorise the payment
        /// </summary>
        /// <param name="user"> System User</param>
        /// <param name="checkLog"> Collection of ClaimFinancialAuthorityLimitCheckResult </param>
        /// <param name="product"> Product Version </param>
        /// <param name="check"> Claim Financial Authority Check Type</param>
        /// <param name="claimDetail"> Claim Detail </param>
        /// <param name="claimTransactionHeader"> Claim Transaction Header </param>
        /// <returns> Bool True / False </returns>
        protected bool CanPerformManualCheck(User user, List<ClaimFinancialAuthorityLimitCheckResult> checkLog, ProductVersion product, StaticValues.ClaimFinancialAuthorityCheckType check, ClaimDetail claimDetail, ClaimTransactionHeader claimTransactionHeader)
        {
            FinancialAuthorityCheckComponents components = new FinancialAuthorityCheckComponents(claimTransactionHeader.ClaimHeader, claimDetail, claimTransactionHeader);
            var claimDetailArgs = new ClaimDetailArgument(AmountDataSource.Historical, claimDetail, claimTransactionHeader);
            var result = ClaimFinancialAuthorityCheckUtil.PerformUserLimitCheck(user.UserID, product, StaticValues.ClaimFinancialAuthorityLimitType.LimitForManualAuthorisation, check, claimDetailArgs, components, null);
            checkLog.Add(result);
            return result == ClaimFinancialAuthorityLimitCheckResult.NullValue || result.IsAuthorised;
        }

        /// <summary>
        /// Check if the user can Manually Authorise the payment 
        /// </summary>
        /// <param name="user">Syatem User</param>
        /// <param name="checkLog"> Collection of ClaimFinancialAuthorityLimitCheckResult </param>
        /// <param name="product"> Product Version </param>
        /// <param name="check"> Claim Financial Authority Check Type </param>
        /// <param name="header"> Claim mHeader</param>
        /// <param name="claimTransactionHeader"> Claim Transaction Header </param>
        /// <returns> True / False</returns>
        protected bool CanPerformManualCheck(User user, List<ClaimFinancialAuthorityLimitCheckResult> checkLog, ProductVersion product, StaticValues.ClaimFinancialAuthorityCheckType check, ClaimHeader header, ClaimTransactionHeader claimTransactionHeader)
        {
            FinancialAuthorityCheckComponents components = new FinancialAuthorityCheckComponents(claimTransactionHeader.ClaimHeader, null, claimTransactionHeader);
            var claimArgs = new ClaimHeaderArgument(AmountDataSource.Historical, header, claimTransactionHeader);
            var result = ClaimFinancialAuthorityCheckUtil.PerformUserLimitCheck(user.UserID, product, StaticValues.ClaimFinancialAuthorityLimitType.LimitForManualAuthorisation, check, claimArgs, components,null);
            checkLog.Add(result);
            return result == ClaimFinancialAuthorityLimitCheckResult.NullValue || result.IsAuthorised;
        }

        /// <summary>
        /// Check user grade Structure Type based on claim type. 
        /// If there are no claim details on the claim, return an empty string.
        /// </summary>
        /// <param name="claimtransactionheader">Claim transaction header</param>
        /// <returns>Grade Structure Type</returns>
        private string GetGradeStructureType(ClaimTransactionHeader claimtransactionheader)
        {
            string gradeStructureType= string.Empty;
            ClaimHeader clmHeader = claimtransactionheader.ClaimHeader;
            if (clmHeader.ClaimDetails != null)
            {
                switch (clmHeader.ClaimHeaderAnalysisCode01.ToUpper())
                {
                    case ClaimConstants.CH_ANALYSISCODE_LIABILITY:
                        gradeStructureType = ClaimConstants.PRODUCT_LIABILITY_GRADESTRUCTURETYPE;
                        break;
                    case ClaimConstants.CH_ANALYSISCODE_MOTOR:
                        if (clmHeader.ClaimDetails.Any(d => d.ClaimDetailTypeCode == ClaimConstants.CLAIMDETAILTYPECODE_TPI))
                        {
                            gradeStructureType = ClaimConstants.PRODUCT_MOTOR_GRADESTRUCTURETYPE_CLAIMDETAIL_TPI;
                        }
                        else
                        {
                            gradeStructureType = ClaimConstants.PRODUCT_MOTOR_GRADESTRUCTURETYPE_CLAIMDETAIL_NOTTPI;
                        }

                        break;
                }
            }

            return gradeStructureType;
        }
    }
}
