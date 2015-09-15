using System;
using Microsoft.Practices.Prism.Commands;
using XIAP.Frontend.Infrastructure;
using XIAP.Frontend.Infrastructure.Events;
using XIAP.FrontendModules.Common.UWService;
using XIAP.FrontendModules.Underwriting.Model;

namespace GeniusX.AXA.FrontendModules.Underwriting.Model
{
    public class AXARiskModel : RiskModel
    {
        public AXARiskModel()
            : base()
        {
            this.UpdateFromGeniusCommand = new NamedDelegateCommand<HeaderDto>("UpdateFromGeniusCommand", this.OnUpdateFromGenius, this.CanUpdateFromGenius);
            this.PolicySummaryCommand = new NamedDelegateCommand<HeaderDto>("PolicySummaryCommand", this.OnPolicySummary, this.CanPolicySummary);
        }

        public event EventHandler<CommandEventArgs<HeaderDto>> OnUpdateFromGeniusClick;
        public event EventHandler<CommandEventArgs<HeaderDto>> OnPolicySummaryClick;
        public DelegateCommand<HeaderDto> UpdateFromGeniusCommand { get; private set; }
        public DelegateCommand<HeaderDto> PolicySummaryCommand { get; private set; }

        protected override void QuoteWizardModel_ExecuteChangedForCommands()
        {
            base.QuoteWizardModel_ExecuteChangedForCommands();
            this.UpdateFromGeniusCommand.RaiseCanExecuteChanged();
            this.PolicySummaryCommand.RaiseCanExecuteChanged();
        }

        public void OnUpdateFromGenius(HeaderDto header)
        {
            InvokeEvent(this.OnUpdateFromGeniusClick, new CommandEventArgs<HeaderDto>(header));
        }

        public bool CanUpdateFromGenius(HeaderDto headerDto)
        {
            ////GetLatestVersion rename GetContextVersion
            if (headerDto == null || this.TryGetFields == null || headerDto.GetContextVersion() == null) 
            {
                return false;
            }

            ////if (!this.HeaderData.ExternalDataSource.Equals("GENIUS"))
            ////{
            ////    return false;
            ////}

            if (this.HeaderData.HeaderStatusThreshold >= 30)
            {
                return false;
            }

            return !this.IsBusy && headerDto.Data.CheckIsEditable();
        }

        public void OnPolicySummary(HeaderDto header)
        {
            InvokeEvent(this.OnPolicySummaryClick, new CommandEventArgs<HeaderDto>(header));
        }

        public bool CanPolicySummary(HeaderDto headerDto)
        {
            if (this.HeaderData.HeaderID == 0)
            {
                return false;
            }

            return !this.IsBusy && this.HeaderData.HeaderID!=0;
        }

        public override void Dispose()
        {
            this.OnUpdateFromGeniusClick = null;
            this.OnPolicySummaryClick = null;
            base.Dispose();
        }
    }
}
