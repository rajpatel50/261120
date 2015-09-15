using System;
using Microsoft.Practices.Prism.Commands;
using Xiap.Framework.Metadata;
using XIAP.FrontendModules.Common;

namespace GeniusX.AXA.FrontendModules.Claims.Model
{
    public class ClaimPaymentRejectReasonModel : PresentationModel
    {
		private string rejectionReason;

        public ClaimPaymentRejectReasonModel()
        {
            this.OkCommand = new DelegateCommand<object>(_ => this.InvokeEvent(this.OnOk), _ => !this.IsBusy);
            this.CancelCommand = new DelegateCommand<object>(_ => this.InvokeEvent(this.OnCancel), _ => !this.IsBusy);
        }

        public event EventHandler OnOk;
        public event EventHandler OnCancel;
		public event EventHandler<VisibilityEventArgs> OnVisibilityChanged;
        
        public DelegateCommand<object> OkCommand { get; private set; }
        public DelegateCommand<object> CancelCommand { get; private set; }

        public string RejectionReason
		{
			get
			{
                return this.rejectionReason;
			}

			set
			{
                this.rejectionReason = value;
                this.OnPropertyChanged("RejectionReason");
				this.OkCommand.RaiseCanExecuteChanged();
			}
		}

        public Field RejectionReasonField
		{
			get
			{
				Field field = new Field();
				field.Visible = true;
				field.Title = "Rejection Reason";
				return field;
			}
		}

		public void ShowView()
		{
			this.InvokeEvent(this.OnVisibilityChanged, new VisibilityEventArgs { Visibility = System.Windows.Visibility.Visible });
		}

		private void OkClick(object param)
		{
			this.InvokeEvent(this.OnOk);
		}

		private bool CanOkClick(object param)
		{
			return !this.IsBusy;
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
