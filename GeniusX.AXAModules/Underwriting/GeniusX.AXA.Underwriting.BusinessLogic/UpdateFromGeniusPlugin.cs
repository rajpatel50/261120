using System;
using System.Linq;
using System.ServiceModel;
using Xiap.Framework;
using Xiap.Framework.Common.Codes;
using Xiap.Framework.Common.Product;
using Xiap.Framework.Data;
using Xiap.Framework.Data.Underwriting;
using Xiap.Framework.Logging;
using Xiap.Framework.Messages;
using Xiap.Framework.ProcessHandling;
using Xiap.Metadata.BusinessComponent;
using Xiap.Metadata.Data;
using Xiap.Metadata.Data.Enums;
using Xiap.UW.BusinessComponent;
using System.Collections.Generic;

namespace GeniusX.AXA.Underwriting.BusinessLogic
{
    /// <summary>
    /// This creates a Policy in Xuber using data from Genius.
    /// Invoked when a user clicks the Update From Genius button in the Business UI when creating a Policy in Xuber.
    /// </summary>
    public class UpdateFromGeniusPlugin : AbstractComponentPlugin
    {
        private static readonly ILogger _logger = LoggerFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private readonly ISystemValueSetQuery<ISectionDetailTypeData> sectionDetailTypeValueSetCategory;
        private readonly IEnumerable<ISectionDetailTypeData> sectionDetailTypes;
        public UpdateFromGeniusPlugin(ISystemValueSetQuery<ISectionDetailTypeData> sectionDetailTypeValueSetCategory)
        {
            this.sectionDetailTypeValueSetCategory = sectionDetailTypeValueSetCategory;
            this.sectionDetailTypes = sectionDetailTypeValueSetCategory.GetValues();
        }

        /// <summary>
        /// Processes the component in the Virtual Invocation Point
        /// </summary>
        /// <param name="component">The component.</param>
        /// <param name="point">The point.</param>
        /// <param name="pluginId">The plugin identifier.</param>
        /// <returns>Process Results Collection</returns>
        public override ProcessResultsCollection ProcessComponent(IBusinessComponent component, ProcessInvocationPoint point, int pluginId)
        {
            ProcessResultsCollection results = new ProcessResultsCollection();
            Header header = (Header)component;
            if (point == ProcessInvocationPoint.Virtual)
            {
                if (this.ValidateXiapHeader(header, results).Count == 0)
                {
                    // Count is 0, i.e. no errors added to the ProcessResultsCollection 'results' in the Validation the GeniusX policy, so update
                    this.UpdateFromGenius(header, results);
                }
            }

            return results;
        }

        /// <summary>
        /// Updates the header from genius.
        /// </summary>
        /// <param name="xiapHeader">The xiap header.</param>
        /// <param name="results">The results.</param>
        /// <exception cref="System.ServiceModel.FaultException">new FaultCode(Sender)</exception>
        /// <exception cref="FaultReason"></exception>
        /// <exception cref="FaultCode">Sender</exception>
        private void UpdateFromGenius(Header xiapHeader, ProcessResultsCollection results)
        {
            try
            {
                IUWHeader externalHeader = null;
                IPolicyService riskService = null;
                // Create an external policy service to the external service linked on the Header product.
                // This external service will be Genius.
                riskService = new PolicyService(xiapHeader.GetProduct().Product.ExternalDataSource);
                externalHeader = riskService.GetPolicy(xiapHeader.HeaderReference);
                if (this.ValidateExternalHeader(xiapHeader, externalHeader, results).Count == 0)
                {
                    // Count is 0, i.e. no errors added to the ProcessResultsCollection 'results' in the Validation of External Header
                    this.UpdateHeader(xiapHeader, externalHeader);

                    // Apply the external policy terms to the GeniusX policy.
                    foreach (IUWTerms externalTerm in externalHeader.ITerms)
                    {
                        this.ProcessTerms(xiapHeader, externalTerm);
                    }

                    // Order the external policy sections by the External Reference, which will be a Genius Reference,
                    // thus the sections will be ordered as they are in the Genius policy.
                    // Then apply each to the GeniusX policy in turn.
                    var sections = externalHeader.ISections.OrderBy(x => x.ExternalReference);
                    foreach (IUWSection externalSection in sections)
                    {
                        this.ProcessSection(xiapHeader, externalSection);
                    }

                    // Finally, update NameInvolvements from the external Genius policy
                    this.UpdateNameInvolvement(xiapHeader, externalHeader);
                }
            }
            catch (Exception e)
            {
                if (_logger.IsErrorEnabled)
                {
                    _logger.Error(e);
                }

                throw new FaultException(new FaultReason(e.Message), new FaultCode("Sender"));
            }
        }

        /// <summary>
        /// Validates GeniusX policy can be updated from Genius.
        /// </summary>
        /// <param name="header">The header.</param>
        /// <param name="results">The results.</param>
        /// <returns>Process Results Collection</returns>
        private ProcessResultsCollection ValidateXiapHeader(Header header, ProcessResultsCollection results)
        {
            // If status is verified, creation of components from Genius not allowed
            if (header.HeaderStatusThreshold >= 30)
            {
                UWBusinessLogicHelper.AddError(results, UwMessageConstants.HEADERSTATUS_VERIFIED_GENIUSUPDATE_NOTALLOWED, ProcessInvocationPoint.Virtual, header);
            }

            // We can't collect from Genius if we haven't defined the product as actually using an external source (which should be Genius).
            if (header.GetProduct().Product.ExternalDataSource == null)
            {
                UWBusinessLogicHelper.AddError(results, UwMessageConstants.EXTERNALDATASOURCE_REQUIRED, ProcessInvocationPoint.Virtual, header);
            }

            // Note that core validation already checks duplicate XIAP header references, so shouldn't have to re-check
            
            return results;
        }

        /// <summary>
        /// Validates the external Genius Policy can be used to refresh the GeniusX policy.
        /// </summary>
        /// <param name="xiapheader">The xiapheader.</param>
        /// <param name="externalheader">The externalheader.</param>
        /// <param name="results">The results.</param>
        /// <returns>Process Results Collection</returns>
        private ProcessResultsCollection ValidateExternalHeader(Header xiapheader, IUWHeader externalheader, ProcessResultsCollection results)
        {
            // Genius policy not found
            if (externalheader == null)
            {
                UWBusinessLogicHelper.AddError(results, MessageConstants.INVALID_POLICY_REFERENCE, ProcessInvocationPoint.Virtual, xiapheader);
            }
            else
            {
                // Genius policy product must match XIAP product
                if (externalheader.ProductCode != xiapheader.GetProduct().Product.Code)
                {
                    UWBusinessLogicHelper.AddError(results, UwMessageConstants.GENIUS_PRODUCT_DOES_NOT_MATCH, ProcessInvocationPoint.Virtual, xiapheader);
                }
            }

            return results;
        }

        /// <summary>
        /// Gets the section matching the external Genius reference.
        /// </summary>
        /// <param name="xiapHeader">The xiap header.</param>
        /// <param name="externalReference">The external reference.</param>
        /// <returns>The section version or null</returns>
        private IUWSection GetSectionForExternalRef(Header xiapHeader, string externalReference)
        {
            SectionVersion sv = null;
            Section section = xiapHeader.Sections.Where(s => s.ExternalReference == externalReference).FirstOrDefault();
            if (section != null)
            {
                sv = (SectionVersion)section.GetLatestVersion();
            }

            return sv;
        }

        /// <summary>
        /// Gets the section detail matching the external Genius reference.
        /// </summary>
        /// <param name="xiapSection">The xiap section.</param>
        /// <param name="externalReference">The external reference.</param>
        /// <returns>The section detail version or null</returns>
        private IUWSectionDetail GetSectionDetailForExternalRef(Section xiapSection, string externalReference)
        {
            SectionDetailVersion sdv = null;
            SectionDetail sectionDetail = xiapSection.SectionDetails.Where(sd => sd.ExternalReference == externalReference).FirstOrDefault();
            if (sectionDetail != null)
            {
                sdv = (SectionDetailVersion)sectionDetail.GetLatestVersion();
            }
  
            return sdv;
        }

        /// <summary>
        /// Gets the coverage mnatching the external Genius  reference.
        /// </summary>
        /// <param name="xiapSectionDetail">The xiap section detail.</param>
        /// <param name="externalReference">The external reference.</param>
        /// <returns>The coverage version or null</returns>
        private IUWCoverage GetCoverageForExternalRef(SectionDetail xiapSectionDetail, string externalReference)
        {
            CoverageVersion cv = null;
            Coverage coverage = xiapSectionDetail.Coverages.Where(c => c.ExternalReference == externalReference).FirstOrDefault();
            if (coverage != null)
            {
                cv = (CoverageVersion)coverage.GetLatestVersion();
            }

            return cv;
        }

        /// <summary>
        /// Updates the header title, inception and expiry dates with the values from the Genius Policy header.
        /// </summary>
        /// <param name="xiapHeader">The xiap header.</param>
        /// <param name="externalHeader">The external header.</param>
        private void UpdateHeader(Header xiapHeader, IUWHeader externalHeader)
        {
            xiapHeader.ExternalReference = externalHeader.ExternalReference;
            HeaderVersion headerVersion = (HeaderVersion)xiapHeader.GetLatestVersion();
            if (headerVersion != null)
            {
                headerVersion.HeaderTitle = externalHeader.HeaderTitle;
                headerVersion.InceptionDate = externalHeader.InceptionDate;
                headerVersion.ExpiryDate = externalHeader.ExpiryDate;
            }
        }

        /// <summary>
        /// Updates the section passed in with the title from the Genius external policy section.
        /// </summary>
        /// <param name="xiapSection">The xiap section.</param>
        /// <param name="externalSection">The external section.</param>
        private void UpdateSection(Section xiapSection, IUWSection externalSection)
        {
            xiapSection.ExternalReference = externalSection.ExternalReference;
            SectionVersion sectionVersion = (SectionVersion)xiapSection.GetLatestVersion();
            if (sectionVersion != null)
            {
                sectionVersion.SectionTitle = externalSection.SectionTitle;
            }
        }

        /// <summary>
        /// Updates the coverage passed in with the title from the Genius external policy coverage.
        /// </summary>
        /// <param name="xiapCoverage">The xiap coverage.</param>
        /// <param name="externalCoverage">The external coverage.</param>
        private void UpdateCoverage(Coverage xiapCoverage, IUWCoverage externalCoverage)
        {
            xiapCoverage.ExternalReference = externalCoverage.ExternalReference;
            CoverageVersion coverageVersion = (CoverageVersion)xiapCoverage.GetLatestVersion();
            if (coverageVersion != null)
            {
                coverageVersion.CoverageTitle = externalCoverage.CoverageTitle;
            }
        }

        /// <summary>
        /// Updates the section detail passed in with the title from the external Genius policy section detail.
        /// </summary>
        /// <param name="xiapSectiondetail">The xiap sectiondetail.</param>
        /// <param name="externalSectionDetail">The external section detail.</param>
        private void UpdateSectionDetail(SectionDetail xiapSectiondetail, IUWSectionDetail externalSectionDetail)
        {
            xiapSectiondetail.ExternalReference = externalSectionDetail.ExternalReference;
            SectionDetailVersion secdetailVersion = (SectionDetailVersion)xiapSectiondetail.GetLatestVersion();
            if (secdetailVersion != null)
            {
                secdetailVersion.SectionDetailTitle = externalSectionDetail.SectionDetailTitle;
            }
        }

        /// <summary>
        /// Updates the Major Insured and Major Broker name involvements on the GeniusX policy, from the Genius Policy.
        /// </summary>
        /// <param name="xiapHeader">The xiap header.</param>
        /// <param name="externalHeader">The external header.</param>
        private void UpdateNameInvolvement(Header xiapHeader, IUWHeader externalHeader)
        {
            // Update Insured from Genius policy
            var insured = externalHeader.IUWNameInvolvements.Where(a => a.NameInvolvementType == (short)StaticValues.NameInvolvementType.MajorInsured).FirstOrDefault();
            UwNameInvolvement ni = null;
            if (insured != null)
            {
                // we have an insured on the Genius Policy so see if we have one on the GeniusX policy.
                ni = xiapHeader.UwNameInvolvements.Where(a => a.NameInvolvementType == (short)StaticValues.NameInvolvementType.MajorInsured).FirstOrDefault();
                if (ni != null)
                {
                    // Update the Name id
                    ni.NameID = insured.NameID;
                }
                else
                {
                    // Check if Insured NI is configured on product and can be created
                    // If so, create and update from Genius
                    long productVersionID = xiapHeader.GetProduct().ProductVersionID;
                    var productNI = ProductService.GetProductNameInvolvementQuery().GetProductNameInvolvementByNameInvolvementType(productVersionID, (short)StaticValues.NameInvolvementType.MajorInsured);
                    if (productNI != null)
                    {
                        ni = xiapHeader.AddNewUwNameInvolvement((short)StaticValues.NameInvolvementType.MajorInsured, insured.NameID);
                        ni.NameUsageTypeCode = insured.NameUsageTypeCode;
                    }
                }
            }


            // Update LossBroker from Genius policy
            var mjrBroker = externalHeader.IUWNameInvolvements.Where(a => a.NameInvolvementType == (short)StaticValues.NameInvolvementType.MajorBroker).FirstOrDefault();
            UwNameInvolvement nameInvMajorBroker = null;
            if (mjrBroker != null)
            {
                // If we have a Major Broker on the Genius Policy, find one on the GeniusX policy
                var nameInvMajorBrokerVersion = xiapHeader.UwNameInvolvements.Where(a => a.NameInvolvementType == (short)StaticValues.NameInvolvementType.MajorBroker).SelectMany(uni => uni.UwNameInvolvementVersion.Where(lv => lv.IsLatestVersion == true)).FirstOrDefault();

                if (nameInvMajorBrokerVersion != null)
                {
                    // Get the name involvement from the lastest version.
                    nameInvMajorBroker = nameInvMajorBrokerVersion.UwNameInvolvement;
                }

                if (nameInvMajorBroker != null)
                {
                    // Update the Name id already existing on the GeniusX policy
                    nameInvMajorBroker.NameID = mjrBroker.NameID;
                }
                else
                {
                    // Check if Major Broker NI is configured on product and can be created
                    // Create it, if so, and apply Genius policy data.
                    long productVersionID = xiapHeader.GetProduct().ProductVersionID;
                    var productNI = ProductService.GetProductNameInvolvementQuery().GetProductNameInvolvementByNameInvolvementType(productVersionID, (short)StaticValues.NameInvolvementType.MajorInsured);
                    if (productNI != null)
                    {
                        nameInvMajorBroker = xiapHeader.AddNewUwNameInvolvement((short)StaticValues.NameInvolvementType.MajorBroker, mjrBroker.NameID);
                        nameInvMajorBroker.NameUsageTypeCode = mjrBroker.NameUsageTypeCode;
                    }
                }
            }
        }

        /// <summary>
        /// Processes the section from the external policy, applying it to the GeniusX policy.
        /// </summary>
        /// <param name="xiapHeader">The xiap header.</param>
        /// <param name="externalSection">The external section.</param>
        private void ProcessSection(Header xiapHeader, IUWSection externalSection)
        {
            Section section = null;
            SectionVersion sectionVersion = (SectionVersion)this.GetSectionForExternalRef(xiapHeader, externalSection.ExternalReference);
            if (sectionVersion == null)
            {
                // No section already exists on the GeniusX policy that matches the External Reference so we create one.
                section = xiapHeader.AddNewSection(externalSection.SectionTypeCode, externalSection.SubSectionTypeCode);
                this.UpdateSection(section, externalSection);

                // Add Summary SD if not already added and product allows it
                if (this.IsSummerySectionDetailAllowed(section.GetProduct().ProductSectionID))
                {
                    section.AddNewSectionDetail(section.GetProduct().ProductSectionDetails.Where(p=> this.sectionDetailTypes.Where(s=> s.Code == p.SectionDetailTypeCode).FirstOrDefault().IsSummarySectionDetailType == true).First().SectionDetailTypeCode,false);
                }
             }
            else
            {
                // map the section to the correct one from the GeniusX policy, retrieved via the Genius policy external reference
                section = sectionVersion.Section;
            }

            // Get all the external Section Details for this Section, ordered by the Genius Reference   
            var sectiondetails = externalSection.ISectionDetails.OrderBy(x => x.ExternalReference);
            foreach (IUWSectionDetail externalSectionDetail in sectiondetails)
            {
                this.ProcessSectionDetail(section, externalSectionDetail);
            }
        }

        /// <summary>
        /// Processes the section detail from the external policy, applying it to the GeniusX policy.
        /// </summary>
        /// <param name="xiapSection">The xiap section.</param>
        /// <param name="externalSectionDetail">The external section detail.</param>
        private void ProcessSectionDetail(Section xiapSection, IUWSectionDetail externalSectionDetail)
        {
            SectionDetail sectionDetail = null;
            SectionDetailVersion sectionDetailVersion = (SectionDetailVersion)this.GetSectionDetailForExternalRef(xiapSection, externalSectionDetail.ExternalReference);
            if (sectionDetailVersion == null)
            {
                // No section detail already exists on the GeniusX policy that matches the External Reference so we create one.
                sectionDetail = xiapSection.AddNewSectionDetail(externalSectionDetail.SectionDetailTypeCode, false);
                this.UpdateSectionDetail(sectionDetail, externalSectionDetail);
            }
            else
            {
                // map the section detail to the correct one from the GeniusX policy, retrieved via the Genius policy external reference
                sectionDetail = sectionDetailVersion.SectionDetail;
            }

            // Get all the external Coverages for this Section Detail, ordered by the Genius Reference
            var coverages = externalSectionDetail.ICoverages.OrderBy(x => x.ExternalReference);
            foreach (IUWCoverage externalCoverage in coverages)
            {
                this.ProcessCoverage(sectionDetail, externalCoverage);
            }
        }

        /// <summary>
        /// Processes the coverage.
        /// </summary>
        /// <param name="xiapSectionDetail">The xiap section detail.</param>
        /// <param name="externalCoverage">The external coverage.</param>
        private void ProcessCoverage(SectionDetail xiapSectionDetail, IUWCoverage externalCoverage)
        {
            Coverage coverage = null;
            CoverageVersion coverageVersion = (CoverageVersion)this.GetCoverageForExternalRef(xiapSectionDetail, externalCoverage.ExternalReference);
            if (coverageVersion == null)
            {
                // No Coverage already exists on the GeniusX policy that matches the External Reference so we create one, via the Product definition.
                ProductCoverage productCoverage = ObjectFactory.Resolve<IMetadataQuery>().GetProductCoverage(xiapSectionDetail.ProductSectionDetailID.Value, externalCoverage.CoverageTypeCode);
                if (productCoverage != null)
                {
                    coverage = xiapSectionDetail.AddNewCoverage(productCoverage.ProductCoverageID);
                    this.UpdateCoverage(coverage, externalCoverage);
                }
            }
        }

        /// <summary>
        /// Processes the terms on the external policy, adding each to the GeniusX policy in turn.
        /// </summary>
        /// <param name="xiapHeader">The xiap header.</param>
        /// <param name="externalTerms">The external terms.</param>
        private void ProcessTerms(Header xiapHeader, IUWTerms externalTerms)
        {
            // Get the current terms on the GeniusX policy and the Product Version.
            Terms terms = xiapHeader.Terms.FirstOrDefault();
            long productVersionID = xiapHeader.GetProduct().ProductVersionID;
            if (terms == null)
            {
                // We have no terms on the policy at all, so first get the default terms from the product.
                using (MetadataEntities metadata = MetadataEntitiesFactory.GetMetadataEntities())
                {
                    ProductTerms productTerms = metadata.ProductTerms.Where(a => a.ProductVersion.ProductVersionID == productVersionID && a.IsDefault == true).FirstOrDefault();
                    if (productTerms != null)
                    {
                        // Map default terms to the GeniusX policy
                        terms = xiapHeader.AddNewTerms(productTerms.ProductTermsID);
                    }
                }
            }

            // Note that this isn't an 'else if'. We should always have terms at this point, and now we map the external data onto them.
            if (terms != null)
            {
                // Update terms on GeniusX policy with the required data from the External policy.
                terms.ExternalReference = externalTerms.ExternalReference;
                TermsVersion termsVersion = (TermsVersion)terms.GetLatestVersion();
                if (termsVersion != null)
                {
                    termsVersion.TermsTitle = externalTerms.TermsTitle;
                    termsVersion.MainOriginalCurrencyCode = externalTerms.MainOriginalCurrencyCode;
                }
            }
        }

        private bool IsSummerySectionDetailAllowed(long sectionID)
        {
            return ObjectFactory.Resolve<IProductSectionQuery>().IsSummarySectionDetailAllowed(sectionID);
        }
    }
}
