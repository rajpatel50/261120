using System;
using System.Configuration;
using System.Linq;
using Xiap.Claims.BusinessComponent;
using Xiap.Claims.BusinessTransaction;
using Xiap.Framework;
using Xiap.Framework.DecisionTable;
using Xiap.Framework.Extensions;
using Xiap.Framework.Logging;
using Xiap.Framework.Messages;
using Xiap.Framework.ProcessHandling;
using Xiap.Framework.Utils;
using Xiap.Framework.Validation;
using Xiap.InsuranceDirectory.BusinessComponent;
using Xiap.Metadata.Data.Enums;
using Xiap.Framework.Configuration;
using System.Collections.Generic;
using Xiap.Metadata.BusinessComponent;
using Xiap.Framework.Data.Underwriting;
using FrameworkSecurity = Xiap.Framework.Security;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    public static class ClaimsBusinessLogicHelper
    {
        private static readonly ILogger _Logger = LoggerFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Method used to read key-value pair of config file.
        /// </summary>
        /// <typeparam name="T">Type value</typeparam>
        /// <param name="propertyName">string value</param>
        /// <returns>return type or throws InvalidOperationException exception</returns>
        public static T ResolveMandatoryConfig<T>(string propertyName)
        {
            var value = ConfigurationManager.AppSettings[propertyName];
            if (value == null)
            {
                throw new InvalidOperationException(string.Format("Missing config property: {0}", propertyName));
            }

            return (T)Convert.ChangeType(value, typeof(T));
        }

        /// <summary>
        /// Method used to resolve enum values from config file.
        /// </summary>
        /// <typeparam name="TEnum">enumerable type</typeparam>
        /// <param name="propertyName">string value</param>
        /// <returns>enum type value, or throws InvalidOperationException exception</returns>
		public static TEnum ResolveEnumFromConfig<TEnum>(string propertyName) where TEnum: struct
        {
            string propertyValue = ResolveMandatoryConfig<string>(propertyName);

            TEnum value;
            bool enumParsed = Enum.TryParse<TEnum>(propertyValue, true, out value);
            if (!enumParsed)
            {
                throw new InvalidOperationException(string.Format("(0) is not a valid value for config property {1} ", propertyValue, propertyName));
            }

            return value;
        }

        /// <summary>
        /// Getting the ID User ID of main claim handler. Raise an error if the claim handler has no name associated with it.
        /// </summary>
        /// <param name="pluginHelper">Business component type</param>
        /// <returns>nullable long value</returns>
        public static long? GetMainClaimHandlerUserID(PluginHelper<IBusinessComponent> pluginHelper)
        {
            long? userID = null;
            string userIdentity = string.Empty;
            ClaimEvent claimEvent = (ClaimEvent)pluginHelper.Component;
            ClaimHeader claimHeader = null;

            // The claim event will either be on a Claim Detail or at the Claim Header level
            // Get the claim header component from it accordingly
            if (claimEvent.Parent is ClaimDetail)
            {
                claimHeader = (ClaimHeader)claimEvent.Parent.Parent;
            }
            else
            {
                claimHeader = (ClaimHeader)claimEvent.Parent;
            }

            ClaimNameInvolvement claimNameInvolvement = GetMainClaimHandlerFromHeader(claimHeader);

            if (claimNameInvolvement != null)
            {
                if (claimNameInvolvement.NameID.HasValue)
                {
                    // Note the TryGetUserIdentityByName method takes the UserID as an 'out' parameter, so this will be filled via the call.
                    // The call directly returns a boolean to show whether it was successful in retrieving the User details.
                    if (!TryGetUserIdentityByName(claimNameInvolvement.NameID.Value, out userIdentity, out userID))
                    {
                        // Raise an error if there was no name on the Claim Name Involvement
                        pluginHelper.AddError(MessageConstants.USER_IS_NOT_CONFIGURED_FOR_NAME, claimNameInvolvement.NameID.Value); // throw an error.
                    }
                }
            }

            return userID;
        }

        /// <summary>
        /// Get the User Identity string by the NameID of the name it's associated with, e.g. a Claim Handler. 
        /// [This method isn't called anywhere in the main code.]
        /// </summary>
        /// <param name="nameID">NameID long value</param>
        /// <param name="userIdentity">output type string value</param>
        /// <returns>bool value</returns>
        public static bool TryGetUserIdentityByName(long nameID, out string userIdentity)
        {
            if (nameID == 0)
            {
                userIdentity = string.Empty;
                return false;
            }

            long? userID = null;
            return TryGetUserIdentityByName(nameID, out userIdentity, out userID);
        }

        /// <summary>
        /// Get the UserID and user identity using the NameID of the name it's associated with, e.g. a Claim Handler.
        /// </summary>
        /// <param name="nameID">NameID long value</param>
        /// <param name="userIdentity">output type string value</param>
        /// <param name="userID">output nullable long value</param>
        /// <returns>bool value</returns>
        public static bool TryGetUserIdentityByName(long nameID, out string userIdentity, out long? userID)
        {
            bool isUserExist = true;
            userIdentity = string.Empty;
            userID = null;

            if (nameID == 0)
            {
                return false;
            }

            FrameworkSecurity.User userDetail = FrameworkSecurity.UserCacheService.GetUserByNameId(nameID);
            if (userDetail == null)
            {
                isUserExist = false;
            }
            else
            {
                userIdentity = userDetail.UserIdentity;
                userID = userDetail.UserId;
            }

            return isUserExist;
        }

        /// <summary>
        /// Get the User object by UserID.
        /// Note, this method is badly named because UserIdentity is a field on the User object
        /// and this method returns the whole User Object.
        /// </summary>
        /// <param name="userID">UserID long value</param>
        /// <param name="user">output User object</param>
        /// <returns>bool value</returns>
        public static bool TryGetUserIdentityByUser(long userID, out FrameworkSecurity.User user)
        {
            bool isUserExist = true;
            user = FrameworkSecurity.UserCacheService.GetUserById(userID);
            if (user == null)
            {
                isUserExist = false;
            }

            return isUserExist;
        }

        /// <summary>
        /// Get the User object by the NameID of the name it's associated with, e.g. a Claim Handler.
        /// </summary>
        /// <param name="nameID">NameID long value</param>
        /// <param name="user">output User object</param>
        /// <returns>bool value</returns>
        public static bool TryGetUserByNameID(long nameID, out FrameworkSecurity.User user)
        {
            ArgumentCheck.ArgumentZeroCheck(nameID, "nameID");

            user = FrameworkSecurity.UserCacheService.GetUserByNameId(nameID);
            return user != null;
        }

        /// <summary>
        /// Creates the given claim detail event on the given claim detail with no user detail.
        /// </summary>
        /// <param name="claimReference">The claim reference.</param>
        /// <param name="claimDetailReference">The claim detail reference.</param>
        /// <param name="eventTypeCode">The event type code.</param>
        public static void CreateClaimDetailEvent(string claimReference, string claimDetailReference, string eventTypeCode)
        {
            CreateClaimDetailEvent(claimReference, claimDetailReference, eventTypeCode, null, null);
        }


        /// <summary>
        /// Creates the given claim detail event on the given claim detail.
        /// If an exception is thrown, the transaction is cancelled but no error is thrown up to the calling routine.
        /// </summary>
        /// <param name="claimReference">The claim reference.</param>
        /// <param name="claimDetailReference">The claim detail reference.</param>
        /// <param name="eventTypeCode">The event type code.</param>
        /// <param name="createByUserID">The create by user identifier.</param>
        /// <param name="taskInitialUserID">The task initial user identifier.</param>
        public static void CreateClaimDetailEvent(string claimReference, string claimDetailReference, string eventTypeCode, long? createByUserID, long? taskInitialUserID)
        {
            IClaimsBusinessTransaction transaction = null;
            try
            {
                transaction = ClaimsBusinessTransactionFactory.CreateEvent(claimReference, true, claimDetailReference, null, eventTypeCode, null); // start the transaction of create event.
                if (createByUserID.HasValue || taskInitialUserID.HasValue)
                {
                    // If we have a UserID who created this or a Task initial UserID then we attept to set the CreatedByUserID and the TaskInitialUserID.
                    ClaimEvent claimEvent = (ClaimEvent)transaction.Component;
                    if (claimEvent != null)
                    {
                        claimEvent.CreatedByUserID = createByUserID;
                        ((ClaimEvent)claimEvent).TaskInitialUserID = taskInitialUserID;
                    }
                }

                transaction.Complete();
            }
            catch (Exception ex)
            {
                _Logger.Error(ex);
                if (transaction != null && transaction.Context != null)
                {
                    transaction.Cancel(); // if any error occured, cancel the transaction.
                }
            }
        }

        /// <summary>
        /// Creates the given claim event with no user details.
        /// </summary>
        /// <param name="claimReference">The claim reference.</param>
        /// <param name="eventTypeCode">The event type code.</param>
        public static void CreateClaimEvent(string claimReference, string eventTypeCode)
        {
            CreateClaimEvent(claimReference, eventTypeCode, null, null);
        }

        /// <summary>
        /// Creates the given claim event.
        /// </summary>
        /// <param name="claimReference">The claim reference.</param>
        /// <param name="eventTypeCode">The event type code.</param>
        /// <param name="createByUserID">The create by user identifier.</param>
        /// <param name="taskInitialUserID">The task initial user identifier.</param>
       public static void CreateClaimEvent(string claimReference, string eventTypeCode, long? createByUserID, long? taskInitialUserID)
        {
            IClaimsBusinessTransaction eventTransaction = null;
            try
            {
                eventTransaction = ClaimsBusinessTransactionFactory.CreateEvent(claimReference, true, null, null, eventTypeCode, null); // start the transaction of create event.
                // If we have a UserID who created this or a Task initial UserID then we attept to set the CreatedByUserID and the TaskInitialUserID.
                if (createByUserID.HasValue || taskInitialUserID.HasValue)
                {
                    ClaimEvent claimEvent = (ClaimEvent)eventTransaction.Component;
                    if (claimEvent != null)
                    {
                        claimEvent.CreatedByUserID = createByUserID;
                        ((ClaimEvent)claimEvent).TaskInitialUserID = taskInitialUserID;
                    }
                }

                eventTransaction.Complete(); // complete the transaction.
            }
            catch (Exception ex)
            {
                _Logger.Error(ex);
                if (eventTransaction != null && eventTransaction.Context != null)
                {
                    eventTransaction.Cancel(); // cancel the transaction, if any error occured.
                }
            }
        }


       /// <summary>
       /// Gets the main claim handler from the claim header.
       /// </summary>
       /// <param name="claimHeader">The claim header.</param>
       /// <returns>Claim Name Involvement component, or null, if none found</returns>
        public static ClaimNameInvolvement GetMainClaimHandlerFromHeader(ClaimHeader claimHeader)
        {
            var claimNameInvolvements = claimHeader.ClaimInvolvements.Where(c => c.ClaimInvolvementType == (short)Xiap.Metadata.Data.Enums.StaticValues.LinkableComponentType.NameInvolvement).SelectMany(b => b.ClaimNameInvolvements).ToList();

            foreach (ClaimNameInvolvement claimNameInvolvement in claimNameInvolvements)
            {
                if (claimNameInvolvement.NameInvolvementType == (short)StaticValues.NameInvolvementType_ClaimNameInvolvement.MainClaimHandler &&
                        claimNameInvolvement.NameInvolvementMaintenanceStatus == (short)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest)
                {
                    // Only return the 'Latest' one. No other is current.
                    return claimNameInvolvement;
                }
            }

            return null;
        }


        /// <summary>
        /// Gets the Major Insured from claim header.
        /// </summary>
        /// <param name="claimHeader">The claim header.</param>
        /// <returns>Claim Name Involvement component, or null, if none found</returns>
        public static ClaimNameInvolvement GetInsuredFromHeader(ClaimHeader claimHeader)
        {
            if (claimHeader.ClaimInvolvements != null)
            {
                var claimNameInvolvements = claimHeader.ClaimInvolvements.Where(c => c.ClaimInvolvementType == (short)Xiap.Metadata.Data.Enums.StaticValues.LinkableComponentType.NameInvolvement).SelectMany(b => b.ClaimNameInvolvements).ToList();

                if (claimNameInvolvements != null)
                {
                    foreach (ClaimNameInvolvement claimNameInvolvement in claimNameInvolvements)
                    {
                        if (claimNameInvolvement.NameInvolvementType == (short)StaticValues.NameInvolvementType_ClaimNameInvolvement.MajorInsured &&
                                claimNameInvolvement.NameInvolvementMaintenanceStatus == (short)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest)
                        {
                            // Only return the 'Latest' one. No other is current.
                            return claimNameInvolvement;
                        }
                    }
                }
            }

            return null;
        }


        /// <summary>
        /// Gets the Major Insured from Claim Reference.
        /// </summary>
        /// <param name="claimReference">The claim reference.</param>
        /// <returns>Claim Name Involvement component, or null, if none found</returns>
        public static ClaimNameInvolvement GetInsuredfromClaim(string claimReference)
        {
            ArgumentCheck.ArgumentNullCheck(claimReference, "claimReference");
            ClaimNameInvolvement nameInv;
            nameInv = ObjectFactory.Resolve<IClaimsQuery>().GetMajorInsuredFromClaim(claimReference);

            return nameInv;
        }
        
        /// <summary>
        /// check whether user is main claim handler or not.
        /// </summary>
        /// <param name="userName">User Name</param>
        /// <param name="claimReference">Claim Reference</param>
        /// <returns>bool value</returns>
        public static bool IsUserMainClaimHandler(string userName, string claimReference)
        {
            bool retVal = false;

            ClaimNameInvolvement nameInv;

            nameInv = ObjectFactory.Resolve<IClaimsQuery>().GetMainClaimHandlerFromClaim(claimReference); // fetching the main claim handler from claim.

            if (nameInv != null)
            {
                if (nameInv.NameID.HasValue)
                {
                    string userIdentity;
                    long? userID;

                    // If we have a Name Involvement, use it to get the details and compare to the user passed in
                    bool isnameParsed = ClaimsBusinessLogicHelper.TryGetUserIdentityByName(nameInv.NameID.Value, out userIdentity, out userID); // getting the user identity of user.
                    if (isnameParsed)
                    {
                        if (userIdentity == userName)
                        {
                            retVal = true;
                        }
                    }
                }
            }

            return retVal;
        }

        /// <summary>
        /// Sets the DateofLossTypeCode on the Claim Header based on the Policy Section Detail of the coverage 
        /// attached to the given Claim Detail.
        /// </summary>
        /// <param name="pluginHelper">ClaimDetail to use to set DateofLoss</param>
        public static void SetDateOfLossType(PluginHelper<ClaimDetail> pluginHelper)
        {
            ClaimDetail claimDetail = pluginHelper.Component as ClaimDetail;
            ClaimHeader clmHeader = claimDetail.Parent as ClaimHeader;
            // Lazy means we only instantiate this boolean if we need to, due to code overheads.
            Lazy<bool> lazyCheckPaymentExists = new Lazy<bool>(() => ValidateIfPaymentsExist(pluginHelper, clmHeader));
            try
            {
                _Logger.Info("SetDateOfLossType Start.");
                if (claimDetail.PolicyCoverageID != null)
                {
                    // This claim detail is linked to a Policy Coverage so 
                    // cycle through all the coverages on the Policy this claim is attached to
                    if (clmHeader.UWHeader.ISections != null)
                    {
                        clmHeader.UWHeader.ISections.ForEach(section =>
                        {
                            section.ISectionDetails.ForEach(sectionDetail =>
                            {
                                sectionDetail.ICoverages.ForEach(coverage =>
                                {
                                    if (coverage.ExternalReference != null && claimDetail.UWCoverage != null && coverage.ExternalReference == claimDetail.UWCoverage.ExternalReference)
                                    {
                                        // If the claim detail is linked to the Policy coverage we are looking at the default the Date of Loss Type code from the Policy Section Detail.

                                        _Logger.Info("UWCoverage External Reference : " + claimDetail.UWCoverage.ExternalReference);
                                        string newDateOfLossTypeCode = sectionDetail.CustomCode03;

                                        // newDateOfLossTypeCode => "M" = Claims Made, "O" = Occurrence.
                                        // If there is no valid M or O value for Date of Loss on the Policy Section Detail, default in 'O'
                                        if (string.IsNullOrWhiteSpace(newDateOfLossTypeCode) || (newDateOfLossTypeCode != "M" && newDateOfLossTypeCode != "O"))
                                        {
                                            newDateOfLossTypeCode = "O";
                                        }

                                        // if the proposed Date Of Loss Type Code doesn't match the existing value on the claim header and
                                        // there are payments against the claim, return without changing the date of loss code
                                        if (newDateOfLossTypeCode != clmHeader.DateOfLossTypeCode)
                                        {
                                            if (!lazyCheckPaymentExists.Value)
                                            {
                                                return;
                                            }
                                        }

                                        // Update the claim header with the new date of loss code.
                                        clmHeader.DateOfLossTypeCode = newDateOfLossTypeCode;
                                    }
                                });
                            });
                        });
                    }
                }
            }
            catch (ObjectDisposedException ex)
            {
                _Logger.Debug(ex.Message);
            }
            finally
            {
                _Logger.Info("SetDateOfLossType End.");
            }
        }

        /// <summary>
        /// Validate if any payment exists on claim.
        /// Raises an error if a payment exists and blanks all policy links on the claim detail.
        /// </summary>
        /// <param name="pluginHelper">claim detail component</param>
        /// <param name="claimHeader">claim header component</param>
        /// <returns>bool value</returns>
        private static bool ValidateIfPaymentsExist(PluginHelper<ClaimDetail> pluginHelper, ClaimHeader claimHeader)
        {
            if (claimHeader.InProgressClaimTransactionHeaders.Any(transactionHeader => transactionHeader.ClaimTransactionSource == (short)StaticValues.ClaimTransactionSource.Payment) || claimHeader.HistoricalClaimTransactionHeaders.Any(transactionHeader => transactionHeader.ClaimTransactionSource == (short)StaticValues.ClaimTransactionSource.Payment))
            {
                ClaimsBusinessLogicHelper.AddError(pluginHelper.ProcessResults, ClaimConstants.PAYMENT_ENTERED_POLICY_TRIGGER_TYPE_CHANGE_NOT_ALLOWED, pluginHelper.InvocationPoint, pluginHelper.Component);
                ClaimDetail claimDetail = pluginHelper.Component as ClaimDetail;
                claimDetail.PolicyLinkLevel = null;
                claimDetail.PolicyCoverageID = null;
                claimDetail.PolicySectionDetailID = null;
                claimDetail.PolicySectionID = null;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Sets the Claim Detail Title on the claim detail of the Link component passed in.
        /// </summary>
        /// <param name="component">ClaimDetailToClaimInvolvementLink component</param>
        /// <param name="point">ProcessInvocationPoint point</param>
        public static void SetClaimDetailTitle(ClaimDetailToClaimInvolvementLink component, ProcessInvocationPoint point)
        {
            ClaimDetail claimDetail = component.ClaimDetail;
            ClaimInvolvement claimInvolvement = component.ClaimInvolvement;
            string title = claimDetail.ClaimDetailTypeCode;
            string name = null;

            // This change will only be called for linked claim name involvement of type Additional Claimant / Driver.
            // If so, append the List Name to the title.
            if (claimInvolvement.ClaimInvolvementType == (short)StaticValues.LinkableComponentType.NameInvolvement)
            {
                ClaimNameInvolvement ni = component.ClaimNameInvolvement;
                
                if (point != ProcessInvocationPoint.Delete && ni != null
                    && (ni.NameInvolvementMaintenanceStatus == (short?)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest && (ni.NameInvolvementType == (short)StaticValues.NameInvolvementType.AdditionalClaimant || ni.NameInvolvementType == (short)StaticValues.NameInvolvementType.Driver)))
                {
                    if (ni.NameID != null)
                    {
                        name = GetListName((long)ni.NameID);

                        if (string.IsNullOrEmpty(name) == false)
                        {
                            title += " - " + name;
                        }
                    }
                }
            }

            // If the title is longer than 50 characters, truncate to 50 characters
            if (!string.IsNullOrEmpty(title) && title.Length > 50)
            {
                title = title.Substring(0, 50);
            }

            claimDetail.ClaimDetailTitle = title;
        }

        /// <summary>
        /// Gets the List Name of the Company or Person.
        /// </summary>
        /// <param name="nameID">The name identifier.</param>
        /// <returns>List Name from Person or Company, or returns null</returns>
        public static string GetListName(long nameID)
        {
            Name name = ObjectFactory.Resolve<IInsuranceDirectoryQuery>().GetName(nameID);

            // Return the Company List Name or the Person List Name, as appropriate
            if (name.NameType == (short)StaticValues.NameType.Company)
            {
                return name.CompanyDetailVersions.First().ListName;
            }
            else if (name.NameType == (short)StaticValues.NameType.Person)
            {
                return name.PersonDetailVersions.First().ListName;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Validating claim header status code is not CON, if the claim has a Recovery or Litigation involvement.
        /// </summary>
        /// <param name="claimHeader">claim header component</param>
        /// <param name="componentName">string value</param>
        /// <returns>bool value</returns>
        public static bool ValidateClaimHeaderStatusCode(ClaimHeader claimHeader, string componentName)
        {
            bool isValid = true;
            // Check Claim Involvements for a link to a recovery or a litigation
            if (claimHeader.ClaimInvolvements.Any(ni => ni.ClaimInvolvementType == (short)StaticValues.LinkableComponentType.Litigation) || claimHeader.ClaimInvolvements.Any(ni => ni.ClaimInvolvementType == (short)StaticValues.LinkableComponentType.Recovery))
            {
                // Claim Header Status Code is only valid if the claim is not  "Opened Unconfirmed" (CON).
                isValid = claimHeader.ClaimHeaderStatusCode == ClaimConstants.CLAIM_STATUS_CLAIM_OPENED_UNCONFIRMED ? false : true;
            }

            return isValid;
        }

        /// <summary>
        /// Add message with severity "error" in process result collection. 
        /// This is called whenever we want to raise an error message.
        /// </summary>
        /// <param name="processResults">Process Results Collection</param>
        /// <param name="errorCode">Error Code</param>
        /// <param name="invocationPoint">invocation point</param>
        /// <param name="component">business component</param>
        /// <param name="args">object array of parameters</param>
        /// <returns>Process Results Collection</returns>
        public static ProcessResultsCollection AddError(ProcessResultsCollection processResults, string errorCode, ProcessInvocationPoint invocationPoint, IBusinessComponent component, params object[] args)
        {
            ProcessResult vr = new ProcessResult(component, null, ErrorSeverity.Error, errorCode, args);
            processResults.Add(vr.Key, invocationPoint, vr);

            return processResults;
        }

        /// <summary>
        /// Add an error message with severity "Fatal" in process result collection.
        /// </summary>
        /// <param name="processResults">Process Results Collection</param>
        /// <param name="errorCode">Error Code</param>
        /// <param name="invocationPoint">invocation point</param>
        /// <param name="component">business component</param>
        /// <param name="args">object array of parameters</param>
        /// <returns>Process Results Collection</returns>
        public static ProcessResultsCollection AddFatalError(ProcessResultsCollection processResults, string errorCode, ProcessInvocationPoint invocationPoint, IBusinessComponent component, params object[] args)
        {
            ProcessResult vr = new ProcessResult(component, null, ErrorSeverity.Fatal, errorCode, args);
            processResults.Add(vr.Key, invocationPoint, vr);

            return processResults;
        }

        /// <summary>
        /// Add an error message with severity "Fatal" in process result collection of *transaction* type.
        /// </summary>
        /// <param name="processResults">Process Results Collection</param>
        /// <param name="errorCode">string value</param>
        /// <param name="invocationPoint">Transaction invocation point</param>
        /// <param name="component">business component</param>
        /// <param name="args">object array of parameters</param>
        /// <returns>Process Results Collection</returns>
        public static ProcessResultsCollection AddFatalError(ProcessResultsCollection processResults, string errorCode, TransactionInvocationPoint invocationPoint, IBusinessComponent component, params object[] args)
        {
            ProcessResult vr = new ProcessResult(component, null, ErrorSeverity.Fatal, errorCode, args);
            processResults.Add(vr.Key, invocationPoint, vr);

            return processResults;
        }

        /// <summary>
        /// check whether more than one notifier flag exists or not on the Claim Name Involvements.
        /// Doesn't care if they are current or not.
        /// </summary>
        /// <param name="claimHeader">Claim Header component</param>
        /// <returns>true if there is more than one</returns>
        public static bool IsNotiferFlagFoundMoreThanOne(ClaimHeader claimHeader)
        {
            if (ClaimNameInvolvementNotifierFlagCount(claimHeader,false) > 1)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Check if there are any *current* notifier flags on the Claim Name Involvements.
        /// </summary>
        /// <param name="claimHeader">Claim Header component</param>
        /// <returns>true if there is at least one notifier flag</returns>
        public static bool IsNotiferFlagNotFound(ClaimHeader claimHeader)
        {
            if (claimHeader.ClaimHeaderStatusThreshold >= 15)
            {
                if (ClaimNameInvolvementNotifierFlagCount(claimHeader,true) == 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the claim name involvement notifier flag count. 
        /// Returns a count of only current notifiers if 'isLatest' is set to true
        /// </summary>
        /// <param name="claimHeader">Claim Header component</param>
        /// <param name="isLatest">Is Latest?</param>
        /// <returns>Number of NI notifier flags on the claim</returns>
        private static int ClaimNameInvolvementNotifierFlagCount(ClaimHeader claimHeader, bool isLatest)
        {
            if (claimHeader.ClaimInvolvements == null)
            {
                return 0;
            }

            if (isLatest == false)
            {
                // Don't care if they are current or not.
                return claimHeader.ClaimInvolvements.Where(c => c.ClaimInvolvementType == (short)StaticValues.LinkableComponentType.NameInvolvement).SelectMany(b => b.ClaimNameInvolvements).ToList().Where(c => c.CustomBoolean01 == true).ToList().Count; // c.CustomBoolean01 = Notifier
            }

            // Only return a count of current flags
            return claimHeader.ClaimInvolvements.Where(c => c.ClaimInvolvementType == (short)StaticValues.LinkableComponentType.NameInvolvement).SelectMany(b => b.ClaimNameInvolvements).ToList().Where(c => c.CustomBoolean01 == true && c.NameInvolvementMaintenanceStatus == (short?)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest).ToList().Count; // c.CustomBoolean01 = notifier
        }

        /// <summary>
        /// Gets the date difference in Years
        /// </summary>
        /// <param name="myDate">Extension Class</param>
        /// <param name="start">Older Date</param>
        /// <param name="end">New Date</param>
        /// <returns>Number of Years</returns>
        public static double Years(this DateTime myDate, DateTime start, DateTime end)
        {
            // Get difference in total months.         
            int months = ((end.Year - start.Year) * 12) + (end.Month - start.Month);
            // substract 1 month if end month is not completed         
            if (end.Day < start.Day)
            {
                months--;
            }

            double totalyears = months / 12d;
            return totalyears;
        }

        /// <summary>
        /// Check whether policy the claim is attached to is verified or not.
        /// </summary>
        /// <param name="claimHeader">claim header component</param>
        /// <returns>true if policy is verified</returns>
        public static bool IsPolicyVerified(ClaimHeader claimHeader)
        {
            // Get the verified policy header status from the application configuration
            var verifiedHeaderStatus = ClaimsBusinessLogicHelper.ResolveMandatoryConfig<string>("PolicyVerifiedHeaderStatus");
            // NB At the point of coverage verification, the PolicyHeaderID is not populated, need to use Header Ref

            IAXAClaimsQuery query = ObjectFactory.Resolve<IAXAClaimsQuery>();
            if (!string.IsNullOrEmpty(claimHeader.UWHeader.HeaderReference) && query.GetHeaderStatus(claimHeader.UWHeader.HeaderReference) == verifiedHeaderStatus)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Checks whether there exists ClaimTransactionHeaders where ReserveAuthorisationStatus = ReserveUnauthorised
        /// </summary>
        /// <param name="claimHeader">Claim Header</param>
        /// <returns>Returns true if any Unauthorised Reserve</returns>
        public static bool HasUnauthorisedReserveAndPayment(ClaimHeader claimHeader)
        {
            return claimHeader.InProgressClaimTransactionHeaders.Any(a => ((a.ClaimTransactionSource == (short?)StaticValues.ClaimTransactionSource.Reserve) || (a.ClaimTransactionSource == (short?)StaticValues.ClaimTransactionSource.Payment)) && a.ReserveAuthorisationStatus == (short?)StaticValues.ReserveAuthorisationStatus.ReserveUnauthorised)
                                         ||
                  claimHeader.HistoricalClaimTransactionHeaders.Any(a => ((a.ClaimTransactionSource == (short?)StaticValues.ClaimTransactionSource.Reserve) || (a.ClaimTransactionSource == (short?)StaticValues.ClaimTransactionSource.Payment)) && a.ReserveAuthorisationStatus == (short?)StaticValues.ReserveAuthorisationStatus.ReserveUnauthorised);
        }

        /// <summary>
        /// Checks whether there exists ClaimTransactionHeaders where RecoveryReceiptAuthorisationStatus = RecoveryReceiptUnauthorised
        /// </summary>
        /// <param name="claimHeader">Claim Header</param>
        /// <returns>Returns true if any Unauthorised RecoveryReceipt</returns>
        public static bool HasUnauthorisedRecoveryReceipt(ClaimHeader claimHeader)
        {
            return claimHeader.InProgressClaimTransactionHeaders.Any(a => a.ClaimTransactionSource == (short?)StaticValues.ClaimTransactionSource.RecoveryReceipt && a.RecoveryReceiptAuthorisationStatus == (short?)StaticValues.RecoveryReceiptAuthorisationStatus.RecoveryReceiptUnauthorised)
                                         ||
                  claimHeader.HistoricalClaimTransactionHeaders.Any(a => a.ClaimTransactionSource == (short?)StaticValues.ClaimTransactionSource.RecoveryReceipt && a.RecoveryReceiptAuthorisationStatus == (short?)StaticValues.RecoveryReceiptAuthorisationStatus.RecoveryReceiptUnauthorised);
        }

        /// <summary>
        /// Checks whether there exists ClaimTransactionHeaders where ReserveAuthorisationStatus = ReserveUnauthorised
        /// </summary>
        /// <param name="claimHeader">Claim Header</param>
        /// <returns>Returns true if any Unauthorised Reserve</returns>
        public static bool HasUnauthorisedReserve(ClaimHeader claimHeader)
        {
            return claimHeader.InProgressClaimTransactionHeaders.Any(a => a.ClaimTransactionSource == (short?)StaticValues.ClaimTransactionSource.Reserve && a.ReserveAuthorisationStatus == (short?)StaticValues.ReserveAuthorisationStatus.ReserveUnauthorised)
                                         ||
                  claimHeader.HistoricalClaimTransactionHeaders.Any(a => a.ClaimTransactionSource == (short?)StaticValues.ClaimTransactionSource.Reserve && a.ReserveAuthorisationStatus == (short?)StaticValues.ReserveAuthorisationStatus.ReserveUnauthorised);
        }

        /// <summary>
        /// Checks whether there exists ClaimTransactionHeaders where ReserveAuthorisationStatus = Paymentunauthorised
        /// </summary>
        /// <param name="claimHeader">Claim Header</param>
        /// <returns>Returns true if any Unauthorised Payment</returns>
        public static bool HasUnauthorisedPayment(ClaimHeader claimHeader)
        {
            return claimHeader.InProgressClaimTransactionHeaders.Any(a => a.ClaimTransactionSource == (short?)StaticValues.ClaimTransactionSource.Payment && a.PaymentAuthorisationStatus == (short?)StaticValues.PaymentAuthorisationStatus.PaymentUnauthorised)
                                         ||
                  claimHeader.HistoricalClaimTransactionHeaders.Any(a => a.ClaimTransactionSource == (short?)StaticValues.ClaimTransactionSource.Payment && a.PaymentAuthorisationStatus == (short?)StaticValues.PaymentAuthorisationStatus.PaymentUnauthorised);
        }


        /// <summary>
        /// Tries to get the type of the reserve movement and puts it in the output parameter
        /// </summary>
        /// <param name="recoveryReservemovementTypeCode">The recovery reserve movement type code.</param>
        /// <param name="productCode">The product code.</param>
        /// <param name="reserveMovementType">output Type of the reserve movement.</param>
        /// <returns>true if a reserve movement type is found, false otherwise</returns>
        public static bool TryGetReserveMovementType(string recoveryReservemovementTypeCode, string productCode, out string reserveMovementType)
        {
            ArgumentCheck.ArgumentNullOrEmptyCheck(recoveryReservemovementTypeCode, "recoveryReservemovementTypeCode");
            ArgumentCheck.ArgumentNullOrEmptyCheck(productCode, "productCode");

            var helper = ObjectFactory.Resolve<IDecisionTableHelper>();
            // Check the AXARECMVMT decision table for the given recovery reserve moment type code and Product
            var component = helper.Call("AXARECMVMT", DateTimeUtils.GetDateTimeToday(), recoveryReservemovementTypeCode, productCode);
            if (component != null && component.Action1 != null)
            {
                // if we have a value, pass it back out in the output parameter and return true
                reserveMovementType = Convert.ToString(component.Action1);
                return true;
            }

            reserveMovementType = null;
            return false;
        }

        /// <summary>
        /// Check whether ClaimDetailToComponentLinkExist in Product.
        /// </summary>
        /// <param name = "productClaimDetail">product Claim Detail </param>
        /// <param name = "involvementLinkableComponentID">involvement Linkable ComponentID</param>
        /// <returns><see cref="Bool"/>True if component link exists</returns>
        public static bool CheckProductClaimDetailToComponentLinkExist(ProductClaimDetail productClaimDetail, long involvementLinkableComponentID)
        {
            if (productClaimDetail.ProductClaimDetailToComponentLinks != null)
            {
                return productClaimDetail.ProductClaimDetailToComponentLinks.Any(c => c != null && c.ProductLinkableComponent != null && c.ProductLinkableComponent.ProductLinkableComponentID == involvementLinkableComponentID);
            }

            return false;
        }

        /// <summary>
        /// Check whether ClaimHeaderStatusCode is valid to transfer by seeing if it's in the supplied list of invalid claim header status codes.
        /// </summary>
        /// <param name="claimHeader">Claim Header</param>
        /// <param name="invalidClaimHeaderStatusCodes">Invalid ClaimHeaderStatusCodes</param>
        /// <returns>True if claimheader status code is valid for transfer</returns>
        public static bool CheckValidHeaderStatus(ClaimHeader claimHeader, string invalidClaimHeaderStatusCodes)
        {
            bool canTransfer = true;
            if (!string.IsNullOrEmpty(invalidClaimHeaderStatusCodes))
            {
                List<string> ClaimHeaderStatusesList = invalidClaimHeaderStatusCodes.Split(',').ToList<string>();
                if (ClaimHeaderStatusesList.Any(status => status.Trim().Equals(claimHeader.ClaimHeaderStatusCode)))
                {
                    canTransfer = false;
                }
            }

            return canTransfer;
        }

        /// <summary>
        /// Checks Whether Claim is migrated claim and is closed claim.
        /// </summary>
        /// <param name="header">Claim Header</param>
        /// <returns>bool value</returns>
        public static bool CheckMigratedCloseClaim(ClaimHeader header)
        {
            // UI Label = Data Source &&
            // UI Label = CMS Migration Status && 
            // UI Label = CMS Migration Status && 
            // UI Label = CMS Migration Status
            if (header.CustomCode19 == ClaimConstants.CLAIMS_MIGRATION_STATUS   
                                && (string.IsNullOrWhiteSpace(header.CustomCode18) || header.CustomCode18 == ClaimConstants.FAILED_POLICY_DOES_NOT_EXIST_IN_GENIUSX   
                                        || header.CustomCode18 == ClaimConstants.FAILED_POLICY_EXISTS_BUT_COULD_NOT_ATTACH_TO_CLAIM   
                                        || header.CustomCode18 == ClaimConstants.FAILED_DUE_TO_INTERNAL_SERVICE_CONNECTION_ISSUES)   
                                 && (header.ClaimHeaderStatusCode == ClaimConstants.CLOSED_STATUS || header.ClaimHeaderStatusCode == ClaimConstants.CLOSED_STATUS_REPORT_ONLY))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Validate start and End Date of ClaimHeader against the Policy component the claim Detail is attached to.
        /// </summary>
        /// <param name="pluginHelper">ClaimDetail  type PluginHelper</param>
        public static void ValidateStartAndEndDate(PluginHelper<ClaimDetail> pluginHelper)
        {
            ClaimHeader claimHeader = pluginHelper.Component.Parent as ClaimHeader;
            ClaimDetail claimDetail = pluginHelper.Component as ClaimDetail;
            IUWSection uwSection = null;
            IUWSectionDetail uwSectionDetail = null;
            IUWCoverage uwCoverage = null;

            // Check we have a Policy Link specified on the Claim Detail
            if (claimDetail.PolicyLinkLevel != null)
            {
                if (claimDetail.PolicySectionID != null)
                {
                    // Claim detail attached to Policy Section
                    uwSection = claimDetail.UWSection;
                    if (uwSection != null && (claimHeader.DateOfLossFrom < uwSection.SectionStartDate || claimHeader.DateOfLossFrom > uwSection.SectionEndDate))
                    {
                        // Raise error as Claim Header outside Policy Section dates
                        ClaimsBusinessLogicHelper.AddError(pluginHelper.ProcessResults, ClaimConstants.DATE_OUTSIDE_THE_COVER_PERIOD, pluginHelper.InvocationPoint, pluginHelper.Component, claimHeader.DateOfLossFrom, uwSection.SectionStartDate, uwSection.SectionEndDate);
                        claimDetail.PolicySectionID = null;
                        claimDetail.PolicyLinkLevel = null;
                        claimHeader.PolicyHeaderID = null;
                        claimHeader.UWHeader = null;
                        return;
                    }
                }
                else if (claimDetail.PolicySectionDetailID != null)
                {
                    // Claim detail attached to Policy Section Detail
                    uwSectionDetail = claimDetail.UWSectionDetail;
                    if (uwSectionDetail != null && (claimHeader.DateOfLossFrom < uwSectionDetail.SectionDetailStartDate || claimHeader.DateOfLossFrom > uwSectionDetail.SectionDetailEndDate))
                    {
                        // Raise error as Claim Header dates outside Policy Section Detail dates
                        ClaimsBusinessLogicHelper.AddError(pluginHelper.ProcessResults, ClaimConstants.DATE_OUTSIDE_THE_COVER_PERIOD, pluginHelper.InvocationPoint, pluginHelper.Component, claimHeader.DateOfLossFrom, uwSectionDetail.SectionDetailStartDate, uwSectionDetail.SectionDetailStartDate);
                        claimDetail.PolicySectionDetailID = null;
                        claimDetail.PolicySectionID = null;
                        claimDetail.PolicyLinkLevel = null;
                        claimHeader.PolicyHeaderID = null;
                        claimHeader.UWHeader = null;
                        return;
                    }
                }
                else if (claimDetail.PolicyCoverageID != null)
                {
                    // Claim detail attached to Policy Coverage
                    uwCoverage = claimDetail.UWCoverage;
                    if (uwCoverage != null && (claimHeader.DateOfLossFrom < uwCoverage.CoverageStartDate || claimHeader.DateOfLossFrom > uwCoverage.CoverageEndDate))
                    {
                        // Raise error since Claim Header dates are outside of Policy Coverage dates
                        ClaimsBusinessLogicHelper.AddError(pluginHelper.ProcessResults, ClaimConstants.DATE_OUTSIDE_THE_COVER_PERIOD, pluginHelper.InvocationPoint, pluginHelper.Component, claimHeader.DateOfLossFrom, uwCoverage.CoverageStartDate, uwCoverage.CoverageEndDate);
                        claimDetail.PolicyCoverageID = null;
                        claimDetail.PolicyLinkLevel = null;
                        claimHeader.PolicyHeaderID = null;
                        claimDetail.PolicySectionID = null;
                        claimDetail.PolicySectionDetailID = null;
                        claimHeader.UWHeader = null;
                        return;
                    }
                }

                ClaimsBusinessLogicHelper.SetDateOfLossType(pluginHelper);
            }
        }

        /// <summary>
        /// Validates that this Claim Detail is attached to the same Policy Component as existing Claim Details
        /// that are attached to the same Policy Component level, e.g. Section compared to Section, or SD compared to SD, etc.
        /// </summary>
        /// <param name="pluginHelper">ClaimDetail type PluginHelper</param>
        public static void ValidateAgainstExistingClaimDetails(PluginHelper<ClaimDetail> pluginHelper)
        {
            ClaimDetail claimDetail = pluginHelper.Component as ClaimDetail;
            ClaimHeader clmHeader = claimDetail.ClaimHeader;

            // Go through each claim detail on the Claim Header in turn.
            clmHeader.ClaimDetails.ForEach(cd =>
            {
                if (cd.PolicyLinkLevel != null && claimDetail.PolicyLinkLevel != null)
                {
                    // Assuming the Claim Detail we are validating and the claim detail on the Claim Header are both attached to the policy
                    // we compare like policy linkings to make sure they are the same
                    if ((cd.PolicyCoverageID.HasValue && claimDetail.PolicyCoverageID.HasValue && cd.PolicyCoverageID != claimDetail.PolicyCoverageID)
                        || (cd.PolicySectionID.HasValue && claimDetail.PolicySectionID.HasValue && cd.PolicySectionID != claimDetail.PolicySectionID)
                        || (cd.PolicySectionDetailID.HasValue && claimDetail.PolicySectionDetailID.HasValue && cd.PolicySectionDetailID != claimDetail.PolicySectionDetailID))
                    {
                        // An existing Claim Detail is already attached at this Policy level (Section, Section Detail, Coverage) but to a different 
                        // component, so raise an error.
                        claimDetail.PolicyLinkLevel = null;
                        claimDetail.PolicySectionID = null;
                        claimDetail.PolicySectionDetailID = null;
                        claimDetail.PolicyCoverageID = null;
                        ClaimsBusinessLogicHelper.AddError(pluginHelper.ProcessResults, ClaimConstants.COVERAGE_IS_ALREADY_ATTACHED, pluginHelper.InvocationPoint, pluginHelper.Component);
                    }
                }
            });
        }
    }
}