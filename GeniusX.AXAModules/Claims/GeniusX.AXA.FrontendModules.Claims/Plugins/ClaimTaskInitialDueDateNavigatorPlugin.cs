using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GeniusX.AXA.FrontendModules.Claims.Controller;
using Microsoft.Practices.Unity;
using XIAP.Frontend.Infrastructure;
using XIAP.Frontend.Infrastructure.Navigation;
using XIAP.Frontend.Infrastructure.Search;
using XIAP.FrontendModules.Tasks.Service;

namespace GeniusX.AXA.FrontendModules.Claims.Plugins
{
	public class ClaimTaskInitialDueDateNavigatorPlugin : AbstractNavigatorPlugin
	{
		private IUnityContainer unityContainer;
		private ITaskServiceHelper taskServiceHelper;
		private AppModel appModel;
		private TaskInitiatedContollerArgs args;
		private ClaimTaskInitialDueDateController claimTaskInitialDueDateController;

		public ClaimTaskInitialDueDateNavigatorPlugin(IUnityContainer unityContainer, TaskServiceHelper taskServiceHelper, AppModel appModel, ClaimTaskInitialDueDateController claimTaskInitialDueDateController)
		{
			this.unityContainer = unityContainer;
			this.taskServiceHelper = taskServiceHelper;
			this.appModel = appModel;
			this.claimTaskInitialDueDateController = claimTaskInitialDueDateController;
		}

		public override bool PreStartTask(INavigator navigator, string graphName, TaskArgumentsHolder holder)
		{
			ObservableCollection<string> processNames = new ObservableCollection<string>();

			////TODO: Need to come from config

			var setting = this.appModel.ShellConfiguration.ConfigurationSettings["ClaimReviewGraphs"];
			if (setting != null)
			{
				processNames = (from v in setting.SettingParmeters.Where(c => c.QualifierName == "ReviewGraph")
								select v.QualifierValue).ToObservableCollection<string>();
			}

			if (processNames.Count > 0)
			{
				string folio = string.Empty;
				this.args = (TaskInitiatedContollerArgs)holder.ControllerArguments;

				ITaskRow taskRow = this.args.TaskRow;
				folio = taskRow.Folio;

				this.taskServiceHelper.GetTaskDetailsByProcessName(
					folio,
					processNames,
					results =>
					{
						if (results != null && results.Count > 0)
						{
							foreach (KeyValuePair<string, int> keyVal in results.ToObservableCollection())
							{
								if (keyVal.Key == taskRow.ProcessName)
								{
									if (keyVal.Value > 1)
									{
										this.FinishGraph(navigator, args);
										break;
									}
								}
								else
								{
									if (keyVal.Value > 0)
									{
										this.FinishGraph(navigator, args);
										break;
									}
								}
							}
						}
					},
					this.claimTaskInitialDueDateController.HandleError);
			}

			return true;
		}

		private void FinishGraph(INavigator navigator, TaskInitiatedContollerArgs args)
		{
			args.CompletionStatus = XIAP.FrontendModules.Common.Controller.CompletionStatus.Complete;
			navigator.Finish(args);
		}
	}
}
