using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Practices.Unity;
using Xiap.Framework.Logging;
using Xiap.Framework.Metadata;
using Xiap.Metadata.Data.Enums;
using XIAP.Frontend.Infrastructure;
using XIAP.Frontend.Infrastructure.DataMapping;
using XIAP.FrontendModules.Claims.Controller;
using XIAP.FrontendModules.Claims.Data;
using XIAP.FrontendModules.Claims.Model;
using XIAP.FrontendModules.Claims.Service;
using XIAP.FrontendModules.Common;
using XIAP.FrontendModules.Common.ClaimService;
using XIAP.FrontendModules.Common.Controller;
using Xiap.Framework.DecisionTable;
using GeniusX.AXA.FrontendModules.Claims.Resources;
using Xiap.ClientServices.Facade.Common;
using Xiap.Framework.Data;
using XIAP.Frontend.CoreControls;
using XIAP.Frontend.CommonInfrastructure.Controller.CoverageVerification;

namespace GeniusX.AXA.FrontendModules.Claims.Controller
{
    public class AXAClaimCoverageVerificationController : ClaimCoverageVerificationController
    {
        #region Private variables
        private static readonly ILogger _Logger = LoggerFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private const string TYPE_OF_LOSS_AND_COVERAGE_TYPE_DECISION_TABLE_CODE = "CLMTOLCTV";
        private const string MOTOR_CLAIM_PRODUCT_CODE = "CGBIMO";
        private const int CLAIM_HEADER_ANALYSIS_CODE_09_VS = 100120;
        private const int CLAIM_HEADER_ANALYSIS_CODE_02_VS = 100058;
        private ClaimsPayloadManager _payloadManager;
        private AppModel _appModel;
        private IMetadataClientService _metadataService;
        private bool continueCoverageVerification = true;
        #endregion

        public AXAClaimCoverageVerificationController(ClaimCoverageVerificationModel claimCoverageVerificationModel, IClaimClientService claimClientService, IUnityContainer container, IMetadataClientService metaClientService, ClaimsPayloadManager payloadManager, AppModel appModel)
            : base(claimCoverageVerificationModel, claimClientService, metaClientService, payloadManager, container)
        {
            this._appModel = appModel;
            this._metadataService = metaClientService;
            this._payloadManager = payloadManager;
        }

        public override void OnOK(object obj, EventArgs e)
        {
            this.LoadDeductibleReasonCodes(() =>
                {
                    if (continueCoverageVerification)
                    {
                        base.OnOK(obj, e);
                    }
                });
        }

        public override void OnNext(object obj, EventArgs e)
        {
            if (this.Model.ComponentChecked)
            {
                this.PerformCoverageVerificationValidation(() =>
                {
                    base.OnNext(obj, e);
                });
            }
            else
            {
                base.OnNext(obj, e);
            }
        }

        public override void OnPrevious(object obj, EventArgs e)
        {
            if (this.Model.ComponentChecked)
            {
                this.PerformCoverageVerificationValidation(() =>
                    {
                        base.OnPrevious(obj, e);
                    });
            }
            else
            {
                base.OnPrevious(obj, e);
            }
        }

        public override void Finish(object obj, EventArgs e)
        {
            this.PerformCoverageVerificationValidation(() =>
                {
                    if (!this.Model.IsMultipleSelection)
                    {
                        if (this.Model.SelectedCoverageVerificationItems.Count == 1)
                        {
                            this.LoadDeductibleReasonCodes(() =>
                            {
                                if (continueCoverageVerification)
                                {
                                    base.Finish(obj, e);
                                }
                            });
                        }
                        else
                        {
                            base.Finish(obj, e);
                        }
                    }
                    else
                    {
                        base.Finish(obj, e);
                    }
                });
        }

        private void PerformCoverageVerificationValidation(Action callback)
        {
            string sectionDetailClassificationCode03 = string.Empty;
            string externalRef = this.PerformCoverageVerificationDatesValidation();

            if (!string.IsNullOrWhiteSpace(externalRef))
            {
                CoverageVerificationBaseSelectedItems selectedItem = this.Model.SelectedCoverageVerificationItems[this.Model.ClaimDetailIndex];
                if (selectedItem != null)
                {
                    UWCoverageData uwCoverageData = selectedItem.PolicyComponentData as UWCoverageData;
                    ClaimHeaderData claimHeaderData = this.Model.ClaimHeaderDto.Data as ClaimHeaderData;
                    UWHeaderData uwHeaderData = selectedItem.UWHeaderData as UWHeaderData;
                    foreach (var section in uwHeaderData.Sections)
                    {
                        if (section.SectionDetails.IsNullOrEmpty() == false)
                        {
                            foreach (var sectionDetail in section.SectionDetails)
                            {
                                if (sectionDetail.Coverages.IsNullOrEmpty() == false)
                                {
                                    foreach (var coverage in sectionDetail.Coverages)
                                    {
                                        if (coverage.ExternalReference != null && coverage.ExternalReference == uwCoverageData.ExternalReference)
                                        {
                                            sectionDetailClassificationCode03 = sectionDetail.SectionDetailClassificationCode03;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    /* Parameters passed to virtual process are as follows. These will be used by the plugins.
                     * 
                     * 0 = externalREf
                     * 1 = PolicyLinkLevel
                     * 2 = CoverageTypeCode
                     * 3 = sectionDetailClassificationCode03
                     * 4 = ExternalReference for coverage
                     */
                    this._payloadManager.VirtualProcess = VirtualProcessRequestBuilder.BuildComponentVirtualProcessRequest(this._payloadManager,
                                                                                                          claimHeaderData.DataId,
                                                                                                          "CoverageVerificationValidation",
                                                                                                          false,
                                                                                                          true,
                                                                                                          externalRef,
                                                                                                          selectedItem.PolicyLinkLevel.ToString(),
                                                                                                          uwCoverageData.CoverageTypeCode,
                                                                                                          sectionDetailClassificationCode03,
                                                                                                          uwCoverageData.ExternalReference);
                    this.Model.IsBusy = true;
                    this._payloadManager.SynchroniseData(this.Model.ClaimHeaderDto,
                        this.Model.TransactionId,
                        (Response r) =>
                        {
                            this.Model.IsBusy = false;
                            if (r.Status == BusinessTransactionStatus.Valid && r.Messages.IsNullOrEmpty())
                            {
                                if (callback != null)
                                {
                                    callback();
                                }
                            }
                            else
                            {
                                XIAPMessageBox.ShowValidationErrors(r.Messages, null);
                            }
                        });
                }
                else
                {
                    if (callback != null)
                    {
                        callback();
                    }
                }
            }
        }

        private string PerformCoverageVerificationDatesValidation()
        {
            string selectedItemID = string.Empty;
            DateTime? startDate = null;
            DateTime? endDate = null;

            if (this.Model.SelectedCoverageVerificationItems != null)
            {
                CoverageVerificationBaseSelectedItems selectedItem = this.Model.SelectedCoverageVerificationItems[this.Model.ClaimDetailIndex];
                switch (selectedItem.PolicyLinkLevel)
                {
                    case StaticValues.PolicyLinkLevel.Header:
                        if (selectedItem.IsHeaderChecked)
                        {
                            selectedItemID = selectedItem.HeaderExternalRef;
                        }

                        break;
                    case StaticValues.PolicyLinkLevel.Section:
                        if (selectedItem.IsSectionChecked)
                        {
                            selectedItemID = selectedItem.SectionExternalRef;
                        }

                        break;
                    case StaticValues.PolicyLinkLevel.SectionDetail:
                        if (selectedItem.IsSectionDetailChecked)
                        {
                            selectedItemID = selectedItem.SectionDetailExternalRef;
                        }

                        break;
                    case StaticValues.PolicyLinkLevel.Coverage:
                        if (selectedItem.IsCoverageChecked)
                        {
                            selectedItemID = selectedItem.CoverageExternalRef;
                        }

                        break;
                }

                if (!string.IsNullOrEmpty(selectedItemID))
                {
                    foreach (UWSectionData section in this.Model.UWHeaderData[0].Sections)
                    {
                        if (section.ExternalReference == selectedItemID)
                        {
                            startDate = section.SectionStartDate;
                            endDate = section.SectionEndDate;
                            break;
                        }
                        else
                        {
                            foreach (UWSectionDetailData sd in section.SectionDetails)
                            {
                                if (sd.ExternalReference == selectedItemID)
                                {
                                    startDate = sd.SectionDetailStartDate;
                                    endDate = sd.SectionDetailEndDate;
                                    break;
                                }
                                else
                                {
                                    foreach (UWCoverageData cov in sd.Coverages)
                                    {
                                        if (cov.ExternalReference == selectedItemID)
                                        {
                                            startDate = cov.CoverageStartDate;
                                            endDate = cov.CoverageEndDate;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (this.Model.CoverageArguments.DateOfLoss < startDate || this.Model.CoverageArguments.DateOfLoss > endDate)
                    {
                        string message = string.Format(GeniusX.AXA.FrontendModules.Claims.Resources.StringResources.DateOutside_CoverPeriod, this.Model.CoverageArguments.DateOfLoss.ToString(), startDate.ToString(), endDate.ToString());
                        throw new InvalidOperationException(message);
                    }
                }
            }

            return selectedItemID;
        }

        private void LoadDeductibleReasonCodes(Action callback)
        {
            ClaimHeaderData claimHeaderData = (ClaimHeaderData)this.Model.ClaimHeaderDto.Data;
            if (string.IsNullOrEmpty(claimHeaderData.ClaimHeaderAnalysisCode08))
            {
                claimHeaderData.ProposedPolicyReference = this.Model.CoverageArguments.PolicyReference;
                this._payloadManager.VirtualProcess = VirtualProcessRequestBuilder.BuildComponentVirtualProcessRequest(this._payloadManager, claimHeaderData.DataId, "LoadDeductibleReasonCodes");
                this._payloadManager.VirtualProcess.SkipComponentValidation = true;
                this.Model.IsBusy = true;
                this._payloadManager.SynchroniseData(
                    this.Model.ClaimHeaderDto,
                    this.Model.CoverageArguments.TransactionId,
                    response =>
                    {
                        this.ShowReasonCodesForComponent(callback);
                        this.Model.IsBusy = false;
                    },
                        this.HandleError);
            }
        }

        private void ShowReasonCodesForComponent(Action callback)
        {
            // reason code selection will only be performed if reason code not populated on claim header (otherwise ReasonCodes will be blank)
            try
            {
                ClaimHeaderData claimHeaderData = (ClaimHeaderData)this.Model.ClaimHeaderDto.Data;
                if (claimHeaderData.CodeRowList != null && claimHeaderData.CodeRowList.Count > 0)
                {
                    UWHeaderData headerData = this.Model.UWHeaderData[0];
                    List<string> externalRefList = this.GetExternalReference(headerData);
                    string externalRefWithDeductibles = string.Empty;
                    if (externalRefList.Count > 0)
                    {
                        foreach (string extRef in externalRefList)
                        {
                            if (!string.IsNullOrEmpty(extRef))
                            {
                                if (claimHeaderData.CodeRowList.ContainsKey(extRef))
                                {
                                    externalRefWithDeductibles = extRef;
                                    break;
                                }
                            }
                        }

                        if (externalRefWithDeductibles != string.Empty)
                        {
                            DeductibleReasonCodeArg args = new DeductibleReasonCodeArg();
                            args.DeductibleReasonList = claimHeaderData.CodeRowList[externalRefWithDeductibles].ToObservableCollection<CodeRow>();
                            // show popup, select value and update claimheader
                            this.Navigator.StartNew("DeductibleReasonCodesGraph", new TaskArgumentsHolder(null, args, null, (controllerArgs) => this.OnReasonCodeSelectComplete(callback, controllerArgs), null));
                        }
                        else
                        {
                            callback();
                        }
                    }
                    else
                    {
                        callback();
                    }
                }
                else
                {
                    callback();
                }
            }
            catch (Exception ex)
            {
                _Logger.Error(ex);
                throw;
            }
        }

        private List<string> GetExternalReference(UWHeaderData headerData)
        {
            // Return list of component ext ref plus the parent ext ref for the selected policy component
            // This enables us to search back up the hierarchy of the policy if no reason codes are found at exact attachment level
            List<string> extRefLookup = new List<string>();
            if (headerData.IsChecked)
            {
                extRefLookup.Add(headerData.ExternalReference);
            }
            else
            {
                foreach (UWSectionData section in headerData.Sections)
                {
                    if (section.IsChecked)
                    {
                        extRefLookup.Add(section.ExternalReference);
                        extRefLookup.Add(headerData.ExternalReference);
                        return extRefLookup;
                    }
                    else
                    {
                        foreach (UWSectionDetailData sd in section.SectionDetails)
                        {
                            if (sd.IsChecked)
                            {
                                extRefLookup.Add(sd.ExternalReference);
                                extRefLookup.Add(section.ExternalReference);
                                extRefLookup.Add(headerData.ExternalReference);
                                return extRefLookup;
                            }
                            else
                            {
                                foreach (UWCoverageData cov in sd.Coverages)
                                {
                                    if (cov.IsChecked)
                                    {
                                        extRefLookup.Add(cov.ExternalReference);
                                        extRefLookup.Add(sd.ExternalReference);
                                        extRefLookup.Add(section.ExternalReference);
                                        extRefLookup.Add(headerData.ExternalReference);
                                        return extRefLookup;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return extRefLookup;
        }

        private void OnReasonCodeSelectComplete(Action callBack, ControllerArgs args)
        {
            if (args != null)
            {
                DeductibleReasonCodeArg reasonCodeArgs = (DeductibleReasonCodeArg)args;
                if (!string.IsNullOrEmpty(reasonCodeArgs.SelectedReasonCode))
                {
                    ClaimHeaderData claimHeaderData = (ClaimHeaderData)this.Model.ClaimHeaderDto.Data;
                    claimHeaderData.ClaimHeaderAnalysisCode08 = reasonCodeArgs.SelectedReasonCode;
                }
            }
            else
            {
                this.continueCoverageVerification = false;
            }

            if (callBack != null)
            {
                callBack();
            }
        }

        private ObservableCollection<CodeRow> ResolveReasonCodes(string reasonCodeListString, string externalReference)
        {
            ObservableCollection<CodeRow> reasonCodes = new ObservableCollection<CodeRow>();
            List<string> entries = new List<string>();
            int extRefStart = reasonCodeListString.IndexOf(externalReference);
            if (extRefStart != -1)
            {
                // Reason codes for component are found
                int nextEntryStart = reasonCodeListString.IndexOf("Start", extRefStart);
                string reasonCodesOnly;
                if (nextEntryStart == -1)
                {
                    reasonCodesOnly = reasonCodeListString.Substring(extRefStart + externalReference.Length + 1);
                }
                else
                {
                    reasonCodesOnly = reasonCodeListString.Substring(extRefStart + externalReference.Length + 1, nextEntryStart - 1);
                }

                char[] splitter = { ',' };
                string[] reasonCodeList = reasonCodesOnly.Split(splitter, StringSplitOptions.RemoveEmptyEntries);
                foreach (string code in reasonCodeList)
                {
                    reasonCodes.Add(new CodeRow() { Code = code });
                }

                return reasonCodes;
            }

            return reasonCodes;
        }
    }
}
