using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using XIAP.Frontend.Infrastructure.Tree;
using XIAP.FrontendModules.Common.ClaimService;
using Xiap.Framework.Data;
using Xiap.Framework.Entity;
using XIAP.FrontendModules.Infrastructure.NavTree;
using Xiap.Metadata.Data.Enums;
using XIAP.Frontend.Infrastructure;
using XIAP.FrontendModules.Claims.Model;

namespace GeniusX.AXA.FrontendModules.Claims.Navigation
{
    public class AXAClaimDetailChildrenAvailabilityBehaviour : INodeAvailabilityBehaviour 
    {
        private bool  IsChildAllowedForClaimDetail(TreeStructureStore child, DtoBase parent)
        {
            IClaimDetailData claimDetailData = null;
            claimDetailData = parent.Data as ClaimDetailData;

                switch (child.Node)
                {
                    case "ClaimDetailMainDetails":
                         if (claimDetailData.ClaimDetailTypeCode == AXAClaimConstants.CLAIMDETAILTYPE_AD || claimDetailData.ClaimDetailTypeCode == AXAClaimConstants.CLAIMDETAILTYPE_TPVD || claimDetailData.ClaimDetailTypeCode == AXAClaimConstants.CLAIMDETAILTYPE_TPPD || claimDetailData.ClaimDetailTypeCode == AXAClaimConstants.CLAIMDETAILTYPE_TPI || claimDetailData.ClaimDetailTypeCode == AXAClaimConstants.CLAIMDETAILTYPE_LIA)
                        {
                            return true;
                        }

                        break ;
                    case "CurrentReserve":
                        if (claimDetailData.ClaimDetailTypeCode == AXAClaimConstants.CLAIMDETAILTYPE_AD || claimDetailData.ClaimDetailTypeCode == AXAClaimConstants.CLAIMDETAILTYPE_TPVD || claimDetailData.ClaimDetailTypeCode == AXAClaimConstants.CLAIMDETAILTYPE_TPPD || claimDetailData.ClaimDetailTypeCode == AXAClaimConstants.CLAIMDETAILTYPE_TPI || claimDetailData.ClaimDetailTypeCode == AXAClaimConstants.CLAIMDETAILTYPE_LIA)
                        {
                            return claimDetailData.HasHistoricalReserves.GetValueOrDefault(false) ? true : false;
                        }

                        break ;
                    case "CurrentRecoveryReserve":
                        if (claimDetailData.ClaimDetailTypeCode == AXAClaimConstants.CLAIMDETAILTYPE_AD || claimDetailData.ClaimDetailTypeCode == AXAClaimConstants.CLAIMDETAILTYPE_TPVD || claimDetailData.ClaimDetailTypeCode == AXAClaimConstants.CLAIMDETAILTYPE_TPPD || claimDetailData.ClaimDetailTypeCode == AXAClaimConstants.CLAIMDETAILTYPE_TPI || claimDetailData.ClaimDetailTypeCode == AXAClaimConstants.CLAIMDETAILTYPE_LIA)
                        {
                            return claimDetailData.HasHistoricalRecoveryReserves.GetValueOrDefault(false) ? true : false;
                        }

                        break;
                    case "ClaimDetailExtendedInfo":
                        if (claimDetailData.ClaimDetailTypeCode == AXAClaimConstants.CLAIMDETAILTYPE_AD || claimDetailData.ClaimDetailTypeCode == AXAClaimConstants.CLAIMDETAILTYPE_TPVD)
                        {
                            child.Label.DefaultValue = "Repair Information";
                            return true;
                        }
                        else if (claimDetailData.ClaimDetailTypeCode == AXAClaimConstants.CLAIMDETAILTYPE_TPI)
                        {
                            child.Label.DefaultValue = "Medical Reports";
                            return true;
                        }
                        else if (claimDetailData.ClaimDetailTypeCode == AXAClaimConstants.CLAIMDETAILTYPE_LIA)
                        {
                            child.Label.DefaultValue = "Exposure / Medical";
                            return true;
                        }

                        break;
                }

            return false;
        }

        public bool IsAvailable(XIAP.Frontend.Infrastructure.ITransactionController transactionController, TreeStructureStore definition, DtoBase parentDto)
        {
           return this.IsChildAllowedForClaimDetail(definition,parentDto);
        }
    }
}
