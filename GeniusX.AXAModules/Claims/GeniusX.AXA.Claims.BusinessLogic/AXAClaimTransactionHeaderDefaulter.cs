using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.Locking;
using Xiap.Framework.ProcessHandling;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    public class AXAClaimTransactionHeaderDefaulter : AbstractComponentPlugin
    {
        /// <summary>
        /// This method assigns next transaction reference to Claim Transaction Reference (ClaimTransactionHeaderReference of ClaimTransactionHeader) 
        /// </summary>
        /// <param name="component">Claim Transaction Header</param>
        /// <param name="point">Process Invocation Point</param>
        /// <param name="pluginId">plugin Id</param>
        /// <returns>Process Results Collection</returns>
        public override ProcessResultsCollection ProcessComponent(IBusinessComponent component, ProcessInvocationPoint point, int pluginId)
        {
            PluginHelper<ClaimTransactionHeader> pluginHelper = new PluginHelper<ClaimTransactionHeader>(point, (ClaimTransactionHeader)component, new ProcessResultsCollection());
            ProcessResultsCollection processResults = new ProcessResultsCollection();

            if (point == ProcessInvocationPoint.Virtual)
            {
                ClaimTransactionHeaderReferenceDefaulting(pluginHelper);
            }

            return processResults;
        }

        /// <summary>
        /// Defaults the Transaction Header Reference for the claim.
        /// </summary>
        /// <param name="pluginHelper">The plugin helper, which contains the ClaimTransactionHeader Component.</param>
        private static void ClaimTransactionHeaderReferenceDefaulting(PluginHelper<ClaimTransactionHeader> pluginHelper)
        {
            ClaimTransactionHeader claimTransHeader = pluginHelper.Component;
            ClaimHeader claimHeader = claimTransHeader.ClaimHeader;

            // Use the XIAP Core Reference Allocation process to allocate a 10-char reference from "0000000001" to "9999999999"
            claimTransHeader.ClaimTransactionHeaderReference = LockManager.AllocateReference(string.Empty, ReferenceType.ClaimTransactionHeaderReference, string.Empty, "0000000001", 10, "9999999999", false);
        }
    }
}
