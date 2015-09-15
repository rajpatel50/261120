using System.Collections.Generic;
using System.Linq;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.Common;
using Xiap.Framework.Data.Underwriting;
using Xiap.Framework.ProcessHandling;
using Xiap.Metadata.BusinessComponent;
using Xiap.Metadata.Data.Enums;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    /// <summary>
    /// If the reference to the attached Policy on the Claim Detail changes—on Coverage attachment—updates are made to the Deductible data and Terms data.
    /// </summary>
    public class UpdateEDDataPlugin : AbstractComponentPlugin
    {
        public override ProcessResultsCollection ProcessComponent(IBusinessComponent component, ProcessInvocationPoint point, int pluginId, ProcessParameters processParameters)
        {
            return null;
        }

        public override ProcessResultsCollection PropertyChange(IBusinessComponent component, ProcessInvocationPoint point, string propertyName, object oldValue, object newValue, int pluginId)
        {
            if (propertyName == ClaimDetail.PolicyCoverageIDFieldName  && newValue != null)
            {
                this.UpdateEDData(component as ClaimDetail, (long)newValue);
            }

            return null;
        }

        /// <summary>
        /// Update Excess and  Deductable data from Genius 
        /// </summary>
        /// <param name="claimDetail">claim Detail</param>
        /// <param name="policyCoverageID">Policy Coverage ID</param>
        private void UpdateEDData(ClaimDetail claimDetail, long? policyCoverageID)
        {
            ClaimHeader claimHeader = claimDetail.ClaimHeader;

            // Extract of E&D data will only occur once. If another uncancelled CD already exists which is linked to policy component, then it is assumed that E&D data has already been retrieved
            if (claimHeader.ClaimDetails.Where(a => a.PolicyLinkLevel > 0 && a.DataId != claimDetail.DataId && a.PolicyCoverageID!=null 
                && a.ClaimDetailInternalStatus
                != (short?)StaticValues.ClaimDetailInternalStatus.ClosedCreatedinError && a.ClaimDetailInternalStatus != (short?)StaticValues.ClaimDetailInternalStatus.Finalized).Any())
            {
                return;
            }
            
            ProductClaimDefinition productClaimDef = claimHeader.GetProduct().ProductClaimDefinition;
            if (productClaimDef.ClaimHeaderAutomaticDeductibleProcessingMethod == (short)StaticValues.ClaimHeaderAutomaticDeductibleProcessingMethod.StandardClaimHeaderDeductible
                && claimHeader.PolicyHeaderID > 0)
            {
                if (policyCoverageID > 0)
                {
                    bool updateTerms = false;
                    // Retrieve the policy data onto the claim
                    IAXAClaimsQuery query = new AXAClaimsQueries();
                    IEnumerable<IGenericDataItem> deductibleGDItems = query.GetPolicyDeductibles(policyCoverageID.Value, claimDetail.PolicyLinkLevel.Value, claimHeader.ClaimHeaderAnalysisCode09, claimHeader.ClaimHeaderAnalysisCode04, claimHeader.ClaimHeaderAnalysisCode08, "AND2");
                    if (deductibleGDItems != null && deductibleGDItems.Count() > 0)
                    {
                        // Assumed that ClaimHeader Generic data set for AND4 is set to default on creation
                        string[] policyRefs = new string[5];
                        ClaimGenericDataItem policyRefData = null;
                        if (claimHeader.GenericDataSet != null)
                        {
                            policyRefData = (ClaimGenericDataItem)claimHeader.GenericDataSet.GenericDataItems.Where(a => a.GenericDataTypeCode == "AND4").FirstOrDefault();
                            if (policyRefData != null)
                            {
                                policyRefs = query.GetPolicyReferences(claimHeader.ProposedPolicyReference);
                            }
                        }

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
                        if (claimHeader.UWHeader != null && claimHeader.UWHeader.ITerms != null && claimHeader.UWHeader.ITerms.Count > 0)
                        {
                            IUWTerms uwTerm = claimHeader.UWHeader.ITerms.OrderByDescending(x => x.VersionNumber).FirstOrDefault();
                            if (!string.IsNullOrWhiteSpace(uwTerm.MainOriginalCurrencyCode))
                            {
                                claimHeader.ClaimCurrencyCode = uwTerm.MainOriginalCurrencyCode;
                            }
                            else
                            {
                                claimHeader.ClaimCurrencyCode = ClaimConstants.DEFAULT_CURRENCY_CODE;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Update Claim DeDucatble Data from Genius
        /// </summary>
        /// <param name="claimHeader">Claim Header</param>
        /// <param name="genericDataItems">Generic Data Items</param>
        /// <param name="policyRefData">Policy Ref Data</param>
        /// <param name="policyRefs">Policy Refs</param>
        private void UpdateClaimDeductibleData(ClaimHeader claimHeader, IEnumerable<IGenericDataItem> genericDataItems, ClaimGenericDataItem policyRefData, string[] policyRefs)
        {
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
                        policyRefData.CustomCode01 = genericData.CustomCode04;   // UI Label = Deductiple Type 1; GDT - AND4
                        if (genericData.CustomBoolean03.GetValueOrDefault(false) && genericData.CustomNumeric03 != null)
                        {
                            policyRefData.CustomReference01 = policyRefs[(int)genericData.CustomNumeric03 - 1];   // UI Label = Deductiple Pol Ref 1; GDT - AND4
                        }
                    }
                    else if (genericData.CustomNumeric01 == 2)
                    {
                        policyRefData.CustomCode02 = genericData.CustomCode04;   // UI Label = Deductiple Type 2; GDT - AND4
                        if (genericData.CustomBoolean03.GetValueOrDefault(false) && genericData.CustomNumeric03 != null)
                        {
                            policyRefData.CustomReference02 = policyRefs[(int)genericData.CustomNumeric03 - 1];   // UI Label = Deductiple Pol Ref 2; GDT - AND4
                        }
                    }
                    else if (genericData.CustomNumeric01 == 3)
                    {
                        policyRefData.CustomCode03 = genericData.CustomCode04;   // UI Label = Deductiple Type 3; GDT - AND4
                        if (genericData.CustomBoolean03.GetValueOrDefault(false) && genericData.CustomNumeric03 != null)
                        {
                            policyRefData.CustomReference03 = policyRefs[(int)genericData.CustomNumeric03 - 1];   // UI Label = Deductiple Pol Ref 3; GDT - AND4
                        }
                    }
                    else if (genericData.CustomNumeric01 == 4)
                    {
                        policyRefData.CustomCode04 = genericData.CustomCode04;   // UI Label = Deductiple Type 4; GDT - AND4
                        if (genericData.CustomBoolean03.GetValueOrDefault(false) && genericData.CustomNumeric03 != null)
                        {
                            policyRefData.CustomReference04 = policyRefs[(int)genericData.CustomNumeric03 - 1];   // UI Label = Deductiple Pol Ref 4; GDT - AND4
                        }
                    }
                    else if (genericData.CustomNumeric01 == 5)
                    {
                        policyRefData.CustomCode05 = genericData.CustomCode04;   // UI Label = Deductiple Type 5; GDT - AND4
                        if (genericData.CustomBoolean03.GetValueOrDefault(false) && genericData.CustomNumeric03 != null)
                        {
                            policyRefData.CustomReference05 = policyRefs[(int)genericData.CustomNumeric03 - 1];   // UI Label = Deductiple Pol Ref 5; GDT - AND4
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Update Term Data Claim Currency Code From Genius
        /// </summary>
        /// <param name="claimHeader">Claim Header</param>
        private void UpdateTermsData(ClaimHeader claimHeader)
        {
            IAXAClaimsQuery query = new AXAClaimsQueries();
            IUWTerms term = query.GetlatestTerm(claimHeader.PolicyHeaderID.Value);
            if (term != null && !string.IsNullOrEmpty(term.MainOriginalCurrencyCode))
            {
                claimHeader.ClaimCurrencyCode = term.MainOriginalCurrencyCode;
            }
        }

        /// <summary>
        /// Update Claim AD Excess Data from Genius
        /// </summary>
        /// <param name="claimHeader">Claim Header</param>
        /// <param name="genericDataItems">Generic Data Items</param>
        private void UpdateClaimADExcessData(ClaimHeader claimHeader, IEnumerable<IGenericDataItem> genericDataItems)
        {
            foreach (IGenericDataItem genericData in genericDataItems)
            {
                if (claimHeader.PolicyAmountCurrencyCodeField.IsInUse.GetValueOrDefault(false))
                {
                    claimHeader.PolicyAmountCurrencyCode = genericData.CurrencyCode;
                }

                // If AD Excess is in use
                // UI Label = Excess
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
