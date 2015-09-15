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
using XIAP.FrontendModules.Claims.Model;
using System.ComponentModel;
using Xiap.Framework.Data;
using System.Collections.ObjectModel;
using System.Linq;
using XIAP.Frontend.Infrastructure;
using XIAP.Frontend.Infrastructure.Users;
using XIAP.FrontendModules.Common;
using XIAP.FrontendModules.Common.ClaimService;
using Xiap.Framework.Metadata;
using XIAP.Frontend.Infrastructure.Search;
using XIAP.FrontendModules.Common.Events;
namespace GeniusX.AXA.FrontendModules.Claims.Model
{
    public class AXABulkEventEntryRow : BulkEventEntryRow
    {
        public AXABulkEventEntryRow()
        {
        }

       public override void BulkEventEntryRow_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "TaskInitialPriority":
                case "EventType":
                    if (((sender as AXABulkEventEntryRow).EventType == AXAClaimConstants.EVENT_POST_TYPECODE) && ((sender as AXABulkEventEntryRow).TaskInitialPriority == AXAClaimConstants.EVENT_PRIORITY_REC))
                    {
                        this.TaskInitialUserIdentity = string.Empty;
                        (sender as AXABulkEventEntryRow).TaskInitialUserID = null;
                        this.TaskInitialUserDisplayValue = string.Empty;
                        this.IsEnabled = false;
                        this.OnPropertyChanged("TaskInitialUserID");
                        this.OnPropertyChanged("TaskInitialUserIdentity");
                        this.OnPropertyChanged("TaskInitialUserDisplayValue");
                    }
                    else
                    {
                        this.IsEnabled = true;
                        if (!string.IsNullOrWhiteSpace(this.ClaimHeaderReference) && this.SearchPopupAction != null && string.IsNullOrWhiteSpace(this.TaskInitialUserIdentity))
                        {
                            this.SearchPopupAction.PerformCustomAction(this);
                            if (this.BusinessDataState != BusinessDataState.Added)
                            {
                                this.BusinessDataState = BusinessDataState.Modified;
                            }
                        }                       
                    }

                    break ;
                default:

                    base.BulkEventEntryRow_PropertyChanged(sender, e);
                    break;
            }
        }
    }
}
