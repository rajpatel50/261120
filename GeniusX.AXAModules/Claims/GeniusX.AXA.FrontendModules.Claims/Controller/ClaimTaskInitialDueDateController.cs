using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GeniusX.AXA.FrontendModules.Claims.Model;
using Microsoft.Practices.Unity;
using XIAP.Frontend.Infrastructure;
using XIAP.Frontend.Infrastructure.Navigation;
using XIAP.Frontend.Infrastructure.Search;
using XIAP.FrontendModules.Common.Controller;
using XIAP.FrontendModules.Tasks.Service;

namespace GeniusX.AXA.FrontendModules.Claims.Controller
{
    public class ClaimTaskInitialDueDateController : ViewControllerBase<ClaimTaskInitialDueDateModel>
    {
        private TaskInitiatedContollerArgs args;
		private ITaskServiceHelper taskServiceHelper;
		private AppModel appModel;

        public ClaimTaskInitialDueDateController(ClaimTaskInitialDueDateModel model, TaskServiceHelper taskServiceHelper, AppModel appModel,IUnityContainer container)
            : base(container)
        {
            this.Model  = model;
			this.taskServiceHelper = taskServiceHelper;
			this.appModel = appModel;

            this.Model.OnOk += this.OkClicked;
            this.Model.OnCancel += this.CancelClicked; 
        }

        public override void OnStart(TaskArgumentsHolder holder)
        {
            base.OnStart(holder);
            this.args = (TaskInitiatedContollerArgs)holder.ControllerArguments;
			this.ValidateVisibility();
        }

        private void OkClicked(object sender, EventArgs args)
        {
			this.args.CompletionStatus = CompletionStatus.Complete;
			this.args.TaskDetail.Data = new System.Collections.Generic.Dictionary<string, object>();
            this.args.TaskDetail.Data["ReviewTaskDueDate"] = this.Model.TaskDueDate;
			this.Navigator.Finish(this.args);
        }

        private void CancelClicked(object sender, EventArgs args)
        {
			this.args.CompletionStatus = CompletionStatus.Cancelled;
			this.Navigator.Finish(this.args);
        }

		public void ValidateVisibility()
		{
			ObservableCollection<string> processNames = new ObservableCollection<string>();

			var setting = this.appModel.ShellConfiguration.ConfigurationSettings["ClaimReviewGraphs"];
			if (setting != null)
			{
				processNames = (from v in setting.SettingParmeters.Where(c => c.QualifierName == "ReviewGraph")
								select v.QualifierValue).ToObservableCollection<string>();
			}

			if (processNames.Count > 0)
			{
				string folio = string.Empty;

				ITaskRow taskRow = this.args.TaskRow;
				folio = taskRow.Folio;

                this.taskServiceHelper.GetTaskDetailsByProcessName(
					folio,
					processNames,
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

							if (doFinishGraph)
							{
								this.FinishGraph(this.Navigator, args);
							}
							else
							{
                                this.Model.ShowView();
							}
						}
					},
					this.HandleError);
			}
		}

		private void FinishGraph(INavigator navigator, TaskInitiatedContollerArgs args)
		{
			args.CompletionStatus = XIAP.FrontendModules.Common.Controller.CompletionStatus.Complete;
			navigator.Finish(args);
		}
    }
}
