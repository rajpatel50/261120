using System.Linq;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework.Common;
using Xiap.Framework.DPService;
using Xiap.Framework.Logging;
using Xiap.Framework.Validation;
using Xiap.Metadata.BusinessComponent;
using Xiap.Metadata.Data.Enums;
using Xiap.Framework;
using Xiap.Framework.Security;

namespace GeniusX.AXA.Claims.BusinessLogic
{
	public class ClaimsDocumentLogInfoUpdater : ICustomDocumentLogInfoUpdater
	{
		private static readonly ILogger logger = LoggerFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Check the Document for any Referral Document segments. If any are found that have a referee who is a valid user in GeniusX
        /// apply that user as the Task Initial User on the Document Log Info object.
        /// </summary>
        /// <param name="documentLogInfo">Document lof info type</param>
        /// <param name="document">document type</param>
		public void UpdateDocumentLogInfo(DocumentLogInfo documentLogInfo, Document document)
		{
			ArgumentCheck.ArgumentNullCheck(documentLogInfo, "documentLogInfo");
			ArgumentCheck.ArgumentNullCheck(document, "document");

            logger.Debug("Custom DocumentLogInfo updater invoked, object: GeniusX.AXA.Claims.BusinessLogic.ClaimsDocumentLogInfoUpdater");
			if (document != null)
			{
                // Only apply to Claim Header documents
				if (documentLogInfo.DocumentLevel == StaticValues.DocumentLevel.ClaimHeader)
				{
                    // Get claim document object via the Document ID.
					ClaimDocument claimDocument = ObjectFactory.Resolve<IClaimsQuery>().GetClaimDocument(document.DocumentID);
					if (claimDocument.DocumentTextSegments.Count > 0)
					{
                        // We have text segments to parse through so collect the ReferralDocument types we need from the application configuration
						StaticValues.TextSegmentType textSegmentType = ClaimsBusinessLogicHelper.ResolveEnumFromConfig<StaticValues.TextSegmentType>("ReferralDocumentTextSegmentType");
						string documentTextSegmentWordingTypeCode = ClaimsBusinessLogicHelper.ResolveMandatoryConfig<string>("ReferralDocumentTextSegmentWordingTypeCode");

                        if (logger.IsDebugEnabled)
                        {
                            logger.Debug("Selected Text Segment : " + textSegmentType.ToString());
                            logger.Debug("Selected Text Segment Num val : " + (short)textSegmentType);
                            logger.Debug("Selected WordingTypeCode : " + documentTextSegmentWordingTypeCode);
                        }

						var documentTextSegment = claimDocument.DocumentTextSegments.Where(a => a.TextSegmentType == (short)textSegmentType);

                        // Cycle through all Referral Document text segments.
                        foreach (IDocumentTextSegment textSegment in documentTextSegment)
						{
							using (MetadataEntities metaDataEntities = MetadataEntitiesFactory.GetMetadataEntities())
							{
                                // Get the WordingCode from the MetaData in the database for this text segment's Wording Version 
								var wordingCode = (from wordversion in metaDataEntities.WordingVersion 
									join word in metaDataEntities.Wording on wordversion.Wording.Code equals word.Code
									where wordversion.IsLatestVersion.Value && wordversion.WordingVersionID == textSegment.WordingVersionID
									select word.Code).FirstOrDefault();

                                if (logger.IsDebugEnabled)
                                {
                                    logger.Debug("DocumentTextSegment WordingVersionID : " + textSegment.WordingVersionID);
                                }

                                // Check the wordingCode matches the Referral Document type retrieved from the configuration
                                if (!string.IsNullOrWhiteSpace(wordingCode) && documentTextSegmentWordingTypeCode == wordingCode.ToString())
                                {
                                    logger.Debug("Document WordingCode : " + wordingCode);
                                    string cusRef1 = ((ClaimDocumentTextSegment)textSegment).CustomReference01; // ((ClaimDocumentTextSegment)textSegment).CustomReference01 = Referee
                                    if (!string.IsNullOrWhiteSpace(cusRef1))
                                    {
                                        if (logger.IsDebugEnabled)
                                        {
                                            logger.Debug("DocumentTextSegment CustomReference01 : " + cusRef1);
                                        }

                                        // We have a Referee value on the text segement so get the User object using the Referee as the UserIdentity 
                                        var userDetail = UserCacheService.GetUserByIdentity(cusRef1);
                                        if (userDetail != null)
                                        {
                                            if (logger.IsDebugEnabled)
                                            {
                                                logger.Debug("DocumentLogInfo TaskInitialUserID : " + userDetail.UserId);
                                            }

                                            // The referee is a valid user in GeniusX so set them as the TaskInitialUser in the Document Log Info
                                            documentLogInfo.TaskInitialUserID = userDetail.UserId;
                                        }
                                    }

                                    // Only process the first text segment found that is a referral type.
                                    break;
                                }
							}
						}
					}
				}
			}
		}
	}
}
