using System;
using System.Collections.Generic;
using System.Xml;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework.Common;
using Xiap.Framework.Validation;
using Xiap.Metadata.Data.Enums;
using Xiap.Framework;
using Xiap.Framework.Security;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    public class RecoveryAuthorisationUserResolution : IDataCollection
    {
        #region IDataCollection Members

        /// <summary>
        /// Resolves a user who can authorise the Recovery. If none found return business support role
        /// </summary>
        /// <param name="parameters">claimTransactionHeaderId and creatorUserIdentity</param>
        /// <returns>Xml destination type and name</returns>
        public XmlElement GetData(IDictionary<string, object> parameters)
        {
            ArgumentCheck.ArgumentNullCheck(parameters, "parameters");

            long claimTransactionHeaderId = Convert.ToInt64(parameters["claimTransactionHeaderId"]);
            string calimHandler = this.ResolveClaimHandler(claimTransactionHeaderId);

            var document = new XmlDocument();
            var element = document.CreateElement("Destination");
            element.SetAttribute("DestinationType", StaticValues.DestinationType.User.ToString());
            element.SetAttribute("DestinationName", calimHandler);

            return element;    
        }

        #endregion
        /// <summary>
        /// Resolve claim handler 
        /// </summary>
        /// <param name="claimTransactionHeaderID">Claim Transaction Header ID</param>
        /// <returns>User Identity</returns>
        private string ResolveClaimHandler(long claimTransactionHeaderID)
        {
            // Get the claim transaction header and then retrieve the main claim handler name involvement from it.
            var claimTransactionHeader = ObjectFactory.Resolve<IClaimsQuery>().GetClaimTransactionHeader(claimTransactionHeaderID);
            var nameInvolvement = ClaimsBusinessLogicHelper.GetMainClaimHandlerFromHeader(claimTransactionHeader.ClaimHeader);
            // Throw exception if no claim handler
            if (nameInvolvement == null)
            {
                throw new InvalidOperationException(string.Format("Claim Handler Not found for claim reference: {0}", claimTransactionHeader.ClaimHeader.ClaimReference));
            }

            // Throw exception if no User is associated with this claim handler
            if (!nameInvolvement.NameID.HasValue)
            {
                throw new InvalidOperationException(string.Format("Claim Handler has no associated name. claim reference: {0}", claimTransactionHeader.ClaimHeader.ClaimReference));
            }

            // Throw exception if the associated user on the claim handler can't be resolved to a full GeniusX user
            User user;
            if (!ClaimsBusinessLogicHelper.TryGetUserByNameID(nameInvolvement.NameID.Value, out user))
            {
                throw new InvalidOperationException(string.Format("No name attachd to name id {0}", nameInvolvement.NameID.Value));
            }

            // Return the user Identity
            return user.UserIdentity;
        }
    }
}
