using System;
using System.Linq;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.Metadata;
using Xiap.Framework.ProcessHandling;
using Xiap.Metadata.BusinessComponent;
using Xiap.Metadata.Data.Enums;
using Xiap.Framework.Common.Product;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    /// <summary>
    /// Sets the Created By field (CustomReference05) on the Claim Header to the identity of the User creating the Claim.
    /// </summary>
    public class ClaimHeaderPlugin : AbstractComponentPlugin
    {
        /// <summary>
        /// Call on ClaimHeader 
        /// </summary>
        /// <param name="component">Claim Header</param>
        /// <param name="point">Points Create,ComponentChange</param>
        /// <param name="pluginId">PlugIN ID</param>
        /// <returns>return ProcessResultCollection which have Error msg If any validation failed.</returns>
        public override ProcessResultsCollection ProcessComponent(IBusinessComponent component, ProcessInvocationPoint point, int pluginId)
        {
            PluginHelper<IBusinessComponent> pluginHelper = new PluginHelper<IBusinessComponent>(point, (ClaimHeader)component, new ProcessResultsCollection());

            switch (pluginHelper.InvocationPoint)
            {
                case ProcessInvocationPoint.Created:
                    this.SetCreatedBy(pluginHelper.Component as ClaimHeader);
                    break;
                case ProcessInvocationPoint.ComponentChange:
                    this.AddDeductibleExcessEvent(pluginHelper.Component as ClaimHeader);
                    break;
            }

            return pluginHelper.ProcessResults;
        }

        /// <summary>
        /// Call on Property change of specific Property
        /// </summary>
        /// <param name="component">Claim Header</param>
        /// <param name="point">Point PropertyChange</param>
        /// <param name="propertyName">Property Name</param>
        /// <param name="oldValue">old value</param>
        /// <param name="newValue">new Value</param>
        /// <param name="pluginId">PlugIN ID</param>
        /// <returns>return ProcessResultCollection which have Error msg If any validation failed.</returns>
        public override ProcessResultsCollection PropertyChange(IBusinessComponent component, ProcessInvocationPoint point, string propertyName, object oldValue, object newValue, int pluginId)
        {
            PluginHelper<ClaimHeader> pluginHelper = new PluginHelper<ClaimHeader>(point, (ClaimHeader)component);
            ClaimHeader claimHeader = pluginHelper.Component;

            // CustomBoolean15 = Funded?
            // If Funded? setting (CustomBoolean15) is being changed, sets IsDeductible01PaidByInsurer on the Accidental Damage Claim Detail accordingly.
            // UI Label = Funded?; Only used for Motor Product - CGBIMO
            // else if UI Label = Excess; Only used for Motor Product - CGBIMO
            // else if UI Label = Client Reference
            if (propertyName == ClaimHeader.CustomBoolean15FieldName)   
            {
                ClaimDetail claimDetail = this.GetADClaimDetail(claimHeader);

                if (claimDetail != null)
                {
                    claimDetail.IsDeductible01PaidByInsurer = Convert.ToBoolean(newValue);
                }
            }
            else if (propertyName == ClaimHeader.CustomNumeric10FieldName)   
            {
                // If Excess value (CustomNumeric10) is being changed, sets PolicyDeductible01 value on the Accidental Damage Claim Detail accordingly.
                // CustomNumeric10 = Excess
                ClaimDetail claimDetail = this.GetADClaimDetail(claimHeader);
                
                if (claimDetail != null)
                {
                    claimDetail.PolicyDeductible01 = Convert.ToDecimal(newValue);
                }
            }
            else if (propertyName == ClaimHeader.CustomReference01FieldName)
            {
                // If the ClientReference value (CustomReference01) is being changed, sets the Reference field (CustomReference01) of the Claim Name Involvement 
                // for the Major Assured to the new value. 
                // CustomReference01= ClientReference
                ClaimNameInvolvement ni = (ClaimNameInvolvement)claimHeader.NameInvolvements.Where(a => a.NameID != null && a.NameInvolvementType == (short)StaticValues.NameInvolvementType.MajorInsured && ((ClaimNameInvolvement)a).NameInvolvementMaintenanceStatus == (short)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest).FirstOrDefault();
 
                if (ni != null)
                {
                    // CustomReference01 = Reference
                    ni.CustomReference01 = (newValue==null) ? string.Empty: newValue.ToString();   // UI Label = Reference; Reference common to all named involvements
                }
            }

            return pluginHelper.ProcessResults;
        }

        /// <summary>
        /// Get  AD Claim Detail   
        /// </summary>
        /// <param name="claimHeader">Claim Header</param>
        /// <returns>Claim Detail</returns>
        private ClaimDetail GetADClaimDetail(ClaimHeader claimHeader)
        {
            return claimHeader.ClaimDetails
                    .Where(a => a.GetProduct().ClaimDetailAutomaticDeductibleProcessingMethod == (short)StaticValues.ClaimDetailAutomaticDeductibleProcessingMethod.StandardClaimDetailDeductible
                    && a.IsAutomaticDeductibleProcessingApplied == true && a.ClaimDetailStatusCode == ClaimConstants.CLAIM_DETAIL_OPEN && a.ClaimDetailTypeCode == ClaimConstants.CLAIM_DETAIL_TYPE_AD).SingleOrDefault();
        }

        /// <summary>
        /// call on field retrival
        /// </summary>
        /// <param name="component">Claim Header</param>
        /// <param name="point">Points FieldRetrieval</param>
        /// <param name="field">Specified Field</param>
        /// <param name="pluginId">Plugin ID</param>
        /// <returns>return ProcessResultCollection which have Error msg If any validation failed.</returns>
        public override ProcessResultsCollection FieldRetrieval(IBusinessComponent component, ProcessInvocationPoint point, ref Field field, int pluginId)
        {
            AXAPolicyEDData data;
            if (this.TryResolvePolicyEDData(component, out data) && (data.DeductiblesExist || data.EDExcessExist))
            {
                // CustomNumeric10 = Exces ,CustomBoolean15 = Funded? , CustomBoolean12 =Deducts Adjustable?
                // If the Excess value has changed sets the Excess Overridden setting (CustomBoolean15) to true and creates a corresponding Event.
                // UI Label = Excess; Only used for Motor Product - CGBIMO &&
                // UI Label = Funded?; Only used for Motor Product - CGBIMO && 
                // UI Label = Deducts Adjustable?; This is on a ClaimHeader object
                if (field.PropertyName != ClaimHeader.CustomNumeric10FieldName   
                && field.PropertyName != ClaimHeader.CustomBoolean15FieldName   
                && !((ClaimHeader)component).CustomBoolean12.GetValueOrDefault(false))   
                {
                    field.Readonly = true;
                }
            }
            else
            {
                field.Visible = false;
            }

            return null;
        }

        /// <summary>
        /// Try resolve policy excess and dedutible  data
        /// If one of the five Policy deductible or funded values has changed, sets the Deductibles Overridden setting (CustomBoolean15) to true 
        /// and creates a corresponding Event.
        /// </summary>
        /// <param name="component">Claim Header</param>
        /// <param name="data">AXA Policy ED Data</param>
        /// <returns>return true if policy deductible id NULL in AXAPolicyEDData</returns>
        private bool TryResolvePolicyEDData(IBusinessComponent component, out AXAPolicyEDData data)
        {
             var policyDeductibles = component.Context.GetAttachedData<AXAPolicyEDData>(false);
             if (policyDeductibles == null)
             {
                 ClaimHeader claimHeader = (ClaimHeader)component;
                 if (claimHeader.PolicyHeaderID.GetValueOrDefault() != 0)
                 {
                     data = this.LoadPolicyDeductibles(claimHeader);
                     claimHeader.Context.AddAttachedData<AXAPolicyEDData>(data);
                     return true;
                 }
                 else
                 {
                     data = null;
                     return false;
                 }
             }

             data = policyDeductibles.Single();
             return true;
        }

        /// <summary>
        /// Load policy deductibles
        /// </summary>
        /// <param name="claimHeader">Claim Header</param>
        /// <returns>AXA policy excess and deductible data</returns>
        private AXAPolicyEDData LoadPolicyDeductibles(ClaimHeader claimHeader)
        {
           // Initialise to false
           AXAPolicyEDData policyEDData = new AXAPolicyEDData(false, false);
           // If policy is attached or changed, set context level flag to identify whether policy deductibles exist
           ProductClaimDefinition productClaimDef = claimHeader.GetProduct().ProductClaimDefinition;
           if (productClaimDef.ClaimHeaderAutomaticDeductibleProcessingMethod == (short)StaticValues.ClaimHeaderAutomaticDeductibleProcessingMethod.StandardClaimHeaderDeductible)
           {
               IAXAClaimsQuery query = new AXAClaimsQueries();
               bool deductibleExist = false;
               bool EDExcessExist = false;
               query.PolicyDeductiblesExist(claimHeader.PolicyHeaderID.Value, "AND2", "AND3", out deductibleExist, out EDExcessExist);
               policyEDData.DeductiblesExist = deductibleExist;
               policyEDData.EDExcessExist = EDExcessExist;
           }

           return policyEDData;
       }

        /// <summary>
        /// Set Createdby field by UserIdenity 
        /// </summary>
        /// <param name="claimHeader">Claim Header</param>
        private void SetCreatedBy(ClaimHeader claimHeader)
        {
            // CustomReference05=CreatedBy
            claimHeader.CustomReference05 = claimHeader.Context.User.UserIdentity;   // UI Label = Created By
        }

        /// <summary>
        /// Add DedutibleExcessEvent on ClaimHeader
        /// </summary>
        /// <param name="claimHeader"> Claim Header</param>
        private void AddDeductibleExcessEvent(ClaimHeader claimHeader)
        {
            ////This needs to be run only if policy is attached.
            if (claimHeader.PolicyHeaderID == null || claimHeader.PolicyHeaderID.GetValueOrDefault(0) <= 0)
            {
                return;
            }
            
            ////Do not add event the first time covergae verification is run
            if (claimHeader.DirtyPropertyList.Any(x => x.Key == ClaimConstants.POLICY_HEADER_ID) == true)
            {
                // UI Label = Excess; Only used for Motor Product - CGBIMO
                if (claimHeader.PropertiesChanged.Any(x => x.Key == ClaimConstants.CustomNumeric10) == true)   
                {
                    // CustomBoolean14 =Excess Overridden?
                    // UI Label = Excess Overridden?; Only used for Motor Product - CGBIMO
                    // else if UI Label = Excess Overridden?; Only used for Motor Product - CGBIMO
                    if (claimHeader.CustomBoolean14 == null)   
                    {
                        claimHeader.CustomBoolean14 = false;   // UI Label = Excess Overridden?; Only used for Motor Product - CGBIMO
                    }
                    else if (claimHeader.CustomBoolean14 == false)   
                    {
                        claimHeader.CustomBoolean14 = true;   // UI Label = Excess Overridden?; Only used for Motor Product - CGBIMO
                        this.AddEvent(claimHeader, ClaimConstants.EVENT_TYPECODE_EXCESS);
                    }
                }

                if (claimHeader.PropertiesChanged.Any(x => x.Key == ClaimConstants.PolicyDeductible01 ||
                    x.Key == ClaimConstants.PolicyDeductible02 ||
                    x.Key == ClaimConstants.PolicyDeductible03 ||
                    x.Key == ClaimConstants.PolicyDeductible04 ||
                    x.Key == ClaimConstants.PolicyDeductible05 ||
                    x.Key == ClaimConstants.IsDeductible01PaidByInsurer ||
                    x.Key == ClaimConstants.IsDeductible02PaidByInsurer ||
                    x.Key == ClaimConstants.IsDeductible03PaidByInsurer ||
                    x.Key == ClaimConstants.IsDeductible04PaidByInsurer ||
                    x.Key == ClaimConstants.IsDeductible05PaidByInsurer) == true)
                {
                    // CustomBoolean13 = Deducts Overridden?
                    // UI Label = Deducts Overridden?
                    // else if UI Label = Deducts Overridden?
                    if (claimHeader.CustomBoolean13 == null)   
                    {
                        claimHeader.CustomBoolean13 = false;   // UI Label = Deducts Overridden?
                    }
                    else if (claimHeader.CustomBoolean13 == false)   
                    {
                        claimHeader.CustomBoolean13 = true;   // UI Label = Deducts Overridden?
                        this.AddEvent(claimHeader, ClaimConstants.EVENT_TYPECODE_DEDUCT);
                    }
                }

                return;
            }

            ////If the user has modified CustomNumeric10, add the event.
            // UI Label = Excess; Only used for Motor Product - CGBIMO
            if (claimHeader.DirtyPropertyList.Any(x => x.Key == ClaimConstants.CustomNumeric10) == true)   
            {
                // UI Label = Excess Overridden?; Only used for Motor Product - CGBIMO
                if (claimHeader.CustomBoolean14.GetValueOrDefault(false) == false)   
                {
                    // CustomBoolean14 =Excess Overridden?
                    claimHeader.CustomBoolean14 = true;   // UI Label = Excess Overridden?; Only used for Motor Product - CGBIMO
                }

                // UI Label = Excess; Only used for Motor Product - CGBIMO
                if (claimHeader.PropertiesChanged.Any(x => x.Key == ClaimConstants.CustomNumeric10) == true)   
                {
                    this.AddEvent(claimHeader, ClaimConstants.EVENT_TYPECODE_EXCESS);
                }
            }

            ////If the user has modified CustomNumeric10, add the event.
            if (claimHeader.DirtyPropertyList.Any(x => x.Key == ClaimConstants.PolicyDeductible01 || 
                x.Key == ClaimConstants.PolicyDeductible02 || 
                x.Key == ClaimConstants.PolicyDeductible03 || 
                x.Key == ClaimConstants.PolicyDeductible04 || 
                x.Key == ClaimConstants.PolicyDeductible05 ||
                x.Key == ClaimConstants.IsDeductible01PaidByInsurer ||
                x.Key == ClaimConstants.IsDeductible02PaidByInsurer ||
                x.Key == ClaimConstants.IsDeductible03PaidByInsurer ||
                x.Key == ClaimConstants.IsDeductible04PaidByInsurer ||
                x.Key == ClaimConstants.IsDeductible05PaidByInsurer) == true)
            {
                if (claimHeader.CustomBoolean13.GetValueOrDefault(false) == false)   
                {
                    // UI Label = Deducts Overridden?
                    // CustomBoolean13 = Deducts Overridden?
                    claimHeader.CustomBoolean13 = true;   // UI Label = Deducts Overridden?
                }

                if (claimHeader.PropertiesChanged.Any(x => x.Key == ClaimConstants.PolicyDeductible01 ||
                    x.Key == ClaimConstants.PolicyDeductible02 ||
                    x.Key == ClaimConstants.PolicyDeductible03 ||
                    x.Key == ClaimConstants.PolicyDeductible04 ||
                    x.Key == ClaimConstants.PolicyDeductible05 ||
                    x.Key == ClaimConstants.IsDeductible01PaidByInsurer ||
                    x.Key == ClaimConstants.IsDeductible02PaidByInsurer ||
                    x.Key == ClaimConstants.IsDeductible03PaidByInsurer ||
                    x.Key == ClaimConstants.IsDeductible04PaidByInsurer ||
                    x.Key == ClaimConstants.IsDeductible05PaidByInsurer) == true)
                {
                    this.AddEvent(claimHeader, ClaimConstants.EVENT_TYPECODE_DEDUCT);
                }
            }
        }

        /// <summary>
        /// Add Event if it has been added within a transaction once.
        /// </summary>
        /// <param name="claimHeader">Claim Header</param>
        /// <param name="eventTypeCode">event TypeCode</param>
        private void AddEvent(ClaimHeader claimHeader, string eventTypeCode)
        {
            //// The event should be added if it has been added within a transaction once.
            if (!claimHeader.ClaimEvents.Any(x => x.IsNew == true && x.EventTypeCode == eventTypeCode))
            {
                var productEvent = ProductService.GetProductEventQuery()
                            .GetProductEvents(claimHeader.ProductVersionID.GetValueOrDefault())
                            .Where(x => x.EventTypeCode == eventTypeCode).FirstOrDefault();

                if (productEvent != null)
                {
                    claimHeader.AddNewClaimEvent(productEvent.ProductEventID, true);
                }
            }
        }
    }
}
