using System;
using System.Linq;
using log4net;
using Microsoft.Practices.Unity;
using Newtonsoft.Json.Linq;
using Xiap.Claims.BusinessComponent;
using Xiap.DataMigration.GeniusInterface.AXACS.Entities;
using Xiap.DataMigration.GeniusInterface.AXACS.Gateways;
using Xiap.Framework;
using Xiap.Framework.Data.Underwriting;
using Xiap.Metadata.Data.Enums;
using Xiap.UW.BusinessTransaction;
using ClaimService = Xiap.ClientServices.Facade.ClaimService;
using Xiap.Framework.Data.Common;
using System.Collections.Generic;
using Xiap.Framework.Common.Codes;
using Xiap.Metadata.Data;
namespace Xiap.DataMigration.GeniusInterface.AXACS.Handlers
{
    public class AttachClaimToPolicyHandler
    {
        private static readonly ILog Logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public bool Execute(ClaimHeader claimHeader, Claim claim)
        {
            if (claimHeader == null) throw new ArgumentNullException("claimHeader");
            if (claim == null) throw new ArgumentNullException("claim");
            IUWHeader uwHeader = null;
            if (!FindPolicy(claimHeader, claim, out uwHeader)) return false;
            claim.PolicyHeaderStatusCode = uwHeader.HeaderStatusCode;
            var geniusGateway = GlobalClaimWakeUp.Container.Resolve<IGeniusGateway>();

            var queryProductSectionDetails = ObjectFactory.Resolve<ISystemValueSetQuery<ISectionDetailTypeData>>();

            try
            {
                //SetDateOfLossTypeCode(claimHeader, uwHeader);
                claimHeader.PolicyHeaderID = uwHeader.HeaderID;
                claimHeader.ProposedPolicyReference = uwHeader.HeaderReference;
                foreach (var claimDetail in claim.ClaimDetails)
                {
                    //var components = new SortedList<string, ComponentRequest>();
                    var geniusXClaimDetail = claimHeader.ClaimDetails.SingleOrDefault(cd => cd.ClaimDetailID == claimDetail.GeniusXDetailID);
                    if (geniusXClaimDetail == null)
                    {
                        Logger.ErrorFormat(
                                           "Could not find ClaimDetail\r\n{0}\r\n",
                                           JObject.FromObject(
                                           new{
                                           claim.ClaimReference,
                                           uwHeader.HeaderReference,
                                           claimDetail.GeniusXDetailID,
                                           claimDetail.ClaimDetailType}));
                        return false;
                    }
                    geniusXClaimDetail.PolicyLinkLevel = (short)StaticValues.PolicyLinkLevel.Coverage;

                    var section = uwHeader.ISections.SingleOrDefault(s => s.SectionTypeCode == claimDetail.PolicySectionCode);
                    if (section == null)
                    {
                        Logger.ErrorFormat("Could not find Section\r\n{0}\r\n", 
                            JObject.FromObject(new{claim.ClaimReference, uwHeader.HeaderReference, claimDetail.PolicySectionCode}));
                        return false;
                    }

                    IUWSectionDetail sectionDetail = section.ISectionDetails
                        .FirstOrDefault(
                            sd => string.Equals(sd.SectionDetailTitle, claimDetail.SectionDetailIdentifier, StringComparison.OrdinalIgnoreCase) &&
                                  sd.ICoverages.Any(
                                      c => c.CoverageTypeCode == claimDetail.CoverageCode &&
                                           geniusGateway.CheckDates(c.ExternalReference, claimHeader.DateOfLossFrom.GetValueOrDefault(DateTime.MinValue)) ||
                                           geniusGateway.CheckDates(c.ExternalReference, claimHeader.DateOfLossTo.GetValueOrDefault(DateTime.MinValue))));
                    if (sectionDetail == null)
                    {
                        Logger.WarnFormat("Could not find a matching SectionDetail, trying 'WW'\r\n{0}",
                            JObject.FromObject(
                            new
                                {
                                    claim.ClaimReference,
                                    uwHeader.HeaderReference,
                                    claimDetail.PolicySectionCode,
                                    claimDetail.SectionDetailIdentifier
                                }
                            ));
                        sectionDetail = section.ISectionDetails
                        .FirstOrDefault(sd => string.Equals(sd.SectionDetailTitle, "WW", StringComparison.OrdinalIgnoreCase) &&
                            sd.ICoverages.Any(
                                c => c.CoverageTypeCode == claimDetail.CoverageCode &&
                                    geniusGateway.CheckDates(c.ExternalReference, claimHeader.DateOfLossFrom.GetValueOrDefault(DateTime.MinValue)) ||
                                    geniusGateway.CheckDates(c.ExternalReference, claimHeader.DateOfLossTo.GetValueOrDefault(DateTime.MinValue))));
                    }

                    if (sectionDetail == null)
                    {
                        Logger.WarnFormat("Could not find a matching SectionDetail, trying 'EL UK'\r\n{0}",
                            JObject.FromObject(
                            new
                            {
                                claim.ClaimReference,
                                uwHeader.HeaderReference,
                                claimDetail.PolicySectionCode,
                                claimDetail.SectionDetailIdentifier
                            }
                            ));
                        sectionDetail = section.ISectionDetails
                        .FirstOrDefault(sd => string.Equals(sd.SectionDetailTitle, "EL UK", StringComparison.OrdinalIgnoreCase) &&
                            sd.ICoverages.Any(
                                c => c.CoverageTypeCode == claimDetail.CoverageCode &&
                                    geniusGateway.CheckDates(c.ExternalReference, claimHeader.DateOfLossFrom.GetValueOrDefault(DateTime.MinValue)) ||
                                    geniusGateway.CheckDates(c.ExternalReference, claimHeader.DateOfLossTo.GetValueOrDefault(DateTime.MinValue))));
                    }

                    if (sectionDetail == null)
                    {
                        Logger.WarnFormat("Could not find a matching SectionDetail, trying 'Europe'\r\n{0}",
                            JObject.FromObject(
                            new
                            {
                                claim.ClaimReference,
                                uwHeader.HeaderReference,
                                claimDetail.PolicySectionCode,
                                claimDetail.SectionDetailIdentifier
                            }
                            ));
                        sectionDetail = section.ISectionDetails
                        .FirstOrDefault(sd => string.Equals(sd.SectionDetailTitle, "Europe", StringComparison.OrdinalIgnoreCase) &&
                            sd.ICoverages.Any(
                                c => c.CoverageTypeCode == claimDetail.CoverageCode &&
                                    geniusGateway.CheckDates(c.ExternalReference, claimHeader.DateOfLossFrom.GetValueOrDefault(DateTime.MinValue)) ||
                                    geniusGateway.CheckDates(c.ExternalReference, claimHeader.DateOfLossTo.GetValueOrDefault(DateTime.MinValue))));
                    }

                    if (sectionDetail == null)
                    {
                        Logger.WarnFormat("Could not find a matching SectionDetail using *any* method, picking first SectionDetail\r\n{0}",
                           JObject.FromObject(
                           new
                           {
                               claim.ClaimReference,
                               uwHeader.HeaderReference,
                               claimDetail.PolicySectionCode,
                           }
                           ));
                        sectionDetail = section.ISectionDetails
                                        .FirstOrDefault(sd => queryProductSectionDetails.GetValue(sd.SectionDetailTypeCode).IsSummarySectionDetailType == false);
                    }

                    if (sectionDetail == null)
                    {
                        Logger.ErrorFormat("Could not find SectionDetail\r\n{0}\r\n",
                            JObject.FromObject(
                            new {
                                           /*\tVehicleType={3}\r\n\tAreaCode={4}\r\n]", */
                                           claim.ClaimReference,
                                           uwHeader.HeaderReference,
                                           claimDetail.PolicySectionCode
                                //,claimDetail.VehicleType
                                //,claimDetail.AreaCode
                            }));
                        return false;
                    }

                    if (!string.Equals(sectionDetail.SectionDetailTitle, claimDetail.SectionDetailIdentifier, StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.WarnFormat(
                                          "Fuzzy matched SectionDetail\r\n{0}\r\n",
                                          JObject.FromObject(
                                          new{
                                          /*\tVehicleType={3}\r\n\tAreaCode={4}\r\n]", */
                                          claim.ClaimReference,
                                          uwHeader.HeaderReference,
                                          claimDetail.CmsPolicySectionCode,
                                          claimDetail.PolicySectionCode,
                                          claimDetail.SectionDetailIdentifier,
                                          sectionDetail.SectionDetailTitle
                                //,claimDetail.VehicleType
                                //,claimDetail.AreaCode
                                          }));
                    }

                    var coverage = sectionDetail.ICoverages.FirstOrDefault(
                        c => c.CoverageTypeCode == claimDetail.CoverageCode && 
                            geniusGateway.CheckDates(c.ExternalReference, claimHeader.DateOfLossFrom.GetValueOrDefault(DateTime.MinValue)) ||
                            geniusGateway.CheckDates(c.ExternalReference, claimHeader.DateOfLossTo.GetValueOrDefault(DateTime.MinValue))) ?? 
                            sectionDetail.ICoverages.FirstOrDefault();

                    if (coverage == null)
                    {
                        Logger.ErrorFormat(
                                           "Could not find Coverage\r\n{0}\r\n",
                                           JObject.FromObject(
                                           new{
                                           claim.ClaimReference,
                                           uwHeader.HeaderReference,
                                           claimDetail.PolicySectionCode,
                                           claimDetail.CoverageCode
                                           }));
                        return false;
                    }
                    if (!string.Equals(sectionDetail.SectionDetailTitle, claimDetail.SectionDetailIdentifier, StringComparison.OrdinalIgnoreCase) || !string.Equals(coverage.CoverageTypeCode, claimDetail.CoverageCode, StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.WarnFormat(
                                          "Fuzzy matched CoverageType\r\n{0}\r\n",
                                          JObject.FromObject(
                                          new {
                                          /*\tVehicleType={3}\r\n\tAreaCode={4}\r\n]", */
                                          claim.ClaimReference,
                                          uwHeader.HeaderReference,
                                          claimDetail.CmsPolicySectionCode,
                                          claimDetail.PolicySectionCode,
                                          claimDetail.SectionDetailIdentifier,
                                          sectionDetail.SectionDetailTitle,
                                          claimDetail.CoverageCode,
                                          coverage.CoverageTypeCode
                                //,claimDetail.VehicleType
                                //,claimDetail.AreaCode
                                          }));
                    }
                    geniusXClaimDetail.PolicyCoverageID = coverage.CoverageID;
                    geniusXClaimDetail.PolicySectionDetailID = sectionDetail.SectionDetailID;
                    geniusXClaimDetail.PolicySectionID = section.SectionID;
                }

                foreach (var sectionDetail in uwHeader.ISections.SelectMany(s => s.ISectionDetails.Where(sd => queryProductSectionDetails.GetValue(sd.SectionDetailTypeCode).IsSummarySectionDetailType == false)))
                {
                    var dateOfLossTypeCode = "O";
                    var sectionDetailCustomCode03 = GlobalClaimWakeUp.Container.Resolve<IGeniusGateway>().GetSectionDetailDateOfLossTypeCode(sectionDetail.ExternalReference);
                    if (!string.IsNullOrEmpty(sectionDetailCustomCode03) && (sectionDetailCustomCode03 == "M" || sectionDetailCustomCode03 == "O"))
                    {
                        dateOfLossTypeCode = sectionDetailCustomCode03;
                    }
                    if (dateOfLossTypeCode != claimHeader.DateOfLossTypeCode)
                    {
                        claimHeader.DateOfLossTypeCode = dateOfLossTypeCode;
                        Logger.InfoFormat("Setting Claim DATEOFLOSSTYPECODE \r\n{0}",
                            JObject.FromObject(new { ClaimReference = claimHeader.ClaimReference, DateOfLossTypeCode = dateOfLossTypeCode }));
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                claim.ClaimIsAttachedToPolicy = false;
                claim.CustomCode18 = "F02";
                claim.ClaimProcessingFailed = true;
                claim.FailureReason = "Could not attach Claim to Policy";
                Logger.WarnFormat("Claim to Policy attachment failed:\r\n{0}\r\n", JObject.FromObject(new{claim.ClaimReference, claim.PolicyNumber, @Exception=ex}));
                return false;
            }

            // Create the lossBroker name involvement
            try
            {
                var transaction = UWBusinessTransactionFactory.DisplayPolicy(uwHeader.HeaderReference, false, VersionSelectionParameters.SelectLatestVersion);
                var lossBroker = transaction.Header.NameInvolvements.FirstOrDefault(ni => ni.NameInvolvementType == (short)StaticValues.NameInvolvementType_UWNameInvolvement.MajorBroker);
                if (lossBroker != null && !claimHeader.NameInvolvementExists((short)StaticValues.NameInvolvementType_ClaimNameInvolvement.LossBroker, lossBroker.NameID))
                {
                    if (lossBroker.NameID != null)
                    { 
                        var claimInvolvement = claimHeader.AddNewClaimInvolvement(StaticValues.LinkableComponentType.NameInvolvement);
                        var claimProduct = claimHeader.GetProduct();
                        // TGB: New changes to XIAP mean you now must supply the NameUsageTypeCode here. For Loss Broke it is 'Broker', which is UBK
                        claimInvolvement.AddClaimNameInvolvement(claimProduct.ProductVersionID, (short)StaticValues.NameInvolvementType_ClaimNameInvolvement.LossBroker, lossBroker.NameID, "UBK");
                        Logger.InfoFormat("LOSSBROKER setup on Claim\r\n{0}\r\n", JObject.FromObject(new { claim.ClaimReference, lossBroker.NameID }));
                    }
                    else
                    {
                        Logger.WarnFormat("Could not setup LOSSBROKER on Claim\r\n{{ClaimReference: {0}\r\n}}", claim.ClaimReference);
                    }
                }
                transaction.Cancel();
            }
            catch (Exception ex)
            {
                Logger.WarnFormat("Error creating LOSSBROKER for '{0}'\r\n[\r\n\tException={1}\r\n]", claim.ClaimReference, ex);
                return false;
            }
            

            Logger.InfoFormat("Claim to Policy attachment success:\r\n{0}\r\n", 
                JObject.FromObject(
                new {claim.ClaimReference, claim.PolicyNumber}
                ));
            return true;
        }

        //private static void SetDateOfLossTypeCode(ClaimHeader claimHeader, IUWHeader uwHeader)
        //{
        //    var claimOccurredDuringPolicyDuration = uwHeader.InceptionDate.GetValueOrDefault(DateTime.MinValue) <= claimHeader.DateOfLossFrom.GetValueOrDefault(DateTime.MinValue)
        //            && uwHeader.ExpiryDate.GetValueOrDefault(DateTime.MinValue) >= claimHeader.DateOfLossFrom.GetValueOrDefault(DateTime.MinValue);
        //    var claimAdvisedDuringPolicyDuration = uwHeader.InceptionDate.GetValueOrDefault(DateTime.MinValue) <= claimHeader.ClaimAdvisedDate.GetValueOrDefault(DateTime.MinValue)
        //            && uwHeader.ExpiryDate.GetValueOrDefault(DateTime.MinValue) >= claimHeader.ClaimAdvisedDate.GetValueOrDefault(DateTime.MinValue);
        //    if (claimOccurredDuringPolicyDuration)
        //    {
        //        claimHeader.DateOfLossTypeCode = "O";
        //    }
        //    else if (claimAdvisedDuringPolicyDuration)
        //    {
        //        claimHeader.DateOfLossTypeCode = "M";
        //    }
        //    else
        //    {
        //        claimHeader.DateOfLossTypeCode = "O";
        //    }
        //}

        private static readonly object SyncLock = new object();
        private static bool FindPolicy(ClaimHeader claimHeader, Claim claim, out IUWHeader uwHeader)
        {
            lock (SyncLock)
            {
                var claimService = ObjectFactory.Resolve<ClaimService>();
                try
                {
                    Logger.InfoFormat("Looking for Policy based on Claim\r\n{0}\r\n", 
                        JObject.FromObject(
                        new {claim.ClaimReference, claim.PolicyNumber}));
                    uwHeader = claimService.GetPolicyDataForCoverageVerification(claim.PolicyNumber, XiapConstants.XIAP_DATASOURCE, null);
                    
                    claim.PolicyShellWasCreated = false;
                    return true;
                }
                catch (Exception ex)
                {
                    //try
                    //{
                    //    Logger.InfoFormat("Could not find Policy in Genius.X, going to Genius\r\n{0}\r\n", 
                    //        JObject.FromObject(new {claim.ClaimReference, claim.PolicyNumber}));
                    //    var claimProduct = claimHeader.GetProduct();
                    //    var linkedUwProduct = claimProduct.Links.First(l => l.ProductLinkType == (short)StaticValues.ProductLinkType.ClaimtoPolicy);
                    //    var tran = UWBusinessTransactionFactory.CreatePolicy(linkedUwProduct.ProductCode, DateTime.UtcNow, false);
                    //    var header = tran.Header;
                    //    header.HeaderReference = claim.PolicyNumber;
                    //    var updater = ObjectFactory.Resolve<UpdateFromGeniusPlugin>();
                    //    updater.ProcessComponent(header, ProcessInvocationPoint.Virtual, 0);
                        
                    //    //_updater.Handle(header);
                    //    tran.Complete(false);
                    //    claim.PolicyShellWasCreated = true;
                    //    uwHeader = claimService.GetPolicyDataForCoverageVerification(claim.PolicyNumber, XiapConstants.XIAP_DATASOURCE, null);
                    //    return true;
                    //}
                    //catch (Exception ex)
                    //{
                        claim.PolicyShellWasCreated = false;
                        claim.ClaimIsAttachedToPolicy = false;
                        claim.CustomCode18 = "F01";
                        claim.ClaimProcessingFailed = true;
                        claim.FailureReason = "Policy does not exist";
                        Logger.ErrorFormat("Exception creating policy from Genius policy \r\n{0}\r\n", 
                            JObject.FromObject(new{ex.Message, ex.StackTrace}));
                        uwHeader = null;
                        return false;
                    //}
                }
            }
        }
    }
}
