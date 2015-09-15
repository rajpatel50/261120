using System;
using System.Collections.Generic;
using System.Linq;
using Xiap.Claims.BusinessComponent;
using Xiap.Claims.BusinessLogic.CustomDataUpdaters;
using Xiap.Framework;
using Xiap.Framework.Data;
using Xiap.Framework.Metadata;
using Xiap.Framework.Validation;
using Xiap.Metadata.Data.Enums;
using Xiap.Framework.Entity;

namespace GeniusX.AXA.Claims.BusinessLogic.CustomDataUpdaters
{
    public class AXAClaimDetailDataUpdater : ClaimDetailDataUpdater
    {
        public override void UpdateBusinessData(Xiap.Framework.IBusinessComponent component, Xiap.Framework.Data.BusinessData dataClass)
        {
            base.UpdateBusinessData(component, dataClass);
            this.UpdateCustomProperties(component, dataClass);
        }

        private void UpdateCustomProperties(IBusinessComponent component, BusinessData dataClass)
        {
            ArgumentCheck.ArgumentNullCheck(component, "component");
            ArgumentCheck.ArgumentNullCheck(dataClass, "dataClass");
            ClaimDetail claimDetail = component as ClaimDetail;
            ClaimHeader claimHeader = claimDetail.ClaimHeader as ClaimHeader;
            if (claimHeader.GetProduct().Product.Code == ClaimConstants.PRODUCT_MOTORCLAIM)
            {
                //// Registration number is only available in MOTOR  Claim product.
                this.AssignClaimantRegistrationNumber(claimHeader, claimDetail, dataClass);
            }

             this.AssignClaimantDetails(claimHeader, claimDetail, dataClass);
        }

        private void AssignClaimantRegistrationNumber(ClaimHeader claimHeader, ClaimDetail claimDetail, BusinessData dataClass)
        {
            string registrationNumber = string.Empty;
            var insuredObjects = claimDetail.ClaimDetailToClaimInvolvementLinks.Where(lnk => lnk.ClaimInvolvement.ClaimInvolvementType == (short)StaticValues.LinkableComponentType.InsuredObject).SelectMany(link => link.ClaimInvolvement.InternalClaimInsuredObjects);
            int number = insuredObjects.Count(io => io.InternalIOType == (short)StaticValues.InternalIOType.Vehicle);
            if (number == 0)
            {
                var nameInvolvements = claimHeader.ClaimInvolvements.SelectMany<ClaimInvolvement, ClaimNameInvolvement>(ci => ci.ClaimNameInvolvements);

                registrationNumber = this.GetRegistationNumber(claimHeader, this.GetNameInvolmentDataID(claimDetail, (short)StaticValues.NameInvolvementType_ClaimNameInvolvement.AdditionalClaimant));
                if (string.IsNullOrEmpty(registrationNumber))
                {
                    registrationNumber = this.GetRegistationNumber(claimHeader, this.GetNameInvolmentDataID(claimDetail, (short)StaticValues.NameInvolvementType_ClaimNameInvolvement.Driver));
                }
            }
            else if (number > 1)
            {
                registrationNumber = ClaimConstants.CONST_VARIOUS;
            }
            else
            {
                var claimIOVehicle = insuredObjects.First(io => io.InternalIOType == (short)StaticValues.InternalIOType.Vehicle).ClaimIOVehicles.FirstOrDefault();
                registrationNumber = claimIOVehicle.RegistrationNumber;
            }

            dataClass.CustomProperties["RegistrationNumber"] = registrationNumber;
        }

        private void AssignClaimantDetails(ClaimHeader claimHeader, ClaimDetail claimDetail, BusinessData dataClass)
        {
            var claimants = claimDetail.ClaimDetailToClaimInvolvementLinks.Where(lnk => lnk.ClaimInvolvement.ClaimInvolvementType == (short)StaticValues.LinkableComponentType.NameInvolvement).SelectMany(link => link.ClaimInvolvement.ClaimNameInvolvements.Where(a=> a.NameUsageTypeCode == "UCL" || a.NameUsageTypeCode == "DRV"));

            if (claimants.Count() > 0)
            {
                ClaimNameInvolvement claimant = claimants.FirstOrDefault();
                if (claimant.NameID.HasValue)
                {
                    dataClass.CustomProperties["ClaimantNameID"] = claimant.NameID.GetValueOrDefault();
                }


                CodeRow code = claimant.CustomCode01Field.AllowedValues().Where(c => c.Code == claimant.CustomCode01).FirstOrDefault();
                if (code != null)
                {
                    dataClass.CustomProperties["ClaimantCustomCode01Desc"] = code.Description;
                }
                else
                {
                    dataClass.CustomProperties["ClaimantCustomCode01Desc"] = string.Empty;
                }
            }
            else
            {
                dataClass.CustomProperties["ClaimantCustomCode01Desc"] = string.Empty;
            }
        }

        private List<Guid> GetNameInvolmentDataID(ClaimDetail claimDetail, short claimNameInvolvement)
        {
            var claimInvolvementLinks = claimDetail.ClaimDetailToClaimInvolvementLinks.Where(x => x.ClaimInvolvement.ClaimInvolvementType == (short)StaticValues.LinkableComponentType.NameInvolvement
                          && x.ClaimNameInvolvement != null && x.ClaimNameInvolvement.NameInvolvementMaintenanceStatus == (short)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest
                          && x.ClaimNameInvolvement.NameInvolvementType == claimNameInvolvement).FirstOrDefault();
            if (claimInvolvementLinks != null)
            {
                return claimInvolvementLinks.ClaimInvolvement.ClaimNameInvolvements.ToList().Select(item => item.ClaimInvolvement.DataId).ToList<Guid>();
            }

            return new List<Guid> { };
        }

        private string GetRegistationNumber(ClaimHeader claimHeader, List<Guid> claimInvolvementDataID)
        {
            string registrationNumber = string.Empty;
            foreach (Guid guidid in claimInvolvementDataID)
            {
                var claimantLinks = claimHeader.ClaimInvolvementLinks.Where(cil => cil.ClaimInvolvementFrom.DataId == guidid);
                var vehicles = claimantLinks.Where(cl => cl.ClaimInvolvementTo != null && cl.ClaimInvolvementTo.ClaimInsuredObjects != null && cl.ClaimInvolvementTo.ClaimInsuredObjects.Count() > 0).SelectMany(cl => cl.ClaimInvolvementTo.ClaimInsuredObjects.SelectMany(cio => cio.ClaimIOVehicles));

                if (vehicles != null && vehicles.Count() > 0)
                {
                    if (vehicles.Count() > 1)
                    {
                        registrationNumber = ClaimConstants.CONST_VARIOUS;
                    }
                    else
                    {
                        registrationNumber = vehicles.First().RegistrationNumber;
                    }
                }
            }

            return registrationNumber;
        }
    }
}
