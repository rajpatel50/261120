using System;
using System.Collections.Generic;
using System.Configuration;
using System.Xml;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.Common;
using Xiap.Framework.Validation;
using Xiap.Metadata.BusinessComponent;
using Xiap.Metadata.Data.Enums;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    public class ReserveAuthorisationUserResolution : IDataCollection
    {
        #region IDataCollection Members

        /// <summary>
        /// Resolves a user who can authorise the payment. If none found return business support role
        /// </summary>
        /// <param name="parameters">claimTransactionHeaderId and creatorUserIdentity</param>
        /// <returns>Xml destination type and name</returns>
        public XmlElement GetData(IDictionary<string, object> parameters)
        {
            ArgumentCheck.ArgumentNullCheck(parameters, "parameters");

            long claimTransactionHeaderId = Convert.ToInt64(parameters["claimTransactionHeaderId"]);
            string creatorUserIdentity = Convert.ToString(parameters["creatorUserIdentity"]);
            string destinationType = null;
            string destinationUser = ResolveDestinationUser(claimTransactionHeaderId, creatorUserIdentity, out destinationType);
            var document = new XmlDocument();
            var element = document.CreateElement("Destination");
            element.SetAttribute("DestinationType", destinationType);
            element.SetAttribute("DestinationName", destinationUser);

            return element;
        }

        #endregion

        /// <summary>
        /// Resolves a user who can authorise the payment. If none found return business support role
        /// </summary>
        /// <param name="claimTransactionHeaderID">Transaction Header ID</param>
        /// <param name="creatorUserIdentity">Task Creator User ID </param>
        /// <param name="destinationType"> Destination type User/Role</param>
        /// <returns>User Identity</returns>
        private static string ResolveDestinationUser(long claimTransactionHeaderID, string creatorUserIdentity, out string destinationType)
        {
            ArgumentCheck.ArgumentZeroCheck(claimTransactionHeaderID, "claimTransactionHeaderID");
            ArgumentCheck.ArgumentNullOrEmptyCheck(creatorUserIdentity, "creatorUserIdentity");
            destinationType = StaticValues.DestinationType.User.ToString();

            var claimTransactionHeader = ObjectFactory.Resolve<IClaimsQuery>().GetClaimTransactionHeader(claimTransactionHeaderID);
            // try to get referee from claim transaction
            string userIdentity = claimTransactionHeader.CustomReference01;   // UI Label = Referee

            if (userIdentity == null)
            {
                // No referee supplied so attept to get Creator User from passed in Identity
                var metadataEntities = ObjectFactory.Resolve<IMetadataQuery>();
                User creatorUser = metadataEntities.GetUserByUserIdentity(creatorUserIdentity);
                if (creatorUser.ManagerID.HasValue)
                {
                    User manager = metadataEntities.GetUserByUserId(creatorUser.ManagerID.Value);
                    // Throw an exception of the user has no valid user as a Manager
                    if (manager == null)
                    {
                        throw new InvalidOperationException(string.Format("No user for id {0}", creatorUser.ManagerID.Value));
                    }

                    // Otherwise, we use the user's manager as the referee
                    userIdentity = manager.UserIdentity;
                }
                else
                {
                    // We can't resolve the creator user so instead we default to the Business Support role, taken from the application configuration
                    var value = ConfigurationManager.AppSettings[ClaimConstants.BUSINESS_SUPPORT_ROLE_SETTING];
                    if (value != null)
                    {
                        userIdentity = (string)value;
                        destinationType = StaticValues.DestinationType.Role.ToString();
                    }
                    else
                    {
                        // If we haven't got the business support role defined in the application config, throw an exception.
                        throw new InvalidOperationException(string.Format("{0} not defined in config", ClaimConstants.BUSINESS_SUPPORT_ROLE_SETTING));
                    }
                }
            }

            return userIdentity;
        }
    }
}
