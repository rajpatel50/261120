using System;
using System.Linq;
using Xiap.Framework;
using Xiap.Framework.Data.InsuranceDirectory;
using Xiap.Framework.ProcessHandling;
using Xiap.Metadata.Data.Enums;
using Xiap.UW.BusinessComponent;

namespace GeniusX.AXA.Underwriting.BusinessLogic
{
    /// <summary>
    /// If a Generic Data Item is used for aggregate, deductible or AD excess data—defined as GDTs AND1, AND2 and AND3 
    /// respectively - then this sets the lookup parameters on the Division (CustomCode01) dropdown of the GDT based on
    /// a value stored in CustomReference01 of the associated Major Insured’s Name Usage
    /// </summary> 
    public class ExcessAndDeductibleDivisionPlugin : AbstractComponentPlugin
    {
        private const string EmptyGroup = "XEMPTY";

        public override ProcessResultsCollection FieldRetrieval(IBusinessComponent component, ProcessInvocationPoint point, ref Xiap.Framework.Metadata.Field field, int pluginId)
        {
            UwGenericDataItem item = (UwGenericDataItem)component;
            // Check if this is an aggregate, deductible or AD excess Generic Data Type
            if (item.GenericDataTypeCode == "AND1" || item.GenericDataTypeCode == "AND2" || item.GenericDataTypeCode == "AND3")
            {
                // If we're retrieving is 'Division', because this is the name on the GenericDataItem's CustomCode01 field.
                if (field.PropertyName.Equals(UwGenericDataItem.CustomCode01FieldName))
                {
                    field.LookupParameters.GroupCode = EmptyGroup;
                    // Get the header from the Generic Data Item
                    Header header = item.GenericDataSet.GetUwHeader();
                    if (header != null)
                    {
                        // Get the inception date from the latest Header Version (or today's date if it's not set), and the Major Insured from the Header.
                        DateTime effectiveDate = ((HeaderVersion)header.GetLatestVersion()).InceptionDate.GetValueOrDefault(DateTime.Today);
                        UwNameInvolvement insured = header.NameInvolvements.Where(a => a.NameID != null && a.NameInvolvementType == (short)StaticValues.NameInvolvementType.MajorInsured).FirstOrDefault() as UwNameInvolvement;
                        if (insured != null)
                        {
                            // Assuming we have the insured name involvement, find the name usage for the inception date from the header.
                            IInsuranceDirectoryService ids = ObjectFactory.Resolve<IInsuranceDirectoryService>();
                            INameUsage assdNameUsage = ids.GetNameUsage(insured.NameID.Value, insured.NameUsageTypeCode, effectiveDate);
                            if (assdNameUsage != null && assdNameUsage.CustomReference01 != null)
                            {
                                // Use the CustomReference01 value from the Insured Name Usage as the Group Code for the drop down in our Division lookup.
                                field.LookupParameters.GroupCode = assdNameUsage.CustomReference01;
                            }
                        }
                    }
                }
            }

            return null;
        }
    }
}
