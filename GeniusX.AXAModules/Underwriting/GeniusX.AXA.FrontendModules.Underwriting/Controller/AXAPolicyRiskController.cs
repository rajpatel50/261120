using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using GeniusX.AXA.FrontendModules.Underwriting.Model;
using GeniusX.AXA.FrontendModules.Underwriting.Resources;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Unity;
using Xiap.ClientServices.Facade.Common;
using Xiap.Framework.Data;
using Xiap.Framework.Logging;
using Xiap.Metadata.Data.Enums;
using XIAP.Frontend.Infrastructure;
using XIAP.Frontend.Infrastructure.Configuration;
using XIAP.Frontend.Infrastructure.DataMapping;
using XIAP.Frontend.Infrastructure.Events;
using XIAP.Frontend.Infrastructure.Rules;
using XIAP.Frontend.Infrastructure.Search;
using XIAP.Frontend.Infrastructure.Validation;
using XIAP.FrontendModules.Application;
using XIAP.FrontendModules.Common;
using XIAP.FrontendModules.Common.UWService;
using XIAP.FrontendModules.Underwriting;
using XIAP.FrontendModules.Underwriting.Controller;
using XIAP.FrontendModules.Underwriting.Data;
using XIAP.FrontendModules.Underwriting.Service;
using XIAP.Frontend.CoreControls;

namespace GeniusX.AXA.FrontendModules.Underwriting.Controller
{
    public class AXAPolicyRiskController : PolicyRiskController
    {
        #region Private variables

        // This regular expression identifies the following characters or their combinations as invalid characters for a filename \/:*?"<>|                 
        private const string PATTERN = @"[\\\/:\*\?""<>|]";
        private const string PRODUCT_GBIPC = "GBIPC";
        private const string PRODUCT_GBIMO = "GBIMO";
        private const string POLICY_SUMMARY_URI = "PolicySummaryLocation";
        private const string AXACUSTOM_WEBPAGES = "AXACustomWebPages";

        private readonly UwPayloadManager _payloadManager;
        private static readonly ILogger _Logger = LoggerFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private AXARiskModel riskModel;

        #endregion

        public AXAPolicyRiskController(IUnityContainer container, ApplicationModel applicationModel, IRiskService riskService, ISearchServiceHandler searchservicehandler, IUwNameSearchHandler uwNameSearchHandler, IMetadataClientService metadataService, AXARiskModel policyWizardModel, UwPayloadManager payloadManager, UwRetrievalManager retrievalManager, AppModel appModel, IUwViewResolver viewresolver, IEventAggregator eventAggregator, IShellRulesHelper rulesHelper, IConverter<Xiap.Metadata.Data.Enums.StaticValues.ClaimLinkLevel, PolicyAttachmentGroupingLevel> claimLinkLevelPolicyAttachmentLevelConverterParam)
            : base(applicationModel, riskService, searchservicehandler, uwNameSearchHandler, metadataService, policyWizardModel, payloadManager, retrievalManager, appModel, viewresolver, eventAggregator, rulesHelper, container, claimLinkLevelPolicyAttachmentLevelConverterParam)
        {
            this.riskModel = policyWizardModel;
            this.riskModel.OnUpdateFromGeniusClick += new EventHandler<CommandEventArgs<HeaderDto>>(this.RiskModel_OnUpdateFromGeniusClick);
            this.riskModel.OnPolicySummaryClick += new EventHandler<CommandEventArgs<HeaderDto>>(this.RiskModel_OnPolicySummaryClick);
            this._payloadManager = payloadManager;
        }

        private static string GetTitle(string headertitle)
        {
            string result = null;
            string truncatedTitle = null;
            Regex rgx = new Regex(PATTERN);

            if (!string.IsNullOrEmpty(headertitle))
            {
                // each regular expression match is replaced with a space
                if (Regex.IsMatch(headertitle, PATTERN))
                {
                    result = rgx.Replace(headertitle, " ");
                }
                else
                {
                    result = headertitle;
                }
            }

            if (!string.IsNullOrEmpty(result))
            {
                if (result.Length > 50)
                {
                    truncatedTitle = result.Substring(0, 50);
                }
                else
                {
                    truncatedTitle = result;
                }
            }
            else
            {
                truncatedTitle = null;
            }

            return truncatedTitle;
        }

        private static void OpenPolicyUrl(string url)
        {
            if (!string.IsNullOrEmpty(url))
            {
                Uri uri = new Uri(url);
                System.Windows.Browser.HtmlPage.Window.Navigate(uri, "_blank");
            }
        }

        private void RiskModel_OnPolicySummaryClick(object sender, CommandEventArgs<HeaderDto> e)
        {
            string ProductType = null;
            string headertitle = null;
            string separator = " - ";
            string headerReference = null;

            HeaderVersionDto headerVersionContainer = this.Model.HeaderDto.HeaderVersions.FirstOrDefault(a => a.HeaderVersionData.IsLatestVersion == true);
            if (headerVersionContainer != null)
            {
                headertitle = (headerVersionContainer.Data as HeaderVersionData).HeaderTitle;
            }

            string truncatedTitle = GetTitle(headertitle);
            headerReference = this.Model.HeaderData.HeaderReference;
            string spreadsheetFileName = StringResources.POLICY_SUMMARY + separator + headerReference + separator + truncatedTitle;

            if (string.Equals(this.Model.HeaderData.ProductCode, PRODUCT_GBIPC))
            {
                ProductType = PRODUCT_GBIPC;
            }
            else if (string.Equals(this.Model.HeaderData.ProductCode, PRODUCT_GBIMO))
            {
                ProductType = PRODUCT_GBIMO;
            }

            long headerID = this.Model.HeaderData.HeaderID;            
            this.GetCustomPageURL(headerID, spreadsheetFileName, ProductType, headerReference);
        }

        private void GetCustomPageURL(long headerID, string spreadsheetFileName, string ProductType, string headerReference)
        {
            ConfigurationManager configurationManager = this.Container.Resolve<ConfigurationManager>();
            SettingParameter settingParameter = new SettingParameter();
            settingParameter.QualifierName = POLICY_SUMMARY_URI;
            settingParameter.QualifierValue = "*";
            string urlTemplate = configurationManager.GetValue(AXACUSTOM_WEBPAGES, settingParameter);
            OpenPolicyUrl(urlTemplate.Replace("{HeaderID}", headerID.ToString()).Replace("{FileName}", spreadsheetFileName).Replace("{ProductType}", ProductType).Replace("{HeaderReference}", headerReference));
        }

        public override void Dispose()
        {
            base.Dispose();
            this.riskModel.OnUpdateFromGeniusClick -= new EventHandler<CommandEventArgs<HeaderDto>>(this.RiskModel_OnUpdateFromGeniusClick);
            this.riskModel.OnPolicySummaryClick -= new EventHandler<CommandEventArgs<HeaderDto>>(this.RiskModel_OnPolicySummaryClick);
        }

        /// <summary>
        /// This is only being used by Update Section Details. Previously it was overriding a core method, but that has been
        /// refactored out of version 6.0
        /// </summary>
        /// <param name="section">Section Data Object</param>
        protected void RefreshSectionDetails(SectionDto section)
        {
            GetSortFieldDelegate<SectionDetailDto> getReference = (SectionDetailDto o) => (o.Data as ISectionDetail).SectionDetailTypeCode + (o.Data as ISectionDetail).ExternalReference;
            ArgumentCheck.ArgumentNullCheck(getReference, "Sort Delegate should not be null");
            section.SortedSectionDetails.Clear();
            if (section.SectionDetails != null)
            {
                section.SectionDetails.Sort<SectionDetailDto>((a, b) => string.Compare(getReference(a), getReference(b)));
                foreach (SectionDetailDto sectionDetail in section.SectionDetails)
                {
                    section.SortedSectionDetails.Add(sectionDetail);
                }
            }

            this.riskModel.InvokePropChanged("SortedSectionDetails");
        }

       private void RiskModel_OnUpdateFromGeniusClick(object sender, CommandEventArgs<HeaderDto> e)
       {
           VirtualProcessRequestBuilder.BuildComponentVirtualProcessRequest(this._payloadManager, (e.Dto.Data as HeaderData).DataId, "UpdateFromGenius");

           this.riskModel.IsBusy = true;
           this._payloadManager.SynchroniseData(this.riskModel.HeaderDto,
               this.riskModel.TransactionId,
               true,
               (Response response) =>
               {
                   if (this.Model.HeaderDto.Sections != null)
                   {
                       this.Model.HeaderDto.Sections.ForEach(dt =>
                           {
                               dt.NotifyPropertyChange("CurrentVersionData");
                               this.RefreshSectionDetails(dt);
                           });
                   }

                   try
                   {             
                       this.UnRegisterNIEvents();
                       // Updating the InsuredData & BrokerData of Risk Model from HeaderDto.UwNameInvolvements
                       this.Model.HeaderDto.UwNameInvolvements.ForEach(ni =>
                      {
                          if (ni.UwNameInvolvementData != null)
                          {
                              UwNameInvolvementDto niDto = (ni.CurrentVersionDto as UwNameInvolvementVersionDto).UwNameInvolvement;
                              if (niDto != null)
                              {
                                  if (niDto.UwNameInvolvementData.NameInvolvementType == (short)StaticValues.NameInvolvementType.MajorInsured && this.Model.InsuredData != null)
                                  {
                                      this.Model.InsuredData.NameID = niDto.UwNameInvolvementData.NameID;
                                  }

                                  if (niDto.UwNameInvolvementData.NameInvolvementType == (short)StaticValues.NameInvolvementType.MajorBroker && this.Model.BrokerData != null)
                                  {
                                      this.Model.BrokerData.NameID = niDto.UwNameInvolvementData.NameID;
                                  }
                              }
                          }
                      });                       
                   }
                   finally
                   {
                       this.RegisterNIEvents();
                   };

                   this.riskModel.IsBusy = false;
               });
       }

       protected override void InsuredData_PropertyChanged(object sender, PropertyChangedEventArgs e)
       {
           if (e.PropertyName == "NameID")
           {
               this.InvalidateGenericDataSetData();
               base.InsuredData_PropertyChanged(sender, e);               
           }
       }

       private void InvalidateGenericDataSetData()
       {
           List<BusinessData> genericDataItem = new List<BusinessData>();
           HeaderVersionDto container = this.Model.HeaderDto.HeaderVersions.FirstOrDefault(a => a.HeaderVersionData.IsLatestVersion == true);
           if (container != null && container.GenericDataSet != null && !container.GenericDataSet.GenericDataItemsDto.IsNullOrEmpty())
           {
               foreach (UwGenericDataItemDto genericData in container.GenericDataSet.GenericDataItemsDto)
               {
                   genericDataItem.Add(genericData.Data);
               }
           }

           if (genericDataItem.Count > 0)
           {
               ////remove the GenericData Item field defintion from cache so next time updated once can be retrieved
               this.MetadataManager.ClearCache(genericDataItem);
           }
       }
    }
}
