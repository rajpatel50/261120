
namespace Xiap.DataMigration.GeniusInterface.AXACS.Handlers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Claims.BusinessComponent;
    using Framework.Common;
    using GeniusX.AXA.Claims.BusinessLogic;
    using Metadata.BusinessComponent;
    using Metadata.Data.Enums;
    using Newtonsoft.Json.Linq;
    using log4net;
    using Xiap.Framework.Data.Underwriting;

    public class UpdateExcessAndDeductibles
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public bool UpdateEDData(ClaimDetail claimDetail, long? policyCoverageID)
        {
            ClaimHeader claimHeader = claimDetail.ClaimHeader;

            // Extract of E&D data will only occur once. If another uncancelled CD already exists which is linked to policy component, then it is assumed that E&D data has already been retrieved
            if (claimHeader.ClaimDetails.Where(a => a.PolicyLinkLevel > 0 && a.DataId != claimDetail.DataId && a.PolicyCoverageID != null
                && a.ClaimDetailInternalStatus
                != (short?)StaticValues.ClaimDetailInternalStatus.ClosedCreatedinError && a.ClaimDetailInternalStatus != (short?)StaticValues.ClaimDetailInternalStatus.Finalized).Any())
            {
                Logger.WarnFormat("This ClaimDetail does not satisfy the E&D update criteria; there could be an uncancelled CD linked to the Policy component\r\n{0}",
                    JObject.FromObject(new
                    {
                        claimDetail.ClaimHeader.ClaimReference,
                        claimDetail.ClaimDetailReference,
                        PolicyLinkLevel = ((StaticValues.PolicyLinkLevel)claimDetail.PolicyLinkLevel).ToString(),
                        ClaimDetailInternalStatus = ((StaticValues.ClaimDetailInternalStatus)claimDetail.ClaimDetailInternalStatus).ToString()
                    }));
                return false;
            }
            Logger.InfoFormat("Attempting update of Excess and Deductibles values\r\n[\r\n\t{{ClaimRefernece:{0}}}\r\n]", claimHeader.ClaimReference);
            ProductClaimDefinition productClaimDef = claimHeader.GetProduct().ProductClaimDefinition;
            if (productClaimDef.ClaimHeaderAutomaticDeductibleProcessingMethod == (short)StaticValues.ClaimHeaderAutomaticDeductibleProcessingMethod.StandardClaimHeaderDeductible
                && claimHeader.PolicyHeaderID > 0)
            {
                if (policyCoverageID > 0)
                {
                    Logger.InfoFormat("Coverage Link found\r\n{0}\r\n",
                        JObject.FromObject(new { claimHeader.ClaimReference, claimDetail.ClaimDetailReference, policyCoverageID }));
                    bool updateTerms = false;
                    // Retrieve the policy data onto the claim
                    IAXAClaimsQuery query = new AXAClaimsQueries();
                    Logger.InfoFormat("GetPolicyDeductibles query parameters for ClaimReference '{0}'\r\n[\r\n\tcomponentID={1}\r\n\tlinkLevel={2}\r\n\tvehicleType={3}\r\n\tdivision={4}\r\n\treasonCode={5}\r\n\tidentifier={6}\r\n]",
                        claimHeader.ClaimReference, policyCoverageID.Value, claimDetail.PolicyLinkLevel.Value, claimHeader.ClaimHeaderAnalysisCode09, claimHeader.ClaimHeaderAnalysisCode04, claimHeader.ClaimHeaderAnalysisCode08, "AND2");
                    IEnumerable<IGenericDataItem> deductibleGDItems = query.GetPolicyDeductibles(policyCoverageID.Value, claimDetail.PolicyLinkLevel.Value, claimHeader.ClaimHeaderAnalysisCode09, claimHeader.ClaimHeaderAnalysisCode04, claimHeader.ClaimHeaderAnalysisCode08, "AND2");

                    if (deductibleGDItems != null && deductibleGDItems.Count() > 0)
                    {
                        Logger.InfoFormat("Deductibles found\r\n{0}\r\n",
                            JObject.FromObject(new { claimDetail.ClaimDetailReference, DeductibleGenericDataItems = deductibleGDItems.Count() }));
                        // Assumed that ClaimHeader Generic data set for AND4 is set to default on creation
                        string[] policyRefs = new string[5];
                        ClaimGenericDataItem policyRefData = null;
                        ClaimGenericDataSet dataSet = claimHeader.GenericDataSet;
                        if (dataSet == null)
                        {
                            dataSet = (ClaimGenericDataSet)claimHeader.CreateGenericDataSet(true);
                            claimHeader.GenericDataSet = dataSet;

                        }
                        if (dataSet.ClaimGenericDataItems.All(gdi => gdi.GenericDataTypeCode != "AND4"))
                        {
                            IClaimGenericDataSetContainer claimGenericDataSetContainer = claimHeader;
                            var genericDataDefinitionHeader = claimHeader.GetProductGDDefinitionHeader();
                            var genericDataDefinitionDetail =
                                genericDataDefinitionHeader.ProductGDDefinitionDetails.FirstOrDefault(
                                    gd =>
                                    gd.GenericDataTypeCode == "AND4");
                            if (genericDataDefinitionDetail != null)
                            {

                                dataSet.AddGenericDataItem(genericDataDefinitionDetail.ProductGDDefinitionDetailID,
                                                           claimGenericDataSetContainer.GDSParentStartDate);
                                Logger.InfoFormat("Created GDI for AND4\r\n{0}",
                                    JObject.FromObject(new
                                    {
                                        claimHeader.ClaimReference
                                    }));
                            }
                        }
                        policyRefData = dataSet.GenericDataItems.Cast<ClaimGenericDataItem>().FirstOrDefault(a => a.GenericDataTypeCode == "AND4");
                        if (policyRefData != null)
                        {
                            policyRefs = query.GetPolicyReferences(claimHeader.ProposedPolicyReference);
                        }
                        Logger.InfoFormat("Claim has been updated with the following Policy References\r\n{0}",
                            JObject.FromObject(new
                            {
                                claimHeader.ClaimReference,
                                PolicyReferences = policyRefs
                            }));
                        this.UpdateClaimDeductibleData(claimHeader, deductibleGDItems, policyRefData, policyRefs);
                        updateTerms = true;
                    }

                    IEnumerable<IGenericDataItem> excessGDItems = query.GetPolicyDeductibles(policyCoverageID.Value, claimDetail.PolicyLinkLevel.Value, claimHeader.ClaimHeaderAnalysisCode09, claimHeader.ClaimHeaderAnalysisCode04, claimHeader.ClaimHeaderAnalysisCode08, "AND3");

                    if (excessGDItems != null && excessGDItems.Count() > 0)
                    {
                        this.UpdateClaimADExcessData(claimHeader, excessGDItems);
                        updateTerms = true;
                    }

                    if (updateTerms)
                    {
                        this.UpdateTermsData(claimHeader);
                    }
                    else
                    {
                        //if (claimHeader.UWHeader != null && claimHeader.UWHeader.ITerms != null && claimHeader.UWHeader.ITerms.Count > 0)
                        //{
                        //    IUWTerms uwTerm = claimHeader.UWHeader.ITerms.OrderByDescending(x => x.VersionNumber).FirstOrDefault();
                        //if (!string.IsNullOrWhiteSpace(uwTerm.MainOriginalCurrencyCode))
                        //{
                        //    claimHeader.ClaimCurrencyCode = uwTerm.MainOriginalCurrencyCode;
                        //}
                        //else
                        //{
                        claimHeader.ClaimCurrencyCode = ClaimConstants.DEFAULT_CURRENCY_CODE;
                        //}
                        //}
                    }
                }
            }
            return true;
        }

        private void UpdateClaimDeductibleData(ClaimHeader claimHeader, IEnumerable<IGenericDataItem> genericDataItems, ClaimGenericDataItem policyRefData, string[] policyRefs)
        {
            Logger.InfoFormat("Updating E&D data\r\n[\r\n\t{{ClaimReference:{0}}}\r\n]", claimHeader.ClaimReference);
            foreach (IGenericDataItem genericData in genericDataItems)
            {
                if (claimHeader.PolicyAmountCurrencyCodeField.IsInUse.GetValueOrDefault(false))
                {
                    claimHeader.PolicyAmountCurrencyCode = genericData.CurrencyCode;
                }

                if (genericData.CustomNumeric01 == 1 && claimHeader.PolicyDeductible01Field.IsInUse.GetValueOrDefault(false))
                {
                    claimHeader.PolicyDeductible01 = genericData.CustomNumeric02; // Deductible amount
                    claimHeader.IsDeductible01PaidByInsurer = genericData.CustomBoolean03; // Funded
                    claimHeader.CustomBoolean12 = genericData.CustomBoolean02; // Adjustable
                }
                else if (genericData.CustomNumeric01 == 2 && claimHeader.PolicyDeductible02Field.IsInUse.GetValueOrDefault(false))
                {
                    claimHeader.PolicyDeductible02 = genericData.CustomNumeric02; // Deductible amount
                    claimHeader.IsDeductible02PaidByInsurer = genericData.CustomBoolean03; // Funded
                    claimHeader.CustomBoolean12 = genericData.CustomBoolean02; // Adjustable
                }
                else if (genericData.CustomNumeric01 == 3 && claimHeader.PolicyDeductible03Field.IsInUse.GetValueOrDefault(false))
                {
                    claimHeader.PolicyDeductible03 = genericData.CustomNumeric02; // Deductible amount
                    claimHeader.IsDeductible03PaidByInsurer = genericData.CustomBoolean03; // Funded
                    claimHeader.CustomBoolean12 = genericData.CustomBoolean02; // Adjustable
                }
                else if (genericData.CustomNumeric01 == 4 && claimHeader.PolicyDeductible04Field.IsInUse.GetValueOrDefault(false))
                {
                    claimHeader.PolicyDeductible04 = genericData.CustomNumeric02; // Deductible amount
                    claimHeader.IsDeductible04PaidByInsurer = genericData.CustomBoolean03; // Funded
                    claimHeader.CustomBoolean12 = genericData.CustomBoolean02; // Adjustable
                }
                else if (genericData.CustomNumeric01 == 5 && claimHeader.PolicyDeductible05Field.IsInUse.GetValueOrDefault(false))
                {
                    claimHeader.PolicyDeductible05 = genericData.CustomNumeric02; // Deductible amount
                    claimHeader.IsDeductible05PaidByInsurer = genericData.CustomBoolean03;
                    claimHeader.CustomBoolean12 = genericData.CustomBoolean02; // Adjustable
                }

                // copy Policy refs
                if (policyRefData != null)
                {
                    if (genericData.CustomNumeric01 == 1)
                    {
                        policyRefData.CustomCode01 = genericData.CustomCode04;
                        if (genericData.CustomBoolean03.GetValueOrDefault(false) && genericData.CustomNumeric03 != null)
                        {
                            policyRefData.CustomReference01 = policyRefs[(int)genericData.CustomNumeric03 - 1];
                        }
                    }
                    else if (genericData.CustomNumeric01 == 2)
                    {
                        policyRefData.CustomCode02 = genericData.CustomCode04;
                        if (genericData.CustomBoolean03.GetValueOrDefault(false) && genericData.CustomNumeric03 != null)
                        {
                            policyRefData.CustomReference02 = policyRefs[(int)genericData.CustomNumeric03 - 1];
                        }
                    }
                    else if (genericData.CustomNumeric01 == 3)
                    {
                        policyRefData.CustomCode03 = genericData.CustomCode04;
                        if (genericData.CustomBoolean03.GetValueOrDefault(false) && genericData.CustomNumeric03 != null)
                        {
                            policyRefData.CustomReference03 = policyRefs[(int)genericData.CustomNumeric03 - 1];
                        }
                    }
                    else if (genericData.CustomNumeric01 == 4)
                    {
                        policyRefData.CustomCode04 = genericData.CustomCode04;
                        if (genericData.CustomBoolean03.GetValueOrDefault(false) && genericData.CustomNumeric03 != null)
                        {
                            policyRefData.CustomReference04 = policyRefs[(int)genericData.CustomNumeric03 - 1];
                        }
                    }
                    else if (genericData.CustomNumeric01 == 5)
                    {
                        policyRefData.CustomCode05 = genericData.CustomCode04;
                        if (genericData.CustomBoolean03.GetValueOrDefault(false) && genericData.CustomNumeric03 != null)
                        {
                            policyRefData.CustomReference05 = policyRefs[(int)genericData.CustomNumeric03 - 1];
                        }
                    }
                }
            }
        }


        private void UpdateTermsData(ClaimHeader claimHeader)
        {
            IAXAClaimsQuery query = new AXAClaimsQueries();
            IUWTerms term = query.GetlatestTerm(claimHeader.PolicyHeaderID.Value);
            claimHeader.ClaimCurrencyCode = ClaimConstants.DEFAULT_CURRENCY_CODE;
            //if (term != null && !string.IsNullOrEmpty(term.MainOriginalCurrencyCode))
            //{
            //    claimHeader.ClaimCurrencyCode = term.MainOriginalCurrencyCode;
            //}
        }

        private void UpdateClaimADExcessData(ClaimHeader claimHeader, IEnumerable<IGenericDataItem> genericDataItems)
        {
            foreach (IGenericDataItem genericData in genericDataItems)
            {
                if (claimHeader.PolicyAmountCurrencyCodeField.IsInUse.GetValueOrDefault(false))
                {
                    claimHeader.PolicyAmountCurrencyCode = genericData.CurrencyCode;
                }

                // If AD Excess is in use
                if (claimHeader.CustomNumeric10Field.IsInUse.GetValueOrDefault(false))
                {
                    if (claimHeader.ClaimHeaderAnalysisCode02 == "31")
                    {
                        claimHeader.CustomNumeric10 = genericData.CustomNumeric01; // Accident
                    }
                    else if (claimHeader.ClaimHeaderAnalysisCode02 == "32")
                    {
                        claimHeader.CustomNumeric10 = genericData.CustomNumeric02; // Fire
                    }
                    else if (claimHeader.ClaimHeaderAnalysisCode02 == "33")
                    {
                        claimHeader.CustomNumeric10 = genericData.CustomNumeric04; // Storm (Natural Event)
                    }
                    else if (claimHeader.ClaimHeaderAnalysisCode02 == "34")
                    {
                        claimHeader.CustomNumeric10 = genericData.CustomNumeric03; // Theft
                    }
                    else if (claimHeader.ClaimHeaderAnalysisCode02 == "35")
                    {
                        claimHeader.CustomNumeric10 = genericData.CustomNumeric05; // Vandalism
                    }
                    else if (claimHeader.ClaimHeaderAnalysisCode02 == "36")
                    {
                        claimHeader.CustomNumeric10 = genericData.CustomNumeric06; // Windscreen
                    }
                    else if (!string.IsNullOrEmpty(claimHeader.ClaimHeaderAnalysisCode02))
                    {
                        claimHeader.CustomNumeric10 = genericData.CustomNumeric07; // Other
                    }

                    claimHeader.CustomBoolean15 = genericData.CustomBoolean03; // Funded
                }
            }
        }
    }
}