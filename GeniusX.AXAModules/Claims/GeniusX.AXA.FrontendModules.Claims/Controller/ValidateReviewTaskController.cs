using GeniusX.AXA.FrontendModules.Claims.Model;
using GeniusX.AXA.FrontendModules.Claims.Resources;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Xiap.Metadata.Data.Enums;
using XIAP.Frontend.CoreControls;
using XIAP.Frontend.Infrastructure;
using XIAP.Frontend.Infrastructure.Search;
using XIAP.FrontendModules.Claims;
using XIAP.FrontendModules.Claims.Service;
using XIAP.FrontendModules.Common.ClaimService;
using XIAP.FrontendModules.Search;
using XIAP.FrontendModules.Tasks.Service;
using Microsoft.Practices.Unity;

namespace GeniusX.AXA.FrontendModules.Claims.Controller
{
    public class ValidateReviewTaskController : ViewControllerBase<ValidateReviewTaskModel>
    {
        private TaskInitiatedContollerArgs args;
        private ITaskServiceHelper taskServiceHelper;
		private IClaimClientService claimService;
		private AppModel appModel;
        private TaskPreviewMappingLookup taskPreviewMappingLookup;

        public ValidateReviewTaskController(ValidateReviewTaskModel model, ITaskServiceHelper taskServiceHelper, IClaimClientService claimService, TaskPreviewMappingLookup taskPreviewMappingLookup, AppModel appModel,IUnityContainer container)
            :base(container)
        {
            this.Model = model;
			this.taskServiceHelper = taskServiceHelper;
            this.claimService = claimService;
            this.taskPreviewMappingLookup = taskPreviewMappingLookup;
			this.appModel = appModel;
        }

        public override void OnStart(TaskArgumentsHolder holder)
        {
            base.OnStart(holder);
            this.args = (TaskInitiatedContollerArgs)holder.ControllerArguments;
            this.ValidateComplete();
        }


        public void ValidateComplete()
        {
            ObservableCollection<string> processNames = new ObservableCollection<string>();
            string claimReference = string.Empty;
            object identifier = this.taskPreviewMappingLookup.LoadIdentifier(this.args.TaskRow, new TaskPreviewData() { TaskDetail = this.args.TaskDetail });

            if (identifier.GetType() == typeof(Dictionary<string, object>))
            {
                Dictionary<string, object> identifiers = (Dictionary<string, object>)identifier;
                claimReference = claimReference = identifiers.ContainsKey(ClaimsConstants.ClaimHeaderReference) ? identifiers[ClaimsConstants.ClaimHeaderReference].ToString() : null;
            }
            else
            {
                claimReference = (string)identifier;
            }

            this.claimService.LoadClaimPreviewData(claimReference,
                null,
                null,
                null,
                response =>
                {
                    ClaimHeaderData headerData = (ClaimHeaderData)response.ClaimHeaderDto.Data;
                    bool claimClosed = false;
                    if (headerData.ClaimHeaderInternalStatus.HasValue && headerData.ClaimHeaderInternalStatus.Value == (short)StaticValues.ClaimHeaderInternalStatus.Finalized || headerData.ClaimHeaderInternalStatus.Value == (short)StaticValues.ClaimHeaderInternalStatus.ClosedOpenedInError)
                    {
                        claimClosed = true;
                    }

                    var setting = this.appModel.ShellConfiguration.ConfigurationSettings["ClaimReviewGraphs"];
                    if (setting != null)
                    {
                        processNames = (from v in setting.SettingParmeters.Where(c => c.QualifierName == "ReviewGraph")
                                        select v.QualifierValue).ToObservableCollection<string>();
                    }

                    if (processNames.Count > 0)
                    {
                        ITaskRow taskRow = this.args.TaskRow;
                        taskServiceHelper.GetTaskCountForProcessesByComponentID(processNames,
                            headerData.ClaimHeaderID,
                            results =>
                            {
                                if (results != null && results.Count > 0)
                                {
                                    bool doFinishGraph = false;
                                    foreach (KeyValuePair<string, int> keyVal in results.ToObservableCollection())
                                    {
                                        if (keyVal.Key == taskRow.ProcessName)
                                        {
                                            if (keyVal.Value > 1)
                                            {
                                                doFinishGraph = true;
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            if (keyVal.Value > 0)
                                            {
                                                doFinishGraph = true;
                                                break;
                                            }
                                        }
                                    }

                                    if (!doFinishGraph && !claimClosed)
                                    {
                                        this.ShowMessage(StringResources.NewClaimTaskCompletionValidationTitle, StringResources.NewClaimTaskCompletionValidationMessage);
                                    }
                                    else
                                    {
                                        this.args.CompletionStatus = XIAP.FrontendModules.Common.Controller.CompletionStatus.Complete;
                                        this.Navigator.Finish(args);
                                    }
                                }
                            },
                            this.HandleError);
                    }
                },
                this.HandleError);
        }

        private bool OKClick(object sender, ExitCode e)
        {
            this.args.CompletionStatus = XIAP.FrontendModules.Common.Controller.CompletionStatus.Cancelled;
            this.Navigator.Finish(this.args);
            return true;
        }

        private void ShowMessage(string messageTitle, string messageText)
        {
            XIAPMessageBox.Show(messageTitle,
                messageText,
                XIAPMessageBox.Buttons.OK,
                XIAPMessageBox.Icons.Error,
                this.OKClick);
        }
               
        private string GetInsuredNameFromHeader(ClaimHeaderDto claimHeader)
        {
            string listName = string.Empty;
            if (claimHeader.ClaimInvolvements != null)
            {
                var claimNameInvolvements = claimHeader.ClaimInvolvements.Where(c => (c.Data as IClaimInvolvementData).ClaimInvolvementType == (short)Xiap.Metadata.Data.Enums.StaticValues.LinkableComponentType.NameInvolvement).SelectMany(b => b.ClaimNameInvolvements).ToList();

                if (claimNameInvolvements != null)
                {
                    foreach (ClaimNameInvolvementDto claimnameinvolvementdto in claimNameInvolvements)
                    {
                        if (claimnameinvolvementdto != null)
                        {
                            if (claimnameinvolvementdto.ClaimNameInvolvementData.NameInvolvementType == (short)StaticValues.NameInvolvementType.MajorInsured &&
                            claimnameinvolvementdto.ClaimNameInvolvementData.NameInvolvementMaintenanceStatus == (short)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest)
                            {
                                ClaimNameInvolvementData claimNameInvolvementData = claimnameinvolvementdto.ClaimNameInvolvementData;
                                if (claimNameInvolvementData != null && claimNameInvolvementData.NameID.HasValue != null)
                                {
                                    listName = claimNameInvolvementData.ListName;
                                }
                            }
                        }
                    }
                }
            }

            return listName;
        }


        private string GenerateFolio(string claimReference, string insuredListName)
        {
            if (!string.IsNullOrEmpty(insuredListName))
            {
                return claimReference + "/" + insuredListName;
            }
            else
            {
                return claimReference;
            }
        }
    }
}
