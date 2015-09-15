using System;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.Data.InsuranceDirectory;
using Xiap.Framework.ProcessHandling;
using Xiap.Metadata.Data.Enums;
using Xiap.Framework.Common;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    /// <summary>
    /// If the Major Assured Name changes—including when first created—it removes the client specific codes—which are implemented using Generic Data Sets 
    /// and adds new codes for the new Name.
    /// Additionally, for PropertyChange, it resets the Contact, Contact Telephone No. and Contact Email details 
    /// to null (CustomReference02, CustomReference03 and CustomReference04 respectively) , when the Client is changed.
    /// </summary>
    public class ClientSpecificDataPlugin : AbstractComponentPlugin
    {
        private static void AddandRemoveClientSpecficCodes(INameUsage assdNameUsage, ClaimHeader claimHeader)
        {
            string clientCode = assdNameUsage.CustomCode01;   // UI Label = Client-Specific Claim Data Set; For Insured

            // Delete existing client specific generic data item on header
            ClaimComponentsHelper.DeleteExistingClientSpecificCode(claimHeader);

            // Add client specific generic data item on header
            ClaimComponentsHelper.CreateClientSpecificGenericDataSet(claimHeader, clientCode);
        }

        /// <summary>
        /// Field Retrival for diffrent type of Name type set field visibilty
        /// </summary>
        /// <param name="component">Claim Name Involvement</param>
        /// <param name="point">Field Retrieval</param>
        /// <param name="field">CustomReference02, CustomReference03,CustomReference04-Field Description  Change according to Name Type </param>
        /// <param name="pluginId">Plugin Id </param>
        /// <returns>Process Results Collection</returns>
        public override ProcessResultsCollection FieldRetrieval(IBusinessComponent component, ProcessInvocationPoint point, ref Xiap.Framework.Metadata.Field field, int pluginId)
        {
            ClaimNameInvolvement claimNameInvolvement = component as ClaimNameInvolvement;

            if (claimNameInvolvement.NameID != null)
            {
                ClaimsTransactionContext transactionContext = component.Context as ClaimsTransactionContext;
                IName name = transactionContext.GetName(claimNameInvolvement.NameID.Value);
                if (name.NameType == (short)StaticValues.NameType.Company)
                {
                    // UI Label = Contact; Not used in all the NI's
                    // else if UI Label = Contact; Not used in all the NI's
                    if (field.PropertyName == ClaimNameInvolvement.CustomReference02FieldName || field.PropertyName == ClaimNameInvolvement.CustomReference03FieldName || field.PropertyName == ClaimNameInvolvement.CustomReference04FieldName)   
                    {
                        field.Visible = true;
                    }
                }
                else if (name.NameType == (short)StaticValues.NameType.Person)
                {
                    if (field.PropertyName == ClaimNameInvolvement.CustomReference02FieldName || field.PropertyName == ClaimNameInvolvement.CustomReference03FieldName || field.PropertyName == ClaimNameInvolvement.CustomReference04FieldName)   
                    {
                        field.Visible = false;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Property change plugin if  Propert get change 
        /// </summary>
        /// <param name="component">Claim Name Involvement</param>
        /// <param name="point">Property Change</param>
        /// <param name="propertyName"> Name ID </param>
        /// <param name="oldValue">OLd Value </param>
        /// <param name="newValue">New Value </param>
        /// <param name="pluginId">Plugin ID </param>
        /// <returns>Process Results Collection</returns>
        public override ProcessResultsCollection PropertyChange(IBusinessComponent component, ProcessInvocationPoint point, string propertyName, object oldValue, object newValue, int pluginId)
        {
            PluginHelper<ClaimNameInvolvement> pluginHelper = new PluginHelper<ClaimNameInvolvement>(point, (ClaimNameInvolvement)component);
            ClaimsTransactionContext transactionContext = component.Context as ClaimsTransactionContext;
            ClaimNameInvolvement claimNameInvolvement = component as ClaimNameInvolvement;

            if (propertyName == ClaimNameInvolvement.NameIDFieldName)
            {
                if (newValue != null && (long)newValue != 0 && claimNameInvolvement != null && claimNameInvolvement.NameInvolvementType == (short)StaticValues.NameInvolvementType.MajorInsured && claimNameInvolvement.NameInvolvementMaintenanceStatus == (short)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest)
                {
                    INameUsage assdNameUsage = null;
                    ClaimHeader claimHeader = claimNameInvolvement.ClaimInvolvement.ClaimHeader;
                    DateTime date = DateTime.Today;
                    if (claimHeader.DateOfLossFrom != null)
                    {
                        date = claimHeader.DateOfLossFrom.Value;
                    }

                    assdNameUsage = transactionContext.GetNameUsage((long)newValue, claimNameInvolvement.NameUsageTypeCode, date);
                    
                    // UI Label = Client-Specific Claim Data Set; For Insured
                    if (assdNameUsage == null || string.IsNullOrEmpty(assdNameUsage.CustomCode01))   
                    {
                        return null;
                    }

                    AddandRemoveClientSpecficCodes(assdNameUsage, claimHeader);
                }
            }

            this.UpdateNameInvolvementField(pluginHelper, propertyName, oldValue, newValue, transactionContext);
            return pluginHelper.ProcessResults;
        }

        /// <summary>
        /// Set Propert to null If NameType is  Person
        /// </summary>
        /// <param name="pluginHelper">Plugin Helper</param>
        /// <param name="propertyName">Name ID</param>
        /// <param name="oldValue">Old value </param>
        /// <param name="newValue">New Value </param>
        /// <param name="transactionContext">Claims Transaction Context</param>
        private void UpdateNameInvolvementField(PluginHelper<ClaimNameInvolvement> pluginHelper, string propertyName, object oldValue, object newValue, ClaimsTransactionContext transactionContext)
        {
            ClaimNameInvolvement claimNameInvolvement = pluginHelper.Component;

            if (propertyName == ClaimNameInvolvement.NameIDFieldName && newValue != null && (long)newValue != 0 && oldValue != newValue)
            {
                IName name = transactionContext.GetName((long)newValue);
                if (name.NameType == (short)StaticValues.NameType.Person)
                {
                    claimNameInvolvement.CustomReference02 = null;   // UI Label = Contact
                    claimNameInvolvement.CustomReference03 = null;   // UI Label = Contact Telephone Number
                    claimNameInvolvement.CustomReference04 = null;   // UI Label = Contact Email
                }
            }
        }

        /// <summary>
        /// Starting point of Plugin invocation 
        /// </summary>
        /// <param name="component">IBusiness Component</param>
        /// <param name="point">Process Invocation Point</param>
        /// <param name="pluginId">Plugin ID</param>
        /// <returns>Process Results Collection </returns>
        public override ProcessResultsCollection ProcessComponent(IBusinessComponent component, ProcessInvocationPoint point, int pluginId)
        {
            PluginHelper<ClaimNameInvolvement> pluginHelper = new PluginHelper<ClaimNameInvolvement>(point, (ClaimNameInvolvement)component);
            ClaimsTransactionContext transactionContext = component.Context as ClaimsTransactionContext;
            ClaimNameInvolvement claimNameInvolvement = component as ClaimNameInvolvement;

            if (claimNameInvolvement != null && claimNameInvolvement.NameID != null && claimNameInvolvement.NameInvolvementType == (short)StaticValues.NameInvolvementType.MajorInsured && claimNameInvolvement.NameInvolvementMaintenanceStatus == (short)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest)
            {
                INameUsage assdNameUsage = null;
                ClaimHeader claimHeader = claimNameInvolvement.ClaimInvolvement.ClaimHeader;
                DateTime date = DateTime.Today;
                if (claimHeader.DateOfLossFrom != null)
                {
                    date = claimHeader.DateOfLossFrom.Value;
                }

                assdNameUsage = transactionContext.GetNameUsage(claimNameInvolvement.NameID.Value, claimNameInvolvement.NameUsageTypeCode, date);

                // UI Label = Client-Specific Claim Data Set; For Insured
                if (assdNameUsage == null || string.IsNullOrEmpty(assdNameUsage.CustomCode01))   
                {
                    return null;
                }

                AddandRemoveClientSpecficCodes(assdNameUsage, claimHeader);
            }

            return pluginHelper.ProcessResults;
        }
    }
}
