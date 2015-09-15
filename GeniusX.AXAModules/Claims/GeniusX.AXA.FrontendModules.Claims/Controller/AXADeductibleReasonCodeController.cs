using System;
using System.Linq;
using GeniusX.AXA.FrontendModules.Claims.Model;
using Microsoft.Practices.Unity;
using Xiap.Framework.Metadata;
using Xiap.Framework.Validation;
using XIAP.Frontend.Infrastructure;

namespace GeniusX.AXA.FrontendModules.Claims.Controller
{
    public class AXADeductibleReasonCodeController : ViewControllerBase<DeductibleReasonCodeModel>
    {
        private DeductibleReasonCodeArg _args;
        
        public AXADeductibleReasonCodeController(DeductibleReasonCodeModel model, IUnityContainer container)
            :base(container)
        {
            this.Model = model;
            this.Model.OnOk += this.Model_OnOkEvent;
            this.Model.OnCancel += this.Model_OnCancelEvent; 
        }

        public override void OnStart(TaskArgumentsHolder holder)
        {
            ArgumentCheck.ArgumentNullCheck(holder, "holder");
            ArgumentCheck.ArgumentNullCheck(holder.ControllerArguments, "holder.ControllerArguments");
            this._args = holder.ControllerArguments as DeductibleReasonCodeArg;
            this.Model.ReasonCodeList = this._args.DeductibleReasonList;
        }

        private void Model_OnOkEvent(object sender, EventArgs args)
        {
            if (!string.IsNullOrEmpty(this.Model.DeductibleReason))
            {
                this._args.SelectedReasonCode = this.Model.DeductibleReason;
            }
            else
            {
                this._args.SelectedReasonCode = string.Empty;
            }

            this.Navigator.Finish(this._args);
        }

        private void Model_OnCancelEvent(object sender, EventArgs args)
        {
            this.Navigator.Finish();
        }

        public override void Dispose()
        {
            base.Dispose();
            this.Model.OnOk -= this.Model_OnOkEvent;
            this.Model.OnCancel -= this.Model_OnCancelEvent;
            this.Model = null;
        }

        private string GetDesc(Field Field, string Code)
        {
            if (string.IsNullOrEmpty(Code))
            {
                return string.Empty;
            }
            else
            {
                var value = Field.AllowedValues.FirstOrDefault(c => c.Code.Trim() == Code.ToString().Trim());
                if (value != null)
                {
                    return value.Description;
                }
                else
                {
                    return string.Empty;
                }
            }
        }
    }
}
