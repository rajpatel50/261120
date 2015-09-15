using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.ProcessHandling;
using Xiap.Metadata.Data.Enums;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    public class ClaimTransactionDescriptionPlugin : AbstractComponentPlugin
    {
        /// <summary>
        /// Sets the Claim Transaction Description field on the Claim Transaction Header
        /// based on whether a payment or reserve is being created.
        /// </summary>
        /// <param name="component">Component of Business Type</param>
        /// <param name="point">Process Invocation Point</param>
        /// <param name="pluginId">unique plugin id</param>
        /// <returns>collection of process results</returns>
        public override ProcessResultsCollection ProcessComponent(IBusinessComponent component, ProcessInvocationPoint point, int pluginId)
        {
            PluginHelper<ClaimTransactionHeader> pluginHelper = new PluginHelper<ClaimTransactionHeader>(point, (ClaimTransactionHeader)component, new ProcessResultsCollection());

            switch (pluginHelper.InvocationPoint)
            {
                case ProcessInvocationPoint.Created:
                    {
                        this.OnCreate(pluginHelper);
                        break;
                    }
            }

            return pluginHelper.ProcessResults;
        }

        /// <summary>
        /// Added a transaction description when component created.
        /// </summary>
        /// <param name="pluginHelper">plugin helper of Claim Transaction Header Type</param>
        private void OnCreate(PluginHelper<ClaimTransactionHeader> pluginHelper)
        {
            ClaimTransactionHeader claimTransHeader = pluginHelper.Component;
            ClaimHeader claimHeader = claimTransHeader.ClaimHeader;

            // If we have a Claim Header and a Claim Transaction Header then set the description to 
            // Payments or Receipt as appliccable, based on the CTH source.
            if (claimHeader != null && claimTransHeader != null)
            {
                if (claimTransHeader.ClaimTransactionSource == (short)StaticValues.ClaimTransactionSource.Payment)
                {
                    claimTransHeader.ClaimTransactionDescription = ClaimConstants.PAYMENTS;
                }
                else if (claimTransHeader.ClaimTransactionSource == (short)StaticValues.ClaimTransactionSource.RecoveryReceipt)
                {
                    claimTransHeader.ClaimTransactionDescription = ClaimConstants.RECEIPT;
                }
            }
        }
    }
}
