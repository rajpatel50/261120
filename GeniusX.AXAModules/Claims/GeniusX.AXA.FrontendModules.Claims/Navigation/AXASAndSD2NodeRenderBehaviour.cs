using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using GeniusX.AXA.FrontendModules.Claims.Controller;
using GeniusX.AXA.FrontendModules.Claims.Model;
using Microsoft.Practices.Prism.Commands;
using Xiap.Framework.Data;
using Xiap.Metadata.Data.Enums;
using XIAP.Frontend.Infrastructure;
using XIAP.Frontend.Infrastructure.Navigation;
using XIAP.FrontendModules.Claims.Controller;
using XIAP.FrontendModules.Claims.Enumerations;
using XIAP.FrontendModules.Claims.Model;
using XIAP.FrontendModules.Common.ClaimService;
using XIAP.FrontendModules.Common.ClaimsService;
using XIAP.Frontend.Infrastructure.DataMapping;

namespace GeniusX.AXA.FrontendModules.Claims.Navigation
{
    // For Summary and Summary detail2
    public class AXASAndSD2NodeRenderBehaviour : INodeRenderBehaviour
    {
       private AXAClaimModel claimModel;
       private AXAClaimController claimController;
        public void SetRenderContext(XIAP.Frontend.Infrastructure.IViewController controller, object context)
        {
            this.claimController = (AXAClaimController)controller;
            this.claimModel = (AXAClaimModel)controller.Model;
            this.claimModel.SelectedDto = (ClaimHeaderDto)context;
            this.claimModel.ReservePaymentData.UpdateReservePaymentButtonVisibility(ReservePaymentModesEnum.Hidden);
            this.ValidateDuplicateCommand();
            this.AssignClaimantRegistrationNumber();
            this.AssignClaimantDetails();
        }

        private void ValidateDuplicateCommand(bool disableDuplicateCommand = false)
        {
            ////Do not display the duplicate check button if DateofLoss and Client and if current claim header status is 
            ////'Claim Opened – Unconfirmed' CON.

            if (this.claimModel.HeaderData == null)
            {
                return;
            }

            if (this.claimModel.TransactionType != ClaimTransactionType.CreateClaim
                && (string.IsNullOrWhiteSpace(this.claimModel.CurrentStatusCode) || this.claimModel.CurrentStatusCode != AXAClaimConstants.CLAIM_STATUS_CLAIM_OPENED_UNCONFIRMED))
            {
                this.DisableDuplicateCheckCommand();
                return;
            }

            if (this.claimModel.HeaderData.DateOfLossFrom.HasValue == false)
            {
                this.DisableDuplicateCheckCommand();
                return;
            }

            if (this.claimModel.HeaderDto.ClaimInvolvements == null)
            {
                this.DisableDuplicateCheckCommand();
                return;
            }

            if (this.claimModel.HeaderDto.ClaimInvolvements.Where(cni => this.IsMajorInsuredFound(cni.ClaimNameInvolvements) == true).Count() == 0)
            {
                this.DisableDuplicateCheckCommand();
                return;
            }

            if (disableDuplicateCommand)
            {
                this.DisableDuplicateCheckCommand();
                return;
            }

            // Enable the duplicate check command.
            this.EnableDuplicateCheckCommand();
        }

        private bool IsMajorInsuredFound(ObservableCollection<ClaimNameInvolvementDto> list)
        {
            if (list != null && list.Where(cniInd => cniInd.ClaimNameInvolvementData.NameInvolvementMaintenanceStatus == (short)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest &&
                            cniInd.ClaimNameInvolvementData.NameInvolvementType == (short)StaticValues.NameInvolvementType.MajorInsured).Count() > 0)
            {
                return true;
            }

            return false;
        }

        private void DisableDuplicateCheckCommand()
        {
            this.claimModel.CanExecute = false;
            this.claimModel.RaiseExecuteChange();
        }

        private void EnableDuplicateCheckCommand()
        {
            this.claimModel.CanExecute = true;
            this.claimModel.RaiseExecuteChange();
        }


        private void AssignClaimantRegistrationNumber()
        {
            if (this.claimModel == null)
            {
                return;
            }

            ClaimHeaderDto claimHeaderDto = this.claimModel.HeaderDto;
            claimHeaderDto.ClaimDetails.ForEach(claimDetailDto =>
            {
                string registrationNumber = string.Empty;
                int number = 0;
                if (claimDetailDto.ClaimDetailToClaimInvolvementLinks != null && claimDetailDto.ClaimDetailToClaimInvolvementLinks.Count() > 0)
                {
                    var insuredObjects = claimDetailDto.ClaimDetailToClaimInvolvementLinks.Where(lnk => lnk.ClaimInvolvement != null && lnk.ClaimInvolvement.ClaimInsuredObjects != null && lnk.ClaimInvolvement.ClaimInsuredObjects.Count() > 0).Select(link => link.ClaimInvolvement).SelectMany(ci => ci.ClaimInsuredObjects);
                    if (insuredObjects != null)
                    {
                        number = insuredObjects.Count(io => (io.Data as IClaimInsuredObjectData).InternalIOType == (short)StaticValues.InternalIOType.Vehicle);
                    }

                    if (number == 0)
                    {
                        Dictionary<Guid, DtoBase> dtos = DtoGraphHelper.GetDtoObjectsList(claimHeaderDto);
                        registrationNumber = this.GetRegistationNumber(this.claimModel.HeaderDto, this.GetNameInvolmentDataID(claimDetailDto, (short)StaticValues.NameInvolvementType_ClaimNameInvolvement.AdditionalClaimant), dtos);

                        if (string.IsNullOrEmpty(registrationNumber))
                        {
                            registrationNumber = this.GetRegistationNumber(this.claimModel.HeaderDto, this.GetNameInvolmentDataID(claimDetailDto, (short)StaticValues.NameInvolvementType_ClaimNameInvolvement.Driver), dtos);
                        }
                    }
                    else if (number > 1)
                    {
                        registrationNumber = AXAClaimConstants.CONST_VARIOUS;
                    }
                    else
                    {
                        var insuredobject = insuredObjects.FirstOrDefault(io => (io.Data as IClaimInsuredObjectData).InternalIOType == (short)StaticValues.InternalIOType.Vehicle);
                        if (insuredobject.ClaimIOVehicles != null && insuredobject.ClaimIOVehicles.Count() > 0)
                        {
                            var claimIOVehicle = insuredobject.ClaimIOVehicles.FirstOrDefault();
                            if (claimIOVehicle != null)
                            {
                                registrationNumber = (claimIOVehicle.Data as IClaimIOVehicleData).RegistrationNumber;
                            }
                        }
                    }

                    if (registrationNumber != null)
                    {
                        claimDetailDto.Data.CustomProperties["RegistrationNumber"] = registrationNumber;
                    }
                }
            });
        }

        private string GetRegistationNumber(ClaimHeaderDto claimHeader, List<Guid> dataID, Dictionary<Guid, DtoBase> dtos)
        {
            string registrationNumber = null;
            if (claimHeader.ClaimInvolvementLinks != null)
            {
                foreach (Guid guidId in dataID)
                {
                    // ClaimInvolvementLinks that has ClaimInvolvementFromDataID equals to NameInvolvementDataID
                    var fromClaimInvolvementDataLinks = claimHeader.ClaimInvolvementLinks.Where((cil => (cil.Data as IClaimInvolvementLinkData).ClaimInvolvementFromDataId != null && (cil.Data as IClaimInvolvementLinkData).ClaimInvolvementFromDataId == guidId));

                    if (fromClaimInvolvementDataLinks != null && fromClaimInvolvementDataLinks.Count() > 0)
                    {
                        List<Guid> toClaimInvolvementDataIDs = fromClaimInvolvementDataLinks.Select(clml => (clml.Data as IClaimInvolvementLinkData).ClaimInvolvementToDataId).ToList();
                        registrationNumber = this.GetRegistrationNumerOfLinkedVehicleObject(toClaimInvolvementDataIDs, dtos);
                    }
                    else
                    {
                        registrationNumber = string.Empty;
                    }
                }
            }

            return registrationNumber;
        }

        private List<Guid> GetNameInvolmentDataID(ClaimDetailDto claimDetailDto, short claimNameInvolvement)
        {
            var nameInvolvementLinks = claimDetailDto.ClaimDetailToClaimInvolvementLinks.Where(
                lnk => lnk.ClaimNameInvolvement != null &&
                (lnk.ClaimNameInvolvement.Data as IClaimNameInvolvementData).NameInvolvementMaintenanceStatus == (short)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest
                && (lnk.ClaimNameInvolvement.Data as IClaimNameInvolvementData).NameInvolvementType == claimNameInvolvement);

            if (nameInvolvementLinks != null)
            {
                return nameInvolvementLinks.Select(item => item.ClaimInvolvement.ClaimInvolvementData.DataId).ToList<Guid>();
            }

            return new List<Guid> { };
        }

        private void AssignClaimantDetails()
        {
            if (this.claimModel == null)
            {
                return;
            }

            ClaimHeaderDto claimHeaderDto = this.claimModel.HeaderDto;
            claimHeaderDto.ClaimDetails.ForEach(claimDetailDto =>
            {
                string listName = string.Empty;

                if (claimDetailDto.Data.CustomProperties.ContainsKey("ClaimantNameID"))
                {
                    long nameId = (long)claimDetailDto.Data.CustomProperties["ClaimantNameID"];
                    var nameInvolvment = claimHeaderDto.ClaimNameInvolvements.Where(a => a.ClaimNameInvolvment.ClaimNameInvolvementData.NameID == nameId).FirstOrDefault();
                    if (nameInvolvment != null)
                    {
                        listName = nameInvolvment.ClaimNameInvolvment.ClaimNameInvolvementData.ListName;
                    }
                }

                claimDetailDto.Data.CustomProperties["ClaimantListName"] = listName;
            });
        }

        private string GetRegistrationNumerOfLinkedVehicleObject(List<Guid> toClaimInvolvementDataIDs, Dictionary<Guid, DtoBase> dtoList)
        {
            string registrationNumber = string.Empty;
            int count = 0;
            foreach (Guid toDtoID in toClaimInvolvementDataIDs)
            {
                DtoBase dtoBaseObject = dtoList.Where(dto => dto.Key == toDtoID).Select(dto => dto.Value).First();

                ClaimInvolvementDto _ClaimInvolvementDto = dtoBaseObject as ClaimInvolvementDto;

                if ((_ClaimInvolvementDto.Data as IClaimInvolvementData).InternalIOType == (short)StaticValues.InternalIOType.Vehicle)
                {
                    count++;
                    if (_ClaimInvolvementDto.ClaimInsuredObjects != null && _ClaimInvolvementDto.ClaimInsuredObject.ClaimIOVehicles != null)
                    {
                        var claimIOvehiclesDtos = _ClaimInvolvementDto.ClaimInsuredObjects.Where(cno => (cno.Data as IClaimInsuredObjectData).InternalIOType == (short)StaticValues.InternalIOType.Vehicle)
                            .SelectMany(iov => iov.ClaimIOVehicles);
                        if (claimIOvehiclesDtos != null && claimIOvehiclesDtos.Count() > 0)
                        {
                            registrationNumber = (claimIOvehiclesDtos.First().Data as IClaimIOVehicleData).RegistrationNumber;
                        }
                    }
                    else
                    {
                        registrationNumber = string.Empty;
                    }
                }
                else
                {
                    registrationNumber = string.Empty;
                }
            }

            if (count > 1)
            {
                registrationNumber = AXAClaimConstants.CONST_VARIOUS;
            }

            return registrationNumber;
        }
    }
}
