using System;
using System.Collections.Generic;
using System.Linq;
using Xiap.Claims.BusinessComponent;
using Xiap.Claims.BusinessLogic;
using Xiap.Framework;
using Xiap.Framework.Common;
using Xiap.Framework.Configuration;
using Xiap.Framework.Logging;
using Xiap.Framework.Messages;
using Xiap.Framework.Metadata;
using Xiap.Framework.ProcessHandling;
using Xiap.Metadata.BusinessComponent;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    /// <summary>
    /// Validates the Claim Transfer 
    /// Determines if a Claim is valid for transfer to Genius based on specified Header statuses.
    /// Invoked when a Claim is ready for transfer from Genius.X to Genius.
    /// </summary>
    public class AXAClaimTransferValidation : IClaimTransferValidation
    {
        private static readonly ILogger logger = LoggerFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public void ValidateClaimForTransfer(string claimReference, string claimTransactionHeaderID)
        {
            if (logger.IsDebugEnabled)
            {
                logger.Debug(string.Format("Start ValidateClaimForTransfer => Claim Reference ID {0} for Claim Transaction Header ID {1} ", claimReference, claimTransactionHeaderID));
            }

            try
            {
                using (IClaimsEntities entities = ClaimsEntitiesFactory.GetIClaimsEntities())
                {
                    Dictionary<string, long> headerIds = entities.GetClaimHeaderIDs(new List<string>() { claimReference });
                    if (headerIds == null || headerIds.Count == 0)
                    {
                        this.InvokeErrorMessage(MessageConstants.CLAIM_INVALID_CLAIMREFERENCE, claimReference);
                    }

                    Dictionary<long, Tuple<string, string, long?>> headerStatuses = entities.GetHeaderStatusCodesByClaimHeaderIDs(new List<long?>() { headerIds[claimReference] });
                    string headerStatus = headerStatuses[headerIds[claimReference]].Item2;
                    headerStatus = string.IsNullOrEmpty(headerStatus) == false ? headerStatus.Trim().ToUpper() : null;

                    if (string.IsNullOrEmpty(headerStatus) == false)
                    {
                        IConfigurationManager configManager = ObjectFactory.Resolve<IConfigurationManager>();
                        string configHeaderStatuses = configManager.AppSettings[ClaimConstants.APP_SETTING_KEY_CLAIMTRANSFERINVALIDCLAIMHEADERSTATUSES];
                        if (string.IsNullOrEmpty(configHeaderStatuses))
                        {
                            throw new ValidationException(string.Format("Missing config property: {0}", ClaimConstants.APP_SETTING_KEY_CLAIMTRANSFERINVALIDCLAIMHEADERSTATUSES));
                        }
                        else
                        {
                            configHeaderStatuses = configHeaderStatuses.ToUpper();
                            List<string> configHeaderStatusesList = configHeaderStatuses.Split(',').ToList<string>();
                            if (configHeaderStatusesList.Any(status => status.Trim().Equals(headerStatus)))
                            {
                                string headerStatusDescription = SystemValueSetCache.GetCodeDescription(headerStatus, SystemValueSetCodeEnum.ClaimHeaderStatus, true);
                                this.InvokeErrorMessage(ClaimConstants.CLAIM_TRANSFER_INVALID_FOR_CLAIMHEADERSTATUSES, claimReference, headerStatusDescription);
                            }
                        }
                    }
                }

                ClaimTransferValidation coreClaimTransferValidation = new ClaimTransferValidation();
                coreClaimTransferValidation.ValidateClaimForTransfer(claimReference, claimTransactionHeaderID);
            }
            finally
            {
                if (logger.IsDebugEnabled)
                {
                    logger.Debug(string.Format("End ValidateClaimForTransfer => Claim Reference ID {0} for Claim Transaction Header ID {1} ", claimReference, claimTransactionHeaderID));
                }
            }
        }

        private void InvokeErrorMessage(string messageAlias, params object[] value)
        {
            string messageBody;
            string messageTitle;
            MessageServiceFactory.GetMessageService().GetMessage(messageAlias, out messageTitle, out messageBody, value);
            throw new ValidationException(messageBody);
        }
    }
}
