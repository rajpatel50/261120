using System;
using System.Net;
using System.Xml;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.Logging;
using Xiap.Framework.ProcessHandling;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    /// <summary>
    /// ClaimWakeUp Processing
    /// This class handles claim wake up processing. The idea is that we make a single request to the Claim WakeUp Service passing in our claim reference.
    /// This is an asynchronous request, so we don't bother about a response. The results of this request will be logged in the text log file 
    /// but we would only expect to check this if some kind of issue was reported by the user when they looked at the claim.
    /// </summary>
    public class ClaimsReviewTaskProcessHandler : AbstractComponentPlugin  
    {
        private static readonly ILogger _Logger = LoggerFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        // These two are set in the ClientServices and ServiceHost web.configs, in the AppSettings
        private const string ClaimProcessorUri = "ClaimProcessorUri";
        private const string ClaimProcessorSoapHeaderUri = "ClaimProcessorSoapHeaderUri";

        public override ProcessResultsCollection ProcessComponent(IBusinessComponent component, ProcessInvocationPoint point, int pluginId, ProcessParameters processParameters)
        {
            if (_Logger.IsInfoEnabled)
            {
                _Logger.Info(string.Format("ClaimReviewTaskProcessHandler - ProcessComponent Called"));
            }

            ClaimHeader header = null;
            PluginHelper<IBusinessComponent> pluginHelper = null;

            if (component.GetType() == typeof(ClaimHeader))
            {
                header = (ClaimHeader)component;
                pluginHelper = new PluginHelper<IBusinessComponent>(point, header, new ProcessResultsCollection());
                this.ProcessClaimForReopen(header, pluginHelper);
            }

            return pluginHelper.ProcessResults;
        }

        /// <summary>
        /// If this claim is Migrated (ClaimHeader.CustomCode19 == 'C') and it's not yet been processed, or it was processed and that 
        /// resulted in an F01 or F02 error (ClaimHeader.CustomCode18 is NULL, whitespace, F01 or F02), and this is a closed claim
        /// we should go ahead and reopen it.
        /// </summary>
        /// <param name="header">ClaimHeader Object</param>
        /// <param name="pluginHelper">PluginHelper Object</param>
        private void ProcessClaimForReopen(ClaimHeader header, PluginHelper<IBusinessComponent> pluginHelper)
        {
            if (ClaimsBusinessLogicHelper.CheckMigratedCloseClaim(header))
            {
                try
                {
                    if (_Logger.IsInfoEnabled)
                    {
                        _Logger.Info(string.Format("Initiating migrated claim processing for claim {0}.", header.ClaimReference));
                    }

                    this.InitiateClaimMigrationProcessing(header);
                }
                catch (Exception err)
                {
                    _Logger.Error("Failed on Claim WakeUp Processing");
                    _Logger.Error(err);

                    pluginHelper.AddError(ClaimConstants.FAILED_TO_INITIATE_MIGRATION_PROCESSING, header.ClaimReference);
                }
            }
        }

        private void InitiateClaimMigrationProcessing(ClaimHeader header)
        {
            using (var client = new WebClient())
            {
                System.Xml.XmlDocument xmlDoc = new System.Xml.XmlDocument();

                if (_Logger.IsInfoEnabled)
                {
                    _Logger.Info(string.Format("ClaimReviewTaskProcessHandler: Calling out to Wake Up service for claim {0}.", header.ClaimReference));
                }

                string soapRequest = string.Format(@"<s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"">
                                        <s:Body>
                                            <ProcessClaim xmlns=""http://tempuri.org/"">
			                                    <ClaimReference>{0}</ClaimReference>
                                            </ProcessClaim>
                                        </s:Body>
                                    </s:Envelope>", header.ClaimReference);

                XmlReader reader = System.Xml.XmlReader.Create(new System.IO.StringReader(soapRequest));
                xmlDoc.Load(reader);

                string data = xmlDoc.InnerXml;

                Uri uri = new Uri(ClaimsBusinessLogicHelper.ResolveMandatoryConfig<string>(ClaimProcessorUri));
                client.Headers.Add("Content-Type", "text/xml;charset=utf-8");
                client.Headers.Add("SOAPAction", string.Format("\"{0}\"", ClaimsBusinessLogicHelper.ResolveMandatoryConfig<string>(ClaimProcessorSoapHeaderUri)));
                client.UploadString(uri, data);
            }
        }
    }
}
