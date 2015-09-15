using System.Linq;
using Xiap.Framework;
using Xiap.Framework.ProcessHandling;
using Xiap.Framework.Security;
using Xiap.UW.BusinessComponent;

namespace GeniusX.AXA.Underwriting.BusinessLogic
{
    /// <summary>
    /// When the Header status of the Policy is changed, this verifies that the User setting the status of the Policy
    /// to Verified is not the user who submitted the Policy for verification.
    /// </summary>
    public class HeaderStatusChangedPlugin : AbstractComponentPlugin
    {
        public override ProcessResultsCollection ProcessComponent(IBusinessComponent component, ProcessInvocationPoint point, int pluginId, ProcessParameters processParameters)
        {
            ProcessResultsCollection processResults = new ProcessResultsCollection();
            Header header = (Header)component;
            // Get the header status code that matches Policy Verified from the Application Configuration
            string verifiedHeaderStatus = UWBusinessLogicHelper.ResolveMandatoryConfig<string>("PolicyVerifiedHeaderStatus");
            // If the header status code has been changed and now matches the verified header status code retried, process
            if (header.HeaderStatusCode == verifiedHeaderStatus && header.HeaderStatusCode != header.OriginalTransitionStatus)
            {
                // Retrieve the Policy Verify Permission Token and the Submitted for verification Event codes from the Application Configuration
                string policyVerifyPermissionToken = UWBusinessLogicHelper.ResolveMandatoryConfig<string>("PolicyVerificationPermissionToken");
                string submittedForVerificationEventTypeCode = UWBusinessLogicHelper.ResolveMandatoryConfig<string>("SubmittedForVerificationEventTypeCode");
                
                // Check if this user has Policy Verify in their permission tokens. This method throws a SecurityException if this fails
                XiapSecurity.Assert(policyVerifyPermissionToken);
                // Check in tasks for a 'submitted for verification' task and make sure it wasn't entered by the user who is now trying to
                // verify the policy. Throw an error if so.
                var eventType = header.UwEvents.Where(e => e.EventTypeCode == submittedForVerificationEventTypeCode).OrderByDescending(e => e.EventDate).FirstOrDefault();
                if (eventType != null && eventType.CreatedByUserID == XiapSecurity.GetUser().UserID)
                {
                    UWBusinessLogicHelper.AddError(processResults, UwMessageConstants.POLICY_CANNOT_BE_VERIFIED_BY_PERSON_SUBMITTED, ProcessInvocationPoint.Virtual, component);
                }
            }

            return processResults;
        }
    }
}
