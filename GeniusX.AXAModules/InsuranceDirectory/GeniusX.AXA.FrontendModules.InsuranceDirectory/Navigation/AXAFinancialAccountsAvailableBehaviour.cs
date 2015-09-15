using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Xiap.Framework.Data;
using XIAP.Frontend.Infrastructure;
using XIAP.Frontend.Infrastructure.Tree;
using XIAP.FrontendModules.Infrastructure.NavTree;

namespace GeniusX.AXA.FrontendModules.InsuranceDirectory.Navigation
{
    public class AXAFinancialAccountsAvailableBehaviour : INodeAvailabilityBehaviour
    {
        private AppModel _appModel;
        public AXAFinancialAccountsAvailableBehaviour(AppModel appModel)
        {
            this._appModel = appModel;
        }

        /// <summary>
        /// Method to check whether the node will be available on screen or not.
        /// </summary>
        /// <param name="transactionController">Object of ITransactionController</param>
        /// <param name="definition">Tree Structure Store</param>
        /// <param name="parentDto">Dto base</param>
        /// <returns>bool value</returns>
        public bool IsAvailable(ITransactionController transactionController, TreeStructureStore definition, DtoBase parentDto)
        {
            bool result = true;
            if (this._appModel.ShellConfiguration.ConfigurationSettings["NameFinancialAccountMaintenancePermissionToken"] != null && !this._appModel.ShellConfiguration.ConfigurationSettings["NameFinancialAccountMaintenancePermissionToken"].SettingParmeters.IsNullOrEmpty())
            {
                string tokenName = this._appModel.ShellConfiguration.ConfigurationSettings["NameFinancialAccountMaintenancePermissionToken"].SettingParmeters[0].SettingValue;

                if (!string.IsNullOrEmpty(tokenName) && !this._appModel.UserProfile.HasPermission(tokenName))
                {
                    result = false;
                }
            }

            return result;
        }
    }
}
