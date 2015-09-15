using System;
using System.Linq;
using Microsoft.Practices.Unity;
using XIAP.Frontend.CoreControls;
using XIAP.Frontend.Infrastructure;
using XIAP.Frontend.Infrastructure.Rules;
using XIAP.FrontendModules.Application;
using XIAP.FrontendModules.Common;
using XIAP.FrontendModules.Common.InsuranceDirectoryService;
using XIAP.FrontendModules.Infrastructure.NavTree;
using XIAP.FrontendModules.InsuranceDirectory.Controller;
using XIAP.FrontendModules.InsuranceDirectory.Data;
using XIAP.FrontendModules.InsuranceDirectory.Model;
using XIAP.FrontendModules.InsuranceDirectory.Resources;
using XIAP.FrontendModules.InsuranceDirectory.Service;
using XIAP.FrontendModules.InsuranceDirectory.ViewResolver;

namespace GeniusX.AXA.FrontendModules.InsuranceDirectory.Controller
{
    public class AXAPersonController : PersonController
    {
        public AXAPersonController(IUnityContainer container, IdPayloadManager idManager, ApplicationModel applicationModel, IInsuranceDirectoryServiceHandler insuranceDirectoryService, PersonModel personModel, IdRetrievalManager retrievalManager, AppModel appModel, IInsuranceDirectoryViewResolver viewresolver, IShellRulesHelper rulesHelper, IMetadataClientService metadataService)
            : base(idManager, applicationModel, insuranceDirectoryService, personModel, retrievalManager, appModel, viewresolver, rulesHelper, container, metadataService, null)
        {
        }

        protected override bool AddUsageVerifyResponse(Xiap.ClientServices.Facade.Common.Response response)
        {
            string messages = string.Empty;
            if (response.Messages != null && response.Messages.Count > 0)
            {
                messages = string.Join("\n", response.Messages.Select(m => m.MessageTitle).ToArray());
                XIAPMessageBox.Show("Error", messages, XIAPMessageBox.Buttons.OK, null);
                return false;
            }

            return true;
        }

        public override void SetNameData(IDResponse response)
        {
            base.SetNameData(response);
            
            this.Model.NameData.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(this.NameData_PropertyChanged);
            this.Model.PersonDetailVersionData.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(this.PersonDetailVersionData_PropertyChanged);
        }


        private void NameData_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "NameReference")
            {
                if (!this.Model.UsageDataRows.IsNullOrEmpty())
                {
                    this.Model.PersonDetailVersionData.CustomReference04 = this.Model.NameData.NameReference;
                }
            }
        }

       
        private void PersonDetailVersionData_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Surname" || e.PropertyName == "Forename" || e.PropertyName == "TitleCode")
            {
                PersonDetailVersionData personDetailVersion = this.Model.PersonDetailVersionData;
                string title = string.Empty;
                if (!string.IsNullOrEmpty(personDetailVersion.TitleCode))
                {
                    CodeLookupService.GetCodeList(this.Model.PersonFields.Title.LookupDefinitionKey,
                        this.Model.PersonFields.Title.LookupParameters,
                        (allowedvalues) =>
                        {
                            title = allowedvalues.Where(item => string.IsNullOrEmpty(personDetailVersion.TitleCode) == false && item.Code == personDetailVersion.TitleCode).FirstOrDefault().Description;
                            if (!string.IsNullOrWhiteSpace(title))
                            {
                                string listName = String.Format("{0} {1}", title, personDetailVersion.Forename).Trim();
                                personDetailVersion.ListName = String.Format("{0} {1}", listName, personDetailVersion.Surname).Trim();
                            }
                            else
                            {
                                personDetailVersion.ListName = String.Format("{0} {1}", personDetailVersion.Forename, personDetailVersion.Surname).Trim();
                            }
                        });
                }
                else
                {
                    personDetailVersion.ListName = String.Format("{0} {1}", personDetailVersion.Forename, personDetailVersion.Surname).Trim();
                }
            }
        }

        public override void Dispose()
        {
            this.Model.NameData.PropertyChanged -= new System.ComponentModel.PropertyChangedEventHandler(this.NameData_PropertyChanged);
            this.Model.PersonDetailVersionData.PropertyChanged -= new System.ComponentModel.PropertyChangedEventHandler(this.PersonDetailVersionData_PropertyChanged);
            base.Dispose();
        }
    }
}
