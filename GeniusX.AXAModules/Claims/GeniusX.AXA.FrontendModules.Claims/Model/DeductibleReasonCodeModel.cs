using System;
using System.Collections.ObjectModel;
using Microsoft.Practices.Prism.Commands;
using Xiap.Framework.Metadata;
using XIAP.FrontendModules.Common;

namespace GeniusX.AXA.FrontendModules.Claims.Model
{
    public class DeductibleReasonCodeModel : PresentationModel
    {
		private string _deductibleReason;
        private ObservableCollection<CodeRow> _reasonCodeList;

        public DeductibleReasonCodeModel()
        {
            this.OkCommand = new DelegateCommand<object>(_=>this.InvokeEvent(this.OnOk), this.CanOkClick);
            this.CancelCommand = new DelegateCommand<object>(_ => this.InvokeEvent(this.OnCancel), _ => !this.IsBusy);
        }

        public event EventHandler OnOk;
        public event EventHandler OnCancel;
		
        public DelegateCommand<object> OkCommand { get; private set; }
        public DelegateCommand<object> CancelCommand { get; private set; }

        public string DeductibleReason
		{
			get
			{
                return this._deductibleReason;
			}

			set
			{
                this._deductibleReason = value;
                this.OnPropertyChanged("DeductibleReason");
				this.OkCommand.RaiseCanExecuteChanged();
			}
		}

        public Field DeductibleReasonField
		{
			get
			{
				Field field = new Field();
				field.Visible = true;
				field.Title = "Deductible Reason Code";
                return field;
			}
		}

        public ObservableCollection<CodeRow> ReasonCodeList
        {
            get
            {
                return this._reasonCodeList;
            }

            set
            {
                this._reasonCodeList = value;
                this.OnPropertyChanged("ReasonCodeList");
                this.OkCommand.RaiseCanExecuteChanged();
            }
        }

       private void OkClick(object param)
		{
			this.InvokeEvent(this.OnOk);
		}

		private bool CanOkClick(object param)
		{
            return !this.IsBusy && !string.IsNullOrEmpty(this.DeductibleReason);
		}

		public bool CanCancel(object param)
		{
			return !this.IsBusy;
		}

		public void Cancel(object param)
		{
			this.InvokeEvent(this.OnCancel);
		}
    }
}
