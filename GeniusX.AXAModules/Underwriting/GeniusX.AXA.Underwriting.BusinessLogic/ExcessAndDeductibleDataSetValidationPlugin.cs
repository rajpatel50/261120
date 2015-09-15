using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Xiap.Framework;
using Xiap.Framework.BusinessTransaction;
using Xiap.Framework.Common;
using Xiap.Framework.ProcessHandling;
using Xiap.Metadata.BusinessComponent;
using Xiap.UW.BusinessComponent;

namespace GeniusX.AXA.Underwriting.BusinessLogic
{
    /// <summary>
    /// Validates Policy data requirements including excess and deductible data.
    /// </summary>
    public class ExcessAndDeductibleDataSetValidationPlugin : ITransactionPlugin
    {
        private const string GenericDataType_Excess = "GenericDataType.Excess";                         // AND3
        private const string GenericDataType_Deductible = "GenericDataType.Deductible";                 // AND2
        private const string GenericDataType_Deductible_Client = "GenericDataType.Deductible.Client";   // CLI
        private const string GenericDataType_Deductible_Captive = "GenericDataType.Deductible.Captive"; // CAP
        private const string GenericDataType_Deductible_Service = "GenericDataType.Deductible.Service"; // SVC

        #region ITransactionPlugin Members

        /// <summary>
        /// Processes the transaction - validates policy data items on PostValidate
        /// </summary>
        /// <param name="businessTransaction">The business transaction.</param>
        /// <param name="point">The point.</param>
        /// <param name="PluginId">The plugin identifier.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>Process Results Collection or null</returns>
        public ProcessResultsCollection ProcessTransaction(IBusinessTransaction businessTransaction, TransactionInvocationPoint point, int PluginId, params object[] parameters)
        {
            if (point == TransactionInvocationPoint.PostValidate)
            {
                return this.ValidatePolicyDataItems((Header)businessTransaction.Component);
            }

            return null;
        }

        #endregion

        /// <summary>
        /// Retrieves a configuration item from the application configuration. 
        /// If this config item isn't found, an exception is thrown.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns>string value</returns>
        /// <exception cref="System.InvalidOperationException"></exception>
        private static string ResolveMandatoryConfig(string propertyName)
        {
            var value = ConfigurationManager.AppSettings[propertyName];
            if (value == null)
            {
                throw new InvalidOperationException(string.Format("Missing config property: {0}", propertyName));
            }

            return value;
        }

        /// <summary>
        /// Validates the policy data items.
        /// </summary>
        /// <param name="header">The header.</param>
        /// <returns>Process Results Collection</returns>
        private ProcessResultsCollection ValidatePolicyDataItems(Header header)
        {
            var processResultsCollection = new ProcessResultsCollection();
            // Get the latest version of the UW Header
            var headerVersion = (HeaderVersion)header.GetLatestVersion();
            // Validate the Generic Data Items
            this.ValidateGenericDataItems(processResultsCollection, headerVersion, this.ExtractGenericDataItems(headerVersion), headerVersion);
            
            var sectionDetails = from s in header.Sections
                                 from sd in s.SectionDetails
                                 select sd;
            // Cycle through all Section Details on the Header
            foreach (var sectionDetail in sectionDetails)
            {
                // Get the latest version
                var sectionDetailVersion = (SectionDetailVersion)sectionDetail.GetLatestVersion();
                // Validate all the generic data items are correct on this Section detail.
                this.ValidateGenericDataItems(processResultsCollection, sectionDetailVersion, this.ExtractGenericDataItems(sectionDetailVersion), headerVersion);

                // Cycle through all the coverages on this Section Detail
                foreach (var coverage in sectionDetail.Coverages)
                {
                    // Get latest version and validate the Generic data items
                    var coverageVersion = (CoverageVersion)coverage.GetLatestVersion();
                    this.ValidateGenericDataItems(processResultsCollection, coverageVersion, this.ExtractGenericDataItems(coverageVersion), headerVersion);
                }
            }

            return processResultsCollection;
        }

        /// <summary>
        /// Validates the generic data items and puts data into the ProcessResultsCollection.
        /// All objects are passed by reference so the ProcessResults collection will return the results
        /// </summary>
        /// <param name="processResults">The process results.</param>
        /// <param name="sourceComponent">The source component.</param>
        /// <param name="genericDataItems">The generic data items.</param>
        /// <param name="header">The header.</param>
        private void ValidateGenericDataItems(ProcessResultsCollection processResults, IBusinessComponent sourceComponent, IEnumerable<IGenericDataItem> genericDataItems, HeaderVersion header)
        {
            // Get excess type Data Items then validate
            var excessDataItems = genericDataItems.Where(a => a.GenericDataTypeCode == ResolveMandatoryConfig("GenericDataType.Excess"));
            this.CommonGenericDataItemsValidation(processResults, sourceComponent, excessDataItems);
            // Get deductible data items, then validate
            var deductibleDataItems = genericDataItems.Where(a => a.GenericDataTypeCode == ResolveMandatoryConfig("GenericDataType.Deductible"));
            this.CommonGenericDataItemsValidation(processResults, sourceComponent, deductibleDataItems);
            // Validate deductible sequence
            this.DeductibleSequenceValidation(processResults, sourceComponent, deductibleDataItems, header);
        }

        /// <summary>
        /// Validates the collection of generic data items that is passed in.
        /// </summary>
        /// <param name="processResults">The process results.</param>
        /// <param name="sourceComponent">The source component.</param>
        /// <param name="genericDataItems">The generic data items.</param>
        private void CommonGenericDataItemsValidation(ProcessResultsCollection processResults, IBusinessComponent sourceComponent, IEnumerable<IGenericDataItem> genericDataItems)
        {
            var genericDataItemCount = genericDataItems.Count();
            // Return if there are no items in the collection
            if (genericDataItemCount == 0)
            {
                return;
            }

            // CustomCode01 = Division SubSection, CustomCode02 = Vehicle SubSection, CustomCode03 = DeductibleReasonSubSection
            bool isNotSubSection = genericDataItems.Any(a => a.CustomCode01 == null && a.CustomCode02 == null && a.CustomCode03 == null);
            bool isDivisionSubSection = genericDataItems.Any(a => a.CustomCode01 != null);
            bool isVehicleSubSection = genericDataItems.Any(a => a.CustomCode02 != null);
            bool isDeductibleReasonSubSection = genericDataItems.Any(a => a.CustomCode03 != null);
            var currentlyActiveSubSections = new[] { isDivisionSubSection, isVehicleSubSection, isDeductibleReasonSubSection }.Count(a => a);

            // should only have one or fewer active sub sections AND not mix sub section and top level
            if (genericDataItemCount > 1 && (currentlyActiveSubSections > 1 || (currentlyActiveSubSections > 0 && isNotSubSection)))
            {
                UWBusinessLogicHelper.AddError(processResults, "DataItemsRepresentMultipleSubSections", ProcessInvocationPoint.Validation, sourceComponent);
            }
        }

        /// <summary>
        /// Validate the Deductible Sequence
        /// </summary>
        /// <param name="processResults">The process results.</param>
        /// <param name="sourceComponent">The source component.</param>
        /// <param name="genericDataItems">The generic data items.</param>
        /// <param name="headerVersion">The header version.</param>
        private void DeductibleSequenceValidation(ProcessResultsCollection processResults, IBusinessComponent sourceComponent, IEnumerable<IGenericDataItem> genericDataItems, HeaderVersion headerVersion)
        {
            // Group by CustomNumeric01 - the Deductible sequence
            var grouping = genericDataItems.ToLookup(a => a.CustomNumeric01);

            // Deductible sequence must be different if subtypes match
            foreach (var group in grouping)
            {
                IGenericDataItem prevGroup = null;

                // Cycle through each entry in the group (of deductibles at a given sequence)
                foreach (IGenericDataItem entry in group)
                {
                    // If we've viewed a group previously, compare the subtypes and throw an error if they match
                    if (prevGroup != null)
                    {
                        // CustomCode01 = Division SubSection, CustomCode02 = Vehicle SubSection, CustomCode03 = DeductibleReasonSubSection
                        if (entry.CustomCode01 == prevGroup.CustomCode01
                            && entry.CustomCode02 == prevGroup.CustomCode02
                            && entry.CustomCode03 == prevGroup.CustomCode03)
                        {
                            UWBusinessLogicHelper.AddError(processResults, "DeductibleSequenceMustBeDistinct", ProcessInvocationPoint.Validation, sourceComponent);
                            break;
                        }
                    }
                    // Assign this current Item to the 'previousGroup' (previous entry in the group) field.
                    prevGroup = entry;
                }
            }

            // Cycle through all Generic Data Items
            foreach (IGenericDataItem genericData in genericDataItems)
            {
                decimal customNumeric03 = genericData.CustomNumeric03.GetValueOrDefault(0); // CustomNumeric03 is Deductible Policy Sequence number
                // Check if this is Funded (CustomBoolean03) AND 
                // - the Deductible Type matches the Deductible Service Generic Data Type in the config
                //  OR 
                // - the Deductible Type matches the Deductible Captive Generic Data Type in the config
                // AND Deductible Policy Sequence number is 0
                if (genericData.CustomBoolean03.GetValueOrDefault(false) == true && (genericData.CustomCode04 == ResolveMandatoryConfig(GenericDataType_Deductible_Service) || genericData.CustomCode04 == ResolveMandatoryConfig(GenericDataType_Deductible_Captive)) && customNumeric03 == 0)
                {
                    // Raise error that the Deductible Policy Sequence must be > 0.
                    string shortDescription = this.GetUWValueSetShortDescription(genericData);
                    UWBusinessLogicHelper.AddError(processResults, UwMessageConstants.DEDUCTIBLE_SEQUENCE_MUST_BE_GREATER_THAN_ZERO, ProcessInvocationPoint.Validation, sourceComponent, shortDescription);
                }
                else if (genericData.CustomCode04 == ResolveMandatoryConfig(GenericDataType_Deductible_Client) && customNumeric03 > 0)
                {
                    // Otherwise, if this is a Client type deductible, and the DEDUCIBLE POLICY sequence is > 0, it should be zero - raise an error.
                    string shortDescription = this.GetUWValueSetShortDescription(genericData);
                    UWBusinessLogicHelper.AddError(processResults, UwMessageConstants.DEDUCTIBLE_POLICY_SEQUENCE_SHOULD_BE_ZERO, ProcessInvocationPoint.Validation, sourceComponent, shortDescription);
                }

                // If we have a HeaderVersion
                if (headerVersion != null)
                {
                    // Raise error if Deductible Policy Sequence == 1 and no Deductible Policy Reference on the UW Header in Custom Reference 01
                    // likewise for Deductible Policy Sequence a value of 2 to 5, with no custom reference02 to 05 set.
                    if (customNumeric03 == 1 && string.IsNullOrWhiteSpace(headerVersion.CustomReference01))
                    {
                        UWBusinessLogicHelper.AddError(processResults, UwMessageConstants.DEDUCTIBLE_POLICY_REFERENCE_SHOULD_NOT_BE_NULL, ProcessInvocationPoint.Validation, sourceComponent, headerVersion.CustomReference01Field.Title);
                    }
                    else if (customNumeric03 == 2 && string.IsNullOrWhiteSpace(headerVersion.CustomReference02))
                    {
                        UWBusinessLogicHelper.AddError(processResults, UwMessageConstants.DEDUCTIBLE_POLICY_REFERENCE_SHOULD_NOT_BE_NULL, ProcessInvocationPoint.Validation, sourceComponent, headerVersion.CustomReference02Field.Title);
                    }
                    else if (customNumeric03 == 3 && string.IsNullOrWhiteSpace(headerVersion.CustomReference03))
                    {
                        UWBusinessLogicHelper.AddError(processResults, UwMessageConstants.DEDUCTIBLE_POLICY_REFERENCE_SHOULD_NOT_BE_NULL, ProcessInvocationPoint.Validation, sourceComponent, headerVersion.CustomReference03Field.Title);
                    }
                    else if (customNumeric03 == 4 && string.IsNullOrWhiteSpace(headerVersion.CustomReference04))
                    {
                        UWBusinessLogicHelper.AddError(processResults, UwMessageConstants.DEDUCTIBLE_POLICY_REFERENCE_SHOULD_NOT_BE_NULL, ProcessInvocationPoint.Validation, sourceComponent, headerVersion.CustomReference04Field.Title);
                    }
                    else if (customNumeric03 == 5 && string.IsNullOrWhiteSpace(headerVersion.CustomReference05))
                    {
                        UWBusinessLogicHelper.AddError(processResults, UwMessageConstants.DEDUCTIBLE_POLICY_REFERENCE_SHOULD_NOT_BE_NULL, ProcessInvocationPoint.Validation, sourceComponent, headerVersion.CustomReference05Field.Title);
                    }
                }
            }
        }


        /// <summary>
        /// Gets the uw value set short description on the Generic Data Item.
        /// </summary>
        /// <param name="genericData">The generic data.</param>
        /// <returns>Short Description</returns>
        private string GetUWValueSetShortDescription(IGenericDataItem genericData)
        {
            string shortDescription = string.Empty;
            // The CustomCode04Field value gives the setcode the value should be in
            object setcode = ((UwGenericDataItem)genericData).CustomCode04Field.LookupDefinitionKey["SetCode"].Value;
            if (setcode != null)
            {
                int sourceSetCode = 0;
                // Convert the SetCode to an int
                int.TryParse(setcode.ToString(), out sourceSetCode);
                if (sourceSetCode > 0)
                {
                    // We have a setcode so retrieve from the UW Value Set Cache using a MetaData Query
                    shortDescription = UnderwritingValueSetCache.GetValueNames(ObjectFactory.Resolve<IMetadataQuery>(), sourceSetCode, LanguageHelper.GetLanguageId()).Where(code => code.Key == genericData.CustomCode04).FirstOrDefault().Value.ShortDescription;
                }
            }

            return shortDescription;
        }

        /// <summary>
        /// Extracts the generic data items from the given component container type
        /// which will be Header, Section Detail or Coverage
        /// </summary>
        /// <param name="container">The container.</param>
        /// <returns>A list of the Generic Data Items</returns>
        private List<IGenericDataItem> ExtractGenericDataItems(IGenericDataSetContainer container)
        {
            var dataItems = new List<IGenericDataItem>();
            var genericDataSet = container.GetGenericDataSet();
            if (genericDataSet != null)
            {
                dataItems.AddRange(genericDataSet.GenericDataItems);
            }

            return dataItems;
        }
    }
}
