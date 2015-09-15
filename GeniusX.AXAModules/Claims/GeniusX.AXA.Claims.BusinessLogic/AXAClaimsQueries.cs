using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Objects;
using System.Data.SqlClient;
using System.Linq;
using Xiap.Claims.BusinessComponent;
using Xiap.Claims.BusinessLogic.AuthorityCheck;
using Xiap.Framework;
using Xiap.Framework.Common;
using Xiap.Framework.Data.Claims;
using Xiap.Framework.Data.Underwriting;
using Xiap.Framework.Entity;
using Xiap.Framework.Logging;
using Xiap.Framework.Metadata;
using Xiap.Framework.Utils;
using Xiap.Metadata.BusinessComponent;
using Xiap.Metadata.Data;
using Xiap.Metadata.Data.Enums;
using Xiap.UW.BusinessComponent;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    public class AXAClaimsQueries : IAXAClaimsQuery
    {
        private static readonly ILogger _Logger = LoggerFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static Func<UnderwritingEntities, long, IQueryable<Header>>
            getHeaderByHeaderID = CompiledQuery.Compile<UnderwritingEntities, long, IQueryable<Header>>((uwEntities, headerID) =>
          (from row in uwEntities.Header
                         .Include("HeaderVersion")
                         .Include("UwNameInvolvement")
           where row.HeaderID == headerID
           select row));

        /// <summary>
        /// Return the connection strings section from the application configuration.
        /// </summary>
        private string ConnectionString
        {
            get { return ConfigurationManager.ConnectionStrings["Config"].ConnectionString; }
        }

        /// <summary>
        /// Returns the Header component on the basis of HeaderId
        /// </summary>
        /// <param name="headerID">Header ID</param>
        /// <returns>Header Component, otherwise returns null</returns>
        public Header GetPolicyHeaderByHeaderID(long headerID)
        {
            UnderwritingEntities uwEntities = UnderwritingEntitiesFactory.GetUnderwritingEntities();

            IQueryable<Header> header = getHeaderByHeaderID.Invoke(uwEntities, headerID);
            if (header != null && header.Count() > 0)
            {
                return header.First();
            }

            return null;
        }

        /// <summary>
        /// Returns the Header Status of a particular Policy on the basis of Header Reference.
        /// </summary>
        /// <param name="headerReference">Policy Header Reference</param>
        /// <returns>Header Status Code or null</returns>
        public string GetHeaderStatus(string headerReference)
        {
            using (UnderwritingEntities entities = UnderwritingEntitiesFactory.GetUnderwritingEntities())
            {
                var result = (from row in entities.Header
                              where row.HeaderReference == headerReference
                              select row.HeaderStatusCode).FirstOrDefault();

                return result;
            }
        }


        /// <summary>
        /// Gets the Deductible Policy References from the given Policy Header Reference
        /// </summary>
        /// <param name="headerReference">The header reference.</param>
        /// <returns>Array of policy references</returns>
        public string[] GetPolicyReferences(string headerReference)
        {
            string[] policyRefs = new string[5];
            using (UnderwritingEntities entities = UnderwritingEntitiesFactory.GetUnderwritingEntities())
            {
                var header = entities.Header.Where(a => a.HeaderReference == headerReference).FirstOrDefault();
                if (header != null)
                {
                    HeaderVersion headerVersion = (HeaderVersion)header.GetLatestVersion();
                    policyRefs[0] = headerVersion.CustomReference01;
                    policyRefs[1] = headerVersion.CustomReference02;
                    policyRefs[2] = headerVersion.CustomReference03;
                    policyRefs[3] = headerVersion.CustomReference04;
                    policyRefs[4] = headerVersion.CustomReference05;
                }

                return policyRefs;
            }
        }

        /// <summary>
        /// This method returns the HeaderId on the basis of external reference of Policy.
        /// </summary>
        /// <param name="externalReference">External Reference of Policy</param>
        /// <returns>Header Id or 0 if no header found</returns>
        public long GetHeaderID(string externalReference)
        {
            using (UnderwritingEntities entities = UnderwritingEntitiesFactory.GetUnderwritingEntities())
            {
                Header header = (from row in entities.Header where row.ExternalReference == externalReference select row).FirstOrDefault();
                if (header != null)
                {
                    return header.HeaderID;
                }
            }

            return 0;
        }

        /// <summary>
        /// This method returns the SectionID on the basis of external reference of Policy Section.
        /// </summary>
        /// <param name="externalReference">External Reference of Section</param>
        /// <returns>Section ID or 0 if no Section found</returns>
        public long GetSectionID(string externalReference)
        {
            using (UnderwritingEntities entities = UnderwritingEntitiesFactory.GetUnderwritingEntities())
            {
                Section section = (from row in entities.Section where row.ExternalReference == externalReference select row).FirstOrDefault();
                if (section != null)
                {
                    return section.SectionID;
                }
            }

            return 0;
        }

        /// <summary>
        /// This method returns the SectionDetailID on the basis of external reference of Policy Section Detail.
        /// </summary>
        /// <param name="externalReference">External Reference of SectionDetail</param>
        /// <returns>Section Detail ID or 0 if no Section Detail found</returns>
        public long GetSectionDetailID(string externalReference)
        {
            using (UnderwritingEntities entities = UnderwritingEntitiesFactory.GetUnderwritingEntities())
            {
                SectionDetail sectionDetail = (from row in entities.SectionDetail where row.ExternalReference == externalReference select row).FirstOrDefault();
                if (sectionDetail != null)
                {
                    return sectionDetail.SectionDetailID;
                }
            }

            return 0;
        }

        /// <summary>
        /// This method returns the CoverageID on the basis of external reference of Policy Coverage.
        /// </summary>
        /// <param name="externalReference">External Reference of CoverageID</param>
        /// <returns>Coverage ID or 0 if no Coverage found</returns>
        public long GetCoverageID(string externalReference)
        {
            using (UnderwritingEntities entities = UnderwritingEntitiesFactory.GetUnderwritingEntities())
            {
                Coverage coverage = (from row in entities.Coverage where row.ExternalReference == externalReference select row).FirstOrDefault();
                if (coverage != null)
                {
                    return coverage.CoverageID;
                }
            }

            return 0;
        }

        /// <summary>
        /// This method checks whether deductibles or excess exists on policy and returns the boolean result 
        /// in output boolean parameters.
        /// A stored procedure is used to ascertain the detail.
        /// </summary>
        /// <param name="policyHeaderID">Policy Header ID</param>
        /// <param name="identifier1">Generic Data Type Code for Deductibles</param>
        /// <param name="identifier2">Generic Data Type Code for Excess</param>
        /// <param name="deductibleExist">output Boolean value DeductibleExist</param>
        /// <param name="EDExcessExist">output Boolean value EDExcessExist</param>
        public void PolicyDeductiblesExist(long policyHeaderID, string identifier1, string identifier2, out bool deductibleExist, out bool EDExcessExist)
        {
            using (new PerfLogger(typeof(AXAClaimsQueries), "PolicyDeductiblesExist"))
            {
                deductibleExist = false;
                EDExcessExist = false;
                // Connect to the database.
                using (SqlConnection sqlConnection = new SqlConnection(this.ConnectionString))
                {
                    using (SqlCommand sqlCommand = new SqlCommand())
                    {
                        // Set up the call to the stored procedure [Claims].[PolicyDeductiblesExist] which takes three parameters
                        sqlConnection.Open();
                        sqlCommand.Connection = sqlConnection;
                        sqlCommand.CommandType = CommandType.StoredProcedure;
                        sqlCommand.CommandText = "[Claims].[PolicyDeductiblesExist]";
                        sqlCommand.Parameters.AddWithValue("@policyHeaderID", policyHeaderID);
                        sqlCommand.Parameters.AddWithValue("@identifier1", identifier1);
                        sqlCommand.Parameters.AddWithValue("@identifier2", identifier2);

                        try
                        {
                            using (SqlDataReader dataReader = sqlCommand.ExecuteReader())
                            {
                                // If no records were found, return.
                                if (dataReader == null || !dataReader.HasRows)
                                {
                                    return;
                                }

                                // For each record, check if it's a Deductible or an Excess record
                                while (dataReader.Read())
                                {
                                    if (dataReader["GenericDataTypeCode"].ToString() == identifier1)
                                    {
                                        deductibleExist = true;
                                    }
                                    else if (dataReader["GenericDataTypeCode"].ToString() == identifier2)
                                    {
                                        EDExcessExist = true;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Method is used to call a stored procedure which takes ClaimReference as a parameter and adds the entries in ClaimTransferControlLog table.
        /// </summary>
        /// <param name="claimReference">Claim Reference</param>
        public void ExecuteClaimTransferControlLogSP(string claimReference)
        {
            var parameters = new[] 
            {
                SqlUtils.CreateSqlParameter("@claimReference",SqlDbType.NVarChar, claimReference)
            };

            SqlUtils.ExecuteStoredProcedure(
               this.ConnectionString,
               ClaimsBusinessLogicHelper.ResolveMandatoryConfig<string>("AXAClaimTransferHeaderToClaimTransferControlLogSP"),
               parameters,
               _ => { });
        }

        /// <summary>
        /// This method returns the collection of deductibles (generic data items) on the basis of policy link level.
        /// </summary>
        /// <param name="componentID">Component ID (Component can be Header, Section, SectionDetail or Coverage)</param>
        /// <param name="linkLevel">Policy Link Level</param>
        /// <param name="vehicleType">Vehicle Type</param>
        /// <param name="division">Division Code</param>
        /// <param name="reasonCode">Reason Code</param>
        /// <param name="identifier">Generic Data Type Code</param>
        /// <returns>Collection of Generic Data items</returns>
        public IEnumerable<IGenericDataItem> GetPolicyDeductibles(long componentID, short linkLevel, string vehicleType, string division, string reasonCode, string identifier)
        {
            switch (linkLevel)
            {
                case (short)StaticValues.PolicyLinkLevel.Header:
                    return this.GetHeaderDeductibles(componentID, vehicleType, division, reasonCode, identifier);
                case (short)StaticValues.PolicyLinkLevel.Section:
                    return this.GetSectionDeductibles(componentID, vehicleType, division, reasonCode, identifier);
                case (short)StaticValues.PolicyLinkLevel.SectionDetail:
                    return this.GetSectionDetailDeductibles(componentID, vehicleType, division, reasonCode, identifier);
                case (short)StaticValues.PolicyLinkLevel.Coverage:
                    return this.GetCoverageDeductibles(componentID, vehicleType, division, reasonCode, identifier);
                default:
                    return null;
            }
        }

        /// <summary>
        /// Returns the reason codes of Policy on the basis of Header Reference and Generic Data Type Code
        /// via a system parameter.
        /// </summary>
        /// <param name="headerReference">Policy Header Reference</param>
        /// <param name="identifier">Generic Data Type Code</param>
        /// <returns>Reason Codes collection</returns>
        public Dictionary<string, List<CodeRow>> GetPolicyReasonCodes(string headerReference, string identifier)
        {
            using (new PerfLogger(typeof(AXAClaimsQueries), "GetPolicyReasonCodes"))
            {
                Dictionary<string, List<CodeRow>> allReasonCodeList = new Dictionary<string, List<CodeRow>>();
                Dictionary<string, List<string>> reasonCodes = new Dictionary<string, List<string>>();

                // Connect to the database.
                using (SqlConnection sqlConnection = new SqlConnection(this.ConnectionString))
                {
                    using (SqlCommand sqlCommand = new SqlCommand())
                    {
                        // Build up the call to the stored procedure [Claims].[GetPolicyReasonCodes]
                        sqlConnection.Open();
                        sqlCommand.Connection = sqlConnection;
                        sqlCommand.CommandType = CommandType.StoredProcedure;
                        sqlCommand.CommandText = "[Claims].[GetPolicyReasonCodes]";
                        sqlCommand.Parameters.AddWithValue("@policyHeaderReference", headerReference);
                        sqlCommand.Parameters.AddWithValue("@identifier", identifier);

                        try
                        {
                            using (SqlDataReader dataReader = sqlCommand.ExecuteReader())
                            {
                                // Return the empty Dictionary of reason codes if nothing is returned from the call.
                                if (dataReader == null || !dataReader.HasRows)
                                {
                                    return allReasonCodeList;
                                }

                                // For each record, add an entry in the ReasonCodes list, key: External Reference, value: Custom Code 03
                                while (dataReader.Read())
                                {
                                    this.AddReasonCode(reasonCodes, dataReader["ExternalReference"].ToString(), dataReader["CustomCode03"].ToString());
                                }
                            }

                            if (reasonCodes != null && reasonCodes.Count() > 0)
                            {
                                // For each reasonCode, add to the allReasonCodeList
                                foreach (var detail in reasonCodes)
                                {
                                    this.BuildAllReasonCodeList(allReasonCodeList, detail.Key, detail.Value);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }

                        return allReasonCodeList;
                    }
                }
            }
        }

        /// <summary>
        /// This method Adds the reason code in the collection of ReasonCodes passed into the method
        /// </summary>
        /// <param name="reasonCodes">Reason Codes</param>
        /// <param name="key">Key of the dicitionary</param>
        /// <param name="value">Value of the Dictionary</param>
        private void AddReasonCode(Dictionary<string, List<string>> reasonCodes, string key, string value)
        {
            // If this key already exists, add the new reason code to the value on that key
            if (reasonCodes.ContainsKey(key))
            {
                List<string> currentvalue = reasonCodes[key];
                currentvalue.Add(value);
                reasonCodes[key] = currentvalue;
            }
            else
            {
                var list = new List<string>();
                list.Add(value);
                reasonCodes.Add(key, list);
            }
        }

        /// <summary>
        /// This method executes the stored procedure [Claims].[AXAExecuteAccumulatedFilteredClaimAmounts] 
        /// that calculates the total amounts for a claim detail, for a given amount type, over all ClaimTransactionHeaders
        /// with an ID equal to or less than the supplied filter value.
        /// </summary>
        /// <param name="claimDetailId">Claim Detail Id</param>
        /// <param name="claimTransactionHeaderId">Claim Transaction Header Id</param>
        /// <param name="amountType">Amount Type(Reserve, Payment, Recovery Reserve or RecoveryReceipt)</param>
        /// <returns>List of ClaimFinancialAmount</returns>
        public List<ClaimFinancialAmount> ExecuteAccumulatedClaimAmounts(long claimDetailId, long claimTransactionHeaderId, StaticValues.AmountType amountType)
        {
            // Parameters for the Stored Procedure
            var parameters = new[] 
            {
                SqlUtils.CreateSqlParameter("@claimDetailID", SqlDbType.BigInt, claimDetailId),
                SqlUtils.CreateSqlParameter("@claimTransactionHeaderID", SqlDbType.BigInt, claimTransactionHeaderId),
                SqlUtils.CreateSqlParameter("@amountType", SqlDbType.SmallInt, (short)amountType)
            };

            // Create a new Financial Amounts list to store the data from the Stored Procedure
            List<ClaimFinancialAmount> claimFinancialAmounts = new List<ClaimFinancialAmount>();
            // Call the stored procedure, reading each row retrieved in turn
            SqlUtils.ExecuteStoredProcedure(
                this.ConnectionString,
                "[Claims].[AXAExecuteAccumulatedFilteredClaimAmounts]",
                parameters,
                reader =>
                {
                    // Create a new ClaimFinancialAmount item from the database.
                    var claimFinancialAmount = new ClaimFinancialAmount
                    {
                        ClaimDetailReference = Convert.ToString(reader[HistoricalFinancialAmountDataSql.LatestClaimAmounts.ClaimDetailReference]),
                        MovementType = Convert.ToString(reader[HistoricalFinancialAmountDataSql.AccumulatedClaimAmounts.MovementType]),
                        AccountingCurrencyCode = Convert.ToString(reader[HistoricalFinancialAmountDataSql.AccumulatedClaimAmounts.AccountingCurrencyCode]),
                        OriginalCurrencyCode = Convert.ToString(reader[HistoricalFinancialAmountDataSql.AccumulatedClaimAmounts.OriginalCurrencyCode]),
                        ClaimCurrencyCode = Convert.ToString(reader[HistoricalFinancialAmountDataSql.LatestClaimAmounts.ClaimCurrencyCode]),
                        TransactionAmountAccounting = DbNullableConvert(Convert.ToDecimal, reader[HistoricalFinancialAmountDataSql.AccumulatedClaimAmounts.TransactionAmountAccounting]),
                        TransactionAmountBase = DbNullableConvert(Convert.ToDecimal, reader[HistoricalFinancialAmountDataSql.AccumulatedClaimAmounts.TransactionAmountBase]),
                        TransactionAmountClaimCurrency = DbNullableConvert(Convert.ToDecimal, reader[HistoricalFinancialAmountDataSql.AccumulatedClaimAmounts.TransactionAmountClaimCurrency]),
                        TransactionAmountOriginal = DbNullableConvert(Convert.ToDecimal, reader[HistoricalFinancialAmountDataSql.AccumulatedClaimAmounts.TransactionAmountOriginal]),
                        MovementAmountAccounting = DbNullableConvert(Convert.ToDecimal, reader[HistoricalFinancialAmountDataSql.AccumulatedClaimAmounts.MovementAmountAccounting]),
                        MovementAmountBase = DbNullableConvert(Convert.ToDecimal, reader[HistoricalFinancialAmountDataSql.AccumulatedClaimAmounts.MovementAmountBase]),
                        MovementAmountClaimCurrency = DbNullableConvert(Convert.ToDecimal, reader[HistoricalFinancialAmountDataSql.AccumulatedClaimAmounts.MovementAmountClaimCurrency]),
                        MovementAmountOriginal = DbNullableConvert(Convert.ToDecimal, reader[HistoricalFinancialAmountDataSql.AccumulatedClaimAmounts.MovementAmountOriginal]),
                    };

                    // Add this new item to the list
                    claimFinancialAmounts.Add(claimFinancialAmount);
                });

            // Return the list of Claim Financial Amounts
            return claimFinancialAmounts;
        }

        /// <summary>
        /// Returns the Header Id on the basis of Policy Header Reference.
        /// </summary>
        /// <param name="headerReference">Policy Header Reference</param>
        /// <returns>Header Id or 0 if no Header found</returns>
        private long GetHeaderIDFromRef(string headerReference)
        {
            using (UnderwritingEntities entities = UnderwritingEntitiesFactory.GetUnderwritingEntities())
            {
                Header header = (from row in entities.Header where row.HeaderReference == headerReference select row).FirstOrDefault();
                if (header != null)
                {
                    return header.HeaderID;
                }
            }

            return 0;
        }

        /// <summary>
        /// Returns the list of Generic data items of a Policy Header Component.
        /// </summary>
        /// <param name="componentID">Component ID</param>
        /// <param name="vehicleType">Vehicle Type</param>
        /// <param name="division">Division Codes</param>
        /// <param name="reasonCode">Reason Codes</param>
        /// <param name="identifier">Generic Data Type Code</param>
        /// <returns>List of Generic data items or null</returns>
        private IEnumerable<IGenericDataItem> GetHeaderDeductibles(long componentID, string vehicleType, string division, string reasonCode, string identifier)
        {
            using (UnderwritingEntities entities = UnderwritingEntitiesFactory.GetUnderwritingEntities())
            {
                // find the header for the given HeaderID (there will be only one)
                Header header = (from row in entities.Header where row.HeaderID == componentID select row).FirstOrDefault();
                if (header != null)
                {
                    // Get the latest Header Version for this Policy Header
                    HeaderVersion headerVersion = (HeaderVersion)header.GetLatestVersion();
                    if (headerVersion.GenericDataSet != null && headerVersion.GenericDataSet.GenericDataItems.Count > 0)
                    {
                        // If there are Generic Data Sets associated with this Header Version then collect any that match the Generic Data Code identifier
                        IEnumerable<IGenericDataItem> genericDataItems = headerVersion.GenericDataSet.GenericDataItems.Where(gdi => gdi.GenericDataTypeCode == identifier);
                        if (genericDataItems != null && genericDataItems.Count() > 0)
                        {
                            // Returns an enumerable list that meets to the vehicle, division and reason code filters
                            return this.MatchOnSubType(vehicleType, division, reasonCode, genericDataItems);
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// This method retrieves the latest terms version from XIAP database.
        /// </summary>
        /// <param name ="headerID">Header Id</param>
        /// <returns>term Versions or null if none found</returns>
        public IUWTerms GetlatestTerm(long headerID)
        {
            using (UnderwritingEntities entities = UnderwritingEntitiesFactory.GetUnderwritingEntities())
            {
                // Find the policy Header data for the given HeaderID and include Terms data.
                Header header = (from row in entities.Header
                                                     .Include("Terms")
                                 where row.HeaderID == headerID
                                 select row).FirstOrDefault();

                if (header != null)
                {
                    // We have a header so attempt to collect the first terms data record
                    Terms terms = header.Terms.FirstOrDefault();
                    if (terms != null)
                    {
                        // Return the latest version of the terms.
                        return (TermsVersion)terms.GetLatestVersion();
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Retuirns the Reason Codes of the Policy Header component.
        /// </summary>
        /// <param name="componentID">Component ID</param>
        /// <param name="identifier">Generic DAta Type Code</param>
        /// <returns>All Reason Codes of the Component</returns>
        private IEnumerable<string> GetHeaderReasonCodes(long componentID, string identifier)
        {
            List<string> reasonCodes = new List<string>();
            using (UnderwritingEntities entities = UnderwritingEntitiesFactory.GetUnderwritingEntities())
            {
                // Find the Policy Header that matches the supplied HeaderID
                Header header = (from row in entities.Header where row.HeaderID == componentID select row).FirstOrDefault();
                if (header != null)
                {
                    // Get the latest version of this Header.
                    HeaderVersion headerVersion = (HeaderVersion)header.GetLatestVersion();
                    if (headerVersion.GenericDataSet != null && headerVersion.GenericDataSet.GenericDataItems.Count > 0)
                    {
                        // If any generic data items are attached to the Header, filter them.
                        IEnumerable<IGenericDataItem> genericDataItems = headerVersion.GenericDataSet.GenericDataItems.Where(gdi => gdi.GenericDataTypeCode == identifier);
                        if (genericDataItems != null && genericDataItems.Count() > 0)
                        {
                            // Select list of Reason Codes
                            reasonCodes = genericDataItems.Where(a => !string.IsNullOrEmpty(a.CustomCode03)).Select(a => a.CustomCode03).Distinct().ToList();
                        }
                    }
                }
            }

            return reasonCodes;
        }

        /// <summary>
        /// Returns the section level deductibles
        /// </summary>
        /// <param name="componentID">Section ID</param>
        /// <param name="vehicleType">Vehicle Type</param>
        /// <param name="division">Division Codes</param>
        /// <param name="reasonCode">Reason Codes</param>
        /// <param name="identifier">Generic Data Type Code</param>
        /// <returns>List of Generic Data Types</returns>
        private IEnumerable<IGenericDataItem> GetSectionDeductibles(long componentID, string vehicleType, string division, string reasonCode, string identifier)
        {
            using (UnderwritingEntities entities = UnderwritingEntitiesFactory.GetUnderwritingEntities())
            {
                // Get the Section for the passed in ID
                Section section = (from row in entities.Section where row.SectionID == componentID select row).FirstOrDefault();
                if (section != null)
                {
                    // Get the Summary Section Detail.
                    SectionDetail summarySD = this.GetSummarySectionDetail(section);
                    if (summarySD != null)
                    {
                        // Get the latest Section Detail version.
                        SectionDetailVersion sectionDetailVersion = (SectionDetailVersion)summarySD.GetLatestVersion();
                        if (sectionDetailVersion.GenericDataSet != null && sectionDetailVersion.GenericDataSet.GenericDataItems.Count > 0)
                        {
                            // If there are any generic data sets on this SDV then we need to process them.
                            IEnumerable<IGenericDataItem> genericDataItems = sectionDetailVersion.GenericDataSet.GenericDataItems.Where(gdi => gdi.GenericDataTypeCode == identifier);
                            if (genericDataItems != null && genericDataItems.Count() > 0)
                            {
                                // Filter the list via Vehicle, division and reasoncode.
                                IEnumerable<IGenericDataItem> selectedDataItems = this.MatchOnSubType(vehicleType, division, reasonCode, genericDataItems);
                                if (selectedDataItems != null && selectedDataItems.Count() > 0)
                                {
                                    // return if we have any items that match the filter.
                                    return selectedDataItems;
                                }
                            }
                        }
                    }

                    // If we have no items matching on Section, check at the Header level and return.
                    return this.GetHeaderDeductibles(section.Header.HeaderID, vehicleType, division, reasonCode, identifier);
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the section level Reason Codes
        /// </summary>
        /// <param name="componentID">Section ID</param>
        /// <param name="identifier">Generic Data Type Code</param>
        /// <returns>List of reason codes</returns>
        private IEnumerable<string> GetSectionReasonCodes(long componentID, string identifier)
        {
            List<string> reasonCodes = new List<string>();
            using (UnderwritingEntities entities = UnderwritingEntitiesFactory.GetUnderwritingEntities())
            {
                // Get the Section for the passed in ID
                Section section = (from row in entities.Section where row.SectionID == componentID select row).FirstOrDefault();
                if (section != null)
                {
                    // Get the Summary Section Detail.
                    SectionDetail summarySD = this.GetSummarySectionDetail(section);
                    if (summarySD != null)
                    {
                        // Get the latest Section Detail version.
                        SectionDetailVersion sectionDetailVersion = (SectionDetailVersion)summarySD.GetLatestVersion();
                        if (sectionDetailVersion.GenericDataSet != null && sectionDetailVersion.GenericDataSet.GenericDataItems.Count > 0)
                        {
                            // If there are any generic data sets on this SDV then apply a filter to them.
                            IEnumerable<IGenericDataItem> genericDataItems = sectionDetailVersion.GenericDataSet.GenericDataItems.Where(gdi => gdi.GenericDataTypeCode == identifier);
                            if (genericDataItems != null && genericDataItems.Count() > 0)
                            {
                                // Select list of Reason Codes
                                reasonCodes = genericDataItems.Where(a => !string.IsNullOrEmpty(a.CustomCode03)).Select(a => a.CustomCode03).Distinct().ToList();
                            }
                        }
                    }
                }
            }

            return reasonCodes;
        }

        /// <summary>
        /// Returns the section detail level deductibles
        /// </summary>
        /// <param name="componentID">Section Detail ID</param>
        /// <param name="vehicleType">Vehicle Type</param>
        /// <param name="division">Division Codes</param>
        /// <param name="reasonCode">Reason Codes</param>
        /// <param name="identifier">Generic Data Type Code</param>
        /// <returns>List of Generic Data Types</returns>
        private IEnumerable<IGenericDataItem> GetSectionDetailDeductibles(long componentID, string vehicleType, string division, string reasonCode, string identifier)
        {
            using (UnderwritingEntities entities = UnderwritingEntitiesFactory.GetUnderwritingEntities())
            {
                // Get the SectionDetail for the passed in ID
                SectionDetail sectionDetail = (from row in entities.SectionDetail where row.SectionDetailID == componentID select row).FirstOrDefault();
                if (sectionDetail != null)
                {
                    // Get the latest Section Detail version.
                    SectionDetailVersion sectionDetailVersion = (SectionDetailVersion)sectionDetail.GetLatestVersion();
                    if (sectionDetailVersion.GenericDataSet != null && sectionDetailVersion.GenericDataSet.GenericDataItems.Count > 0)
                    {
                        // If there are any generic data sets on this SDV then process them.
                        IEnumerable<IGenericDataItem> genericDataItems = sectionDetailVersion.GenericDataSet.GenericDataItems.Where(gdi => gdi.GenericDataTypeCode == identifier);
                        if (genericDataItems != null && genericDataItems.Count() > 0)
                        {
                            // Filter out ones that don't match on Vehicle Type, division and ReasonCode
                            IEnumerable<IGenericDataItem> selectedDataItems = this.MatchOnSubType(vehicleType, division, reasonCode, genericDataItems);
                            if (selectedDataItems != null && selectedDataItems.Count() > 0)
                            {
                                // If we have any from the filtering, return them.
                                return selectedDataItems;
                            }
                        }
                    }

                    // We didn't find any items that matched the filter so check again at the Section level
                    return this.GetSectionDeductibles(sectionDetail.Section.SectionID, vehicleType, division, reasonCode, identifier);
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the section detail level Reason Codes
        /// </summary>
        /// <param name="componentID">Component ID</param>
        /// <param name="identifier">Generic Data Type Code</param>
        /// <returns>List of reason codes</returns>
        private IEnumerable<string> GetSectionDetailReasonCodes(long componentID, string identifier)
        {
            List<string> reasonCodes = new List<string>();
            using (UnderwritingEntities entities = UnderwritingEntitiesFactory.GetUnderwritingEntities())
            {
                // Get the SectionDetail for the passed in ID
                SectionDetail sectionDetail = (from row in entities.SectionDetail where row.SectionDetailID == componentID select row).FirstOrDefault();
                if (sectionDetail != null)
                {
                    // Get the latest Section Detail version.
                    SectionDetailVersion sectionDetailVersion = (SectionDetailVersion)sectionDetail.GetLatestVersion();
                    if (sectionDetailVersion.GenericDataSet != null && sectionDetailVersion.GenericDataSet.GenericDataItems.Count > 0)
                    {
                        // If there are any generic data sets on this SDV then process them.
                        IEnumerable<IGenericDataItem> genericDataItems = sectionDetailVersion.GenericDataSet.GenericDataItems.Where(gdi => gdi.GenericDataTypeCode == identifier);
                        if (genericDataItems != null && genericDataItems.Count() > 0)
                        {
                            // Select list of Reason Codes
                            reasonCodes = genericDataItems.Where(a => !string.IsNullOrEmpty(a.CustomCode03)).Select(a => a.CustomCode03).Distinct().ToList();
                        }
                    }
                }
            }

            return reasonCodes;
        }

        /// <summary>
        /// Returns the coverage level deductibles
        /// </summary>
        /// <param name="componentID">Coverage ID</param>
        /// <param name="vehicleType">Vehicle Type</param>
        /// <param name="division">Division Codes</param>
        /// <param name="reasonCode">Reason Codes</param>
        /// <param name="identifier">Generic Data Type Code</param>
        /// <returns>List of Generic Data Types</returns>
        private IEnumerable<IGenericDataItem> GetCoverageDeductibles(long componentID, string vehicleType, string division, string reasonCode, string identifier)
        {
            using (UnderwritingEntities entities = UnderwritingEntitiesFactory.GetUnderwritingEntities())
            {
                // Get the Coverage from the passed in ID
                Coverage coverage = (from row in entities.Coverage where row.CoverageID == componentID select row).FirstOrDefault();
                if (coverage != null)
                {
                    // Get the latest Coverage Version.
                    CoverageVersion coverageVersion = (CoverageVersion)coverage.GetLatestVersion();
                    if (coverageVersion.GenericDataSet != null && coverageVersion.GenericDataSet.GenericDataItems.Count > 0)
                    {
                        // If there are any generic data sets on this coverage version, process them
                        IEnumerable<IGenericDataItem> genericDataItems = coverageVersion.GenericDataSet.GenericDataItems.Where(gdi => gdi.GenericDataTypeCode == identifier);
                        if (genericDataItems != null && genericDataItems.Count() > 0)
                        {
                            // Select generic data items via filtering on vehicle type, division and reason code.
                            IEnumerable<IGenericDataItem> selectedDataItems = this.MatchOnSubType(vehicleType, division, reasonCode, genericDataItems);
                            if (selectedDataItems != null && selectedDataItems.Count() > 0)
                            {
                                // If we have any items that match the filter, return them.
                                return selectedDataItems;
                            }
                        }
                    }

                    // Otherwise, return a call to check the Section Detail containing this coverage.
                    return this.GetSectionDetailDeductibles(coverage.SectionDetail.SectionDetailID, vehicleType, division, reasonCode, identifier);
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the coverage level Reason Codes
        /// </summary>
        /// <param name="componentID">Component ID</param>
        /// <param name="identifier">Generic Data Type Code</param>
        /// <returns>List of reason codes</returns>
        private IEnumerable<string> GetCoverageReasonCodes(long componentID, string identifier)
        {
            List<string> reasonCodes = new List<string>();
            using (UnderwritingEntities entities = UnderwritingEntitiesFactory.GetUnderwritingEntities())
            {
                // Get the Coverage from the passed in ID
                Coverage coverage = (from row in entities.Coverage where row.CoverageID == componentID select row).FirstOrDefault();
                if (coverage != null)
                {
                    // Get the latest Coverage Version.
                    CoverageVersion coverageVersion = (CoverageVersion)coverage.GetLatestVersion();
                    if (coverageVersion.GenericDataSet != null && coverageVersion.GenericDataSet.GenericDataItems.Count > 0)
                    {
                        // If there are any generic data sets on this coverage version, process them
                        IEnumerable<IGenericDataItem> genericDataItems = coverageVersion.GenericDataSet.GenericDataItems.Where(gdi => gdi.GenericDataTypeCode == identifier);
                        if (genericDataItems != null && genericDataItems.Count() > 0)
                        {
                            // Select list of Reason Codes
                            reasonCodes = genericDataItems.Where(a => !string.IsNullOrEmpty(a.CustomCode03)).Select(a => a.CustomCode03).Distinct().ToList();
                        }
                    }
                }
            }

            return reasonCodes;
        }

        /// <summary>
        /// Checks wheather deductibles exists on Policy Header.
        /// </summary>
        /// <param name="header">Header Component</param>
        /// <param name="identifier">Generic Data Type Code</param>
        /// <returns>Boolean value</returns>
        private bool HeaderDeductiblesExist(Header header, string identifier)
        {
            // Get the Header Version from the Header
            HeaderVersion headerVersion = (HeaderVersion)header.GetLatestVersion();
            if (headerVersion.GenericDataSet != null && headerVersion.GenericDataSet.GenericDataItems.Count > 0)
            {
                // if we have any generic data set items that match the passed in identifier then deductibles exist.
                if (headerVersion.GenericDataSet.GenericDataItems.Where(gdi => gdi.GenericDataTypeCode == identifier).Any())
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks wheather deductibles exists on Policy Section.
        /// </summary>
        /// <param name="header">Header Component</param>
        /// <param name="identifier">Generic Data Type Code</param>
        /// <returns>Boolean value</returns>
        private bool SectionDeductiblesExist(Header header, string identifier)
        {
            foreach (Section section in header.Sections)
            {
                // Check the Summary SD to see if there is a Generic Data Set
                SectionDetail summarySD = this.GetSummarySectionDetail(section);
                if (summarySD != null)
                {
                    // Get the latest Section Detail Verison
                    SectionDetailVersion sectionDetailVersion = (SectionDetailVersion)summarySD.GetLatestVersion();
                    if (sectionDetailVersion.GenericDataSet != null && sectionDetailVersion.GenericDataSet.GenericDataItems.Count > 0)
                    {
                        // if we have any generic data set items that match the passed in identifier then deductibles exist.
                        if (sectionDetailVersion.GenericDataSet.GenericDataItems.Where(gdi => gdi.GenericDataTypeCode == identifier).Any())
                        {
                            return true;
                        }
                    }
                }

                if (this.SDDeductiblesExist(section, identifier))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks wheather deductibles exists on Policy SectionDetails.
        /// </summary>
        /// <param name="section">Section Component</param>
        /// <param name="identifier">Generic Data Type Code</param>
        /// <returns>Boolean value</returns>
        private bool SDDeductiblesExist(Section section, string identifier)
        {
            // loop through each Section Detail on the given Section
            foreach (SectionDetail sectionDetail in section.SectionDetails)
            {
                // Get the latest Section Detail Verison
                SectionDetailVersion sectionDetailVersion = (SectionDetailVersion)sectionDetail.GetLatestVersion();
                if (sectionDetailVersion.GenericDataSet != null && sectionDetailVersion.GenericDataSet.GenericDataItems.Count > 0)
                {
                    // if we have any generic data set items that match the passed in identifier then deductibles exist.
                    if (sectionDetailVersion.GenericDataSet.GenericDataItems.Where(gdi => gdi.GenericDataTypeCode == identifier).Any())
                    {
                        return true;
                    }
                }

                // We didn't find anything at the SectionDetail version so check the coverages.
                if (this.CoverageDeductiblesExist(sectionDetail, identifier))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks wheather deductibles exists on Policy Coverage.
        /// </summary>
        /// <param name="sectionDetail">Section Detail</param>
        /// <param name="identifier">Generic Data Type Code</param>
        /// <returns>Boolean value</returns>
        private bool CoverageDeductiblesExist(SectionDetail sectionDetail, string identifier)
        {
            // Loop through each coverage on the given Section Detail
            foreach (Coverage coverage in sectionDetail.Coverages)
            {
                // Get the latest version
                CoverageVersion coverageVersion = (CoverageVersion)coverage.GetLatestVersion();
                if (coverageVersion.GenericDataSet != null)
                {
                    // if we have any generic data set items that match the passed in identifier then deductibles exist.
                    if (coverageVersion.GenericDataSet.GenericDataItems.Where(gdi => gdi.GenericDataTypeCode == identifier).Any())
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Filters Generic Data Items on the basis of Vehicle Type, Division Codes or Reason Codes.
        /// </summary>
        /// <param name="vehicleType">Vehicle Type</param>
        /// <param name="division">Division Codes</param>
        /// <param name="reasonCode">Reason Codes</param>
        /// <param name="genericDataItems">Generic Data Items</param>
        /// <returns>List of Generic Data Items</returns>
        private IEnumerable<IGenericDataItem> MatchOnSubType(string vehicleType, string division, string reasonCode, IEnumerable<IGenericDataItem> genericDataItems)
        {
            // Check Vehicle Type
            IEnumerable<IGenericDataItem> selectedDataItems = genericDataItems.Where(a => !string.IsNullOrEmpty(a.CustomCode02));
            if (selectedDataItems != null && selectedDataItems.Count() > 0)
            {
                return selectedDataItems.Where(a => a.CustomCode02 == vehicleType);
            }
            // Check Division
            selectedDataItems = genericDataItems.Where(a => !string.IsNullOrEmpty(a.CustomCode01));
            if (selectedDataItems != null && selectedDataItems.Count() > 0)
            {
                return selectedDataItems.Where(a => a.CustomCode01 == division);
            }
            // Check Reason Code
            selectedDataItems = genericDataItems.Where(a => !string.IsNullOrEmpty(a.CustomCode03));
            if (selectedDataItems != null && selectedDataItems.Count() > 0)
            {
                return selectedDataItems.Where(a => a.CustomCode03 == reasonCode);
            }

            // If no match, then Deductibles have been recorded without sublevel
            return genericDataItems;
        }

        /// <summary>
        /// Builds all reason code list.
        /// </summary>
        /// <param name="allReasonCodeList">All reason code list.</param>
        /// <param name="externalReference">The external reference.</param>
        /// <param name="reasonCodes">The reason codes.</param>
        private void BuildAllReasonCodeList(Dictionary<string, List<CodeRow>> allReasonCodeList, string externalReference, List<string> reasonCodes)
        {
            if (!string.IsNullOrEmpty(externalReference) && reasonCodes.Count() > 0)
            {
                // Only add the allowed reason codes to the list via call to LoadReasonCodes
                allReasonCodeList.Add(externalReference, this.LoadReasonCodes(reasonCodes));
            }
        }

        /// <summary>
        /// Loads the reason codes that are allowed into a returned string list.
        /// </summary>
        /// <param name="reasonCode">Reason Code list</param>
        /// <returns>Returns the Code rows to be dispayed on UI</returns>
        private List<CodeRow> LoadReasonCodes(IEnumerable<string> reasonCode)
        {
            List<CodeRow> codeRows = new List<CodeRow>();
            Field field = new Field();
            // Assign the LookupDefinition in the form of BusinessComponentKey for the Deductible Reason Codes
            string keyName = "SetCode";

            field.LookupDefinitionKey = new BusinessComponentKey("ValueSet", 1);
            field.LookupDefinitionKey.Add(keyName, "100059"); // SetCode for Deductible Reason Codes
            field.LookupParameters = new LookupParameters();

            // results from the LookUp are used to check the allowed values: is the reason code valid?
            var results = FieldsHelper.AllowedValues(field, Enumerations.DescriptionType.ShortDescription);
            foreach (var reason in reasonCode)
            {
                var records = results.Where(o => o.Code == reason);
                if (records != null && records.Count() > 0)
                {
                    // Add the ReasonCode to the returned list
                    codeRows.AddRange(records);
                }
            }

            return codeRows;
        }

        /// <summary>
        /// Used when attempting convert a value taken from the database to a valid value in code.
        /// If we attempt to convert a null DB value we would get an error so we simply return null.
        /// Otherwise, the convert function that is passed in will be used, e.g. ConvertTo.Int32()
        /// </summary>
        /// <typeparam name="T">Variable type</typeparam>
        /// <param name="converter">The converter.</param>
        /// <param name="value">The value.</param>
        /// <returns>Converted value</returns>
        private T? DbNullableConvert<T>(Func<object, T> converter, object value) where T : struct
        {
            if (value == System.DBNull.Value)
            {
                return null;
            }

            return converter(value);
        }

        /// <summary>
        /// Checks whether authorised, not in progress, financial transactions exists on the claim.
        /// </summary>
        /// <param name="claimHeaderId">Claim Header Id</param>
        /// <returns>Boolean value</returns>
        public bool HasFinancialTransactionInTheClaim(long claimHeaderId)
        {
            bool hasFinanTransaction = false;
            using (ClaimsEntities entities = ClaimsEntitiesFactory.GetClaimsEntities())
            {
                try
                {
                    // open the connection to the system data if it's closed.
                    if (entities.Connection.State == System.Data.ConnectionState.Closed)
                    {
                        entities.Connection.Open();
                    }

                    // Retrieve the claim header object for the given ID
                    var claimHeader = (from transHeader in entities.ClaimHeader
                                       where transHeader.ClaimHeaderID == claimHeaderId
                                       select transHeader).FirstOrDefault();

                    // Count the authorised historic claim transaction headers for Reserves, payments, recovery reserves and recovery receipts
                    var count = claimHeader.HistoricalClaimTransactionHeaders.Where(m => (m.ClaimTransactionSource == (short)StaticValues.ClaimTransactionSource.Reserve && m.ReserveAuthorisationStatus == (short)StaticValues.ReserveAuthorisationStatus.ReserveAuthorised)
                                                                                          || (m.ClaimTransactionSource == (short)StaticValues.ClaimTransactionSource.Payment && m.PaymentAuthorisationStatus == (short)StaticValues.PaymentAuthorisationStatus.PaymentAuthorised)
                                                                                          || (m.ClaimTransactionSource == (short)StaticValues.ClaimTransactionSource.RecoveryReserve && m.RecoveryReserveAuthorisationStatus == (short)StaticValues.RecoveryReserveAuthorisationStatus.RecoveryReserveAuthorised)
                                                                                          || (m.ClaimTransactionSource == (short)StaticValues.ClaimTransactionSource.RecoveryReceipt && m.RecoveryReceiptAuthorisationStatus == (short)StaticValues.RecoveryReceiptAuthorisationStatus.RecoveryReceiptAuthorised)).Count();

                    if (count > 0)
                    {
                        // return true if we find any.
                        hasFinanTransaction = true;
                    }
                }
                finally
                {
                    // Safely close the connection to the system data, if necessary.
                    if (entities != null && entities.Connection != null)
                    {
                        if (entities.Connection.State == System.Data.ConnectionState.Open)
                        {
                            entities.Connection.Close();
                        }

                        entities.Connection.Dispose();
                    }
                }
            }

            return hasFinanTransaction;
        }
        /// <summary>
        /// method is used to execute stored procedure IsUserAvailableSP which takes user identity as a parameter.
        /// </summary>
        /// <param name="userId">user Id</param>
        /// <param name="taskRedirectUserIdentity">Return taskRedirectUserIdentity</param>
        /// <returns>If user is not available i.e. IsUserOutOfOffice is true return value as true else false</returns>
        public bool IsUserOutOfOffice(long userId, out string taskRedirectUserIdentity)
        {
            bool result = false;
            taskRedirectUserIdentity = string.Empty;
            using (SqlConnection sqlConnection = new SqlConnection(this.ConnectionString))
            {
                // Connect to the DB
                using (SqlCommand sqlCommand = new SqlCommand())
                {
                    // Set up the parameters and call to the stored procedure [dbo].[IsUserAvailable]
                    sqlCommand.Connection = sqlConnection;
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.CommandText = "[dbo].[IsUserAvailable]";
                    sqlCommand.Parameters.AddWithValue("@userId", userId);
                    sqlCommand.Parameters.AddWithValue("@availableAtDate", DateTimeUtils.GetDateTimeNow());
                    sqlCommand.Parameters.AddWithValue("@result", result);
                    sqlCommand.Parameters.AddWithValue("@overrideTaskRedirectUserIdentity", taskRedirectUserIdentity);
                    sqlCommand.Parameters["@result"].Direction = ParameterDirection.Output;
                    sqlCommand.Parameters["@overrideTaskRedirectUserIdentity"].Size = 50;
                    sqlCommand.Parameters["@overrideTaskRedirectUserIdentity"].Direction = ParameterDirection.Output;

                    try
                    {
                        sqlConnection.Open();
                        if (_Logger.IsDebugEnabled)
                        {
                            _Logger.Debug(string.Format("ExecuteIsUserAvailableSP({0})", userId));
                        }

                        sqlCommand.ExecuteNonQuery();
                        result = (bool)sqlCommand.Parameters["@result"].Value;
                        taskRedirectUserIdentity = Convert.ToString(sqlCommand.Parameters["@overrideTaskRedirectUserIdentity"].Value);
                        /* SP returns the value as true for user is available and false for user is not available. 
                           IF SP returns value for true, means the user is available and here negate this value as false and vice versa:
                         * TRUE = In the office, so return FALSE
                         * FALSE = out of the office, so return TRUE
                         */

                        return !result;
                    }
                    catch (Exception ex)
                    {
                        _Logger.Error(string.Format("ExecuteIsUserAvailableSP({0})", userId));
                        _Logger.Error(ex);
                        throw ex;
                    }
                }
            }
        }

        private SectionDetail GetSummarySectionDetail(Section section)
        {
            List<SectionDetailTypeData> sectionDetailTypes = SystemValueSetCache.GetSystemValues<SectionDetailTypeData>(Xiap.Framework.Metadata.SystemValueSetCodeEnum.SectionDetailType).Where(x => x.IsSummarySectionDetailType == true).ToList();
            SectionDetail summarySectionDetail = section.SectionDetails.Where(sd => sectionDetailTypes.Exists(sdt => sdt.Code == sd.SectionDetailTypeCode)).FirstOrDefault();
            return summarySectionDetail;
        }
    }
}
