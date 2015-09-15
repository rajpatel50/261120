using System.Collections.ObjectModel;
using Xiap.Framework.Metadata;
using XIAP.FrontendModules.Common.Controller;

namespace GeniusX.AXA.FrontendModules.Claims.Controller
{
    public class DeductibleReasonCodeArg : ControllerArgs
    {
        public ObservableCollection<CodeRow> DeductibleReasonList
        {
            get
            {
                return this.GetProperty<ObservableCollection<CodeRow>>("DeductibleReasonList");
            }

            set
            {
                this.SetProperty("DeductibleReasonList", value);
            }
        }

        public string SelectedReasonCode
        {
            get
            {
                return this.GetProperty<string>("SelectedReasonCode");
            }

            set
            {
                this.SetProperty("SelectedReasonCode", value);
            }
        }
        
        public Field ReasonCodeField
        {
            get
            {
                return this.GetProperty<Field>("ReasonCodeField");
            }

            set
            {
                this.SetProperty("ReasonCodeField", value);
            }
        }
    }
}
