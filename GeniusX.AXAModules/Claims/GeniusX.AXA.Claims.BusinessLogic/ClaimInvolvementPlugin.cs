using System.Linq;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.Extensions;
using Xiap.Framework.ProcessHandling;
using Xiap.Metadata.Data.Enums;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    /// <summary>
    /// Sets the Name on the Claim Detail Title if the Name for the associated Claim Name Involvement (Additional Claimant) is changed.
    /// </summary>
   public class ClaimInvolvementPlugin : AbstractComponentPlugin
    {
       /// <summary>
        /// Call on  ClaimInvolvement
       /// </summary>
        /// <param name="component">Claim Involvement</param>
        /// <param name="point">Component Change,Delete</param>
       /// <param name="pluginId">PlugIn  ID</param>
        /// <returns>return ProcessResultCollection which have Error msg If any validation failed.</returns>
        public override ProcessResultsCollection ProcessComponent(Xiap.Framework.IBusinessComponent component, Xiap.Framework.ProcessInvocationPoint point, int pluginId)
        {
            PluginHelper<ClaimInvolvement> pluginHelper = new PluginHelper<ClaimInvolvement>(point, (ClaimInvolvement)component, new ProcessResultsCollection());
            
            switch (point)
            {
                case Xiap.Framework.ProcessInvocationPoint.ComponentChange:
                    this.ChangeCDTitle(pluginHelper, point);
                    break; 
                case Xiap.Framework.ProcessInvocationPoint.Delete:
                    this.OnDelete(pluginHelper, point);
                    break;               
            }

            return pluginHelper.ProcessResults;
        }

       /// <summary>
       /// Call on ClaimInvolvement Deletion
       /// Sets the Name on the Claim Detail Title on the deletion of the AdditionalClaimant and Driver type Claim Name Involvements
       /// </summary>
        /// <param name="pluginHelper">ClaimInvolvement type claim helper</param>
        /// <param name="point"> Delete Point</param>
        private void OnDelete(PluginHelper<ClaimInvolvement> pluginHelper, Xiap.Framework.ProcessInvocationPoint point)
        {
            pluginHelper.Component.ClaimDetailToClaimInvolvementLinks
                  .ForEach(c =>
                  {
                      ClaimsBusinessLogicHelper.SetClaimDetailTitle(c, point);
                  });
        }

       /// <summary>
        /// Change claimdetail title
       /// </summary>
        /// <param name="pluginHelper">ClaimInvolvement type claim helper</param>
        /// <param name="point">Component Change</param>
        private void ChangeCDTitle(PluginHelper<ClaimInvolvement> pluginHelper, ProcessInvocationPoint point)
        {
            ClaimInvolvement claimInvolvement = pluginHelper.Component as ClaimInvolvement;

            claimInvolvement.ClaimNameInvolvements.ForEach(x =>
            {
                if (x.DirtyPropertyList.Any(y => y.Key == ClaimConstants.Name_ID))
                {
                    UpdateClaimDetailTitle(point, claimInvolvement);
                    return;
                }
            });
        }

       /// <summary>
        /// Update claim detail title
       /// </summary>
        /// <param name="point">Component Change</param>
        /// <param name="claimInvolvement">Claim Involvement</param>
        private void UpdateClaimDetailTitle(ProcessInvocationPoint point, ClaimInvolvement claimInvolvement)
        {
            claimInvolvement.ClaimHeader.ClaimDetails.ForEach(claimDetail =>
            {
                if (claimDetail.ClaimDetailToClaimInvolvementLinks.Any(y => y.ClaimInvolvement.ClaimInvolvementType == (short)StaticValues.LinkableComponentType.NameInvolvement && y.ClaimNameInvolvement != null && y.ClaimNameInvolvement.NameInvolvementType == (short)StaticValues.NameInvolvementType_ClaimNameInvolvement.AdditionalClaimant))
                {
                    ClaimDetailToClaimInvolvementLink link = claimDetail.ClaimDetailToClaimInvolvementLinks.Where(y => y.ClaimInvolvement.ClaimInvolvementType == (short)StaticValues.LinkableComponentType.NameInvolvement
                    && y.ClaimNameInvolvement != null && y.ClaimNameInvolvement.NameInvolvementType == (short)StaticValues.NameInvolvementType_ClaimNameInvolvement.AdditionalClaimant).First();

                    if (link != null)
                    {
                        ClaimsBusinessLogicHelper.SetClaimDetailTitle(link, point);
                    }
                }
            });
        }       
    }
}
    
