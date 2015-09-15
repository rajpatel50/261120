using System.Collections.ObjectModel;
using System.Linq;
using GeniusX.AXA.FrontendModules.Claims.Resources;
using Microsoft.Practices.Unity;
using Xiap.ClientServices.Facade.Common;
using Xiap.Framework.Data;
using Xiap.Framework.Metadata;
using Xiap.Metadata.Data.Enums;
using XIAP.Frontend.CommonInfrastructure.Controller.Documents;
using XIAP.Frontend.CommonInfrastructure.Model.Documents;
using XIAP.Frontend.CoreControls;
using XIAP.Frontend.Infrastructure;
using XIAP.Frontend.Infrastructure.Names;
using XIAP.Frontend.Infrastructure.Rules;
using XIAP.FrontendModules.Common.ClaimService;
using XIAP.FrontendModules.Common.Controller;
using XIAP.FrontendModules.Common.Documents;
using XIAP.FrontendModules.Common.NameInvolvements;


namespace GeniusX.AXA.FrontendModules.Claims.Controller
{
    public class AXADocumentDataController : DocumentDataController
    {
        public AXADocumentDataController(IUnityContainer container, DocumentDataModel model, INameService nameService, AppModel appModel, IShellRulesHelper rulesHelper)
            : base(model, nameService, appModel, rulesHelper, container)
        { 
        }

        protected override void SetNameDetail(NameItem nameItem, XIAP.FrontendModules.Common.InsuranceDirectoryService.NameData nameData)
        {
            base.SetNameDetail(nameItem, nameData);
            if (nameData.NameType == (short)StaticValues.NameType.Company)
            {
                ReadOnlyCollection<INameInvolvementData> nameInvolvementDataCollection = this.GetNameInvolvements();
                nameInvolvementDataCollection.ForEach(nameInvolvementData =>
                {
                    ClaimNameInvolvementData claimNameInvolvementData = nameInvolvementData as ClaimNameInvolvementData;
                    if (claimNameInvolvementData != null && !string.IsNullOrWhiteSpace(claimNameInvolvementData.CustomReference04) && this.nameAndEmails[nameItem] != null && nameItem.NameID == claimNameInvolvementData.NameID)
                    {
                        this.nameAndEmails[nameItem].Add(new CodeRow { Code = claimNameInvolvementData.CustomReference04, Description = claimNameInvolvementData.CustomReference04 });
                    }
                });
            }
        }

        protected override void ShowValidationMessageAndNavigate(Response response)
        {
            this.Model.UIMessages.Clear();
            // put a null check for collection to casting issue
            var segmentValidationMessages = ExtractTextSegmentValidationMessages(response.Messages.IsNullOrEmpty() ? null : response.Messages.ToObservableCollection<MessageData>(), this.Model.DocumentDetails);
            ControllerArgs args = new ControllerArgs();
            // skip the message if only description editing is allowed
            if (segmentValidationMessages.Any() && this.Model.IsDataEntryAllowed)
            {
                args.SetProperty("WarningsExist", true);
                this.Model.UIMessages.AddRange(segmentValidationMessages);
                XIAPMessageBox.Show(StringResources.DocumentData_Validation,
                    StringResources.DocumentData_ValidationWarning,
                     XIAPMessageBox.Buttons.OkCancel,
                     (a, e) =>
                     {
                         if (e == ExitCode.OK)
                         {
                             Finish(args, this.Model.DocumentDetails);
                         }

                         return true;
                     });
            }
            else
            {
                args.SetProperty("WarningsExist", false);
                this.Finish(args, this.Model.DocumentDetails);
            }
        }

        private void Finish(ControllerArgs args, IDocumentDetails docData)
        {
            if (docData.CurrentStatusDescription != StaticValues.DocumentStatus.Generated.ToString())
            {
                this.Navigator.Finish(args);
            }
            else
            {
                this.Navigator.Finish(null);
            }
        }
    }
}