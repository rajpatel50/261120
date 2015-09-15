using System;
using System.Linq;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.Data.InsuranceDirectory;
using Xiap.Framework.Extensions;
using Xiap.Framework.Metadata;
using Xiap.Framework.ProcessHandling;
using Xiap.Metadata.Data;
using Xiap.Metadata.Data.Enums;
using Xiap.Framework.Common;
/// Sets up the Division, Sub-division and Cost Centre code values to be based on the client—Major Assured.
/// 
/// Also sets up the Circumstances1, Circumstances2 and Circumstances3 codes based on the Type of Loss code.
/// 
namespace GeniusX.AXA.Claims.BusinessLogic
{
    public class ClaimHeaderDefaultingDivisionPlugin : AbstractComponentPlugin
    {
        private readonly ILookupDefinitionCache lookupDefinitionCache;

        /// <summary>
        /// Resolve LookupDefinitionCache on object Creation of ClaimHeaderDefaultingDivisionPlugin.
        /// </summary>
        public ClaimHeaderDefaultingDivisionPlugin()
        {
            this.lookupDefinitionCache = ObjectFactory.Resolve<ILookupDefinitionCache>();
        }

        /// <summary>
        /// Call on specific field of ClaimHeader.
        /// </summary>
        /// <param name="component">Claim Header</param>
        /// <param name="point">Field Retrieval</param>
        /// <param name="field">On which field</param>
        /// <param name="pluginId">PlugIN ID</param>
        /// <returns>return ProcessResultCollection which have Error msg If any validation failed.</returns>
        public override ProcessResultsCollection FieldRetrieval(IBusinessComponent component, ProcessInvocationPoint point, ref Xiap.Framework.Metadata.Field field, int pluginId)
        {
            bool isGroupCodeSet = false;
            ClaimHeader header = (ClaimHeader)component;

            switch (field.PropertyName)
            {
                case ClaimHeader.ClaimHeaderAnalysisCode04FieldName:
                    var insurd = (from ci in header.ClaimInvolvements
                                  where ci.ClaimInvolvementType == (short)StaticValues.LinkableComponentType.NameInvolvement
                                  from cni in ci.ClaimNameInvolvements
                                  where cni.NameID != null &&
                                  cni.NameInvolvementType == (short)StaticValues.NameInvolvementType.MajorInsured &&
                                  cni.NameInvolvementMaintenanceStatus == (short)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest
                                  select cni).FirstOrDefault();
                                      
                    if (insurd != null)
                    {
                        ClaimsTransactionContext transactionContext = (ClaimsTransactionContext)component.Context;
                        INameUsage assdNameUsage = transactionContext.GetNameUsage(insurd.NameID.Value, ((ClaimNameInvolvement)insurd).NameUsageTypeCode, header.DateOfLossFrom.GetValueOrDefault(DateTime.Today));
                        // UI Label = Client ID; For Insured
                        if (assdNameUsage != null && assdNameUsage.CustomReference01 != null)
                        {
                            field.LookupParameters.GroupCode = assdNameUsage.CustomReference01;   // UI Label = Client ID; For Insured
                            isGroupCodeSet = true;
                        }
                    }

                    if (!isGroupCodeSet)
                    {
                        field.LookupParameters.GroupCode = ClaimConstants.EmptyGroup;
                    }

                    this.SetFieldMandatory(field, header.ClaimHeaderAnalysisCode04VS, header.ClaimHeaderAnalysisCode04, header.ValidationDate);
                    break;

                case ClaimHeader.ClaimHeaderAnalysisCode05FieldName:
                    this.SetFieldMandatory(field, header.ClaimHeaderAnalysisCode05VS, header.ClaimHeaderAnalysisCode04, header.ValidationDate);
                    break;

                case ClaimHeader.ClaimHeaderAnalysisCode06FieldName:
                    this.SetFieldMandatory(field, header.ClaimHeaderAnalysisCode06VS, header.ClaimHeaderAnalysisCode05, header.ValidationDate);
                    break;

                case ClaimHeader.CustomCode01FieldName:   // UI Label = Circumstances 1
                    // CustomCode01VS =Circumstances 1
                    this.SetFieldMandatory(field, header.CustomCode01VS, header.ClaimHeaderAnalysisCode02, header.ValidationDate);   // UI Label = Circumstance 1
                    break;

                case ClaimHeader.CustomCode02FieldName:   // UI Label = Circumstances 2
                    // CustomCode02VS =Circumstances 2
                    this.SetFieldMandatory(field, header.CustomCode02VS, header.CustomCode01, header.ValidationDate);   // UI Label = Circumstance 2
                    break;

                case ClaimHeader.CustomCode03FieldName:   // UI Label = Circumstances 3; Only used for Motor Product - CGBIMO
                    // CustomCode03VS =Circumstances 3
                    this.SetFieldMandatory(field, header.CustomCode03VS, header.CustomCode02, header.ValidationDate);   // UI Label = Circumstance 3; Only used for Motor Product - CGBIMO
                    break;
            }

            return null;
        }

        /// <summary>
        /// Set field mandatory
        /// </summary>
        /// <param name="field">specified Field</param>
        /// <param name="valueSet">valueSet code</param>
        /// <param name="groupCode">group Code</param>
        /// <param name="validationDate">valid Date</param>
        private void SetFieldMandatory(Field field, int? valueSet, string groupCode, DateTime? validationDate)
        {
            if (string.IsNullOrWhiteSpace(groupCode) == false && valueSet.HasValue)
            {
                var lookupDefinition = this.lookupDefinitionCache.GetLookupDefinition(field.LookupDefinitionKey.Component, valueSet.Value);
                IValueSetData valueSetData = lookupDefinition as IValueSetData;
                if (valueSetData != null)
                {
                    if (!valueSetData.IsGroupingVersioned.GetValueOrDefault(false))
                    {
                        validationDate = null;
                    }
                }

                LookupParameters lkp = new LookupParameters();
                lkp.GroupCode = groupCode;
                lkp.EffectiveDate = validationDate;
                field.Mandatory = !lookupDefinition.RetrieveValues(lkp).IsNullOrEmpty();
            }
            else
            {
                field.Mandatory = false;
            }
        }
    }
}
