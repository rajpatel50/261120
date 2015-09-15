using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.Messages;
using Xiap.Framework.ProcessHandling;
using ProductXML = Xiap.Metadata.Data.XML.ProductVersion;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    /// <summary>
    /// When a new Claim Detail is created, sets the ClaimDetailReference and ensures it is unique among Claim Detail References.
    /// </summary>
    public class ClaimDetailReferenceDefaulterPlugin : AbstractComponentPlugin
    {
        public override ProcessResultsCollection ProcessComponent(IBusinessComponent component, ProcessInvocationPoint point, int pluginId)
        {
            ProcessResultsCollection processResults = new ProcessResultsCollection();

            if (point == ProcessInvocationPoint.Created)
            {
                bool isClaimDetailDefaulted = false;
                ClaimDetail claimDetail = (ClaimDetail)component;
                bool isUniqueClaimDetail = false;

                // default the Claim Detail Reference with AllocateClaimDetailReference 
                claimDetail.ClaimDetailReference = this.AllocateClaimDetailReference(claimDetail, claimDetail.GetProduct(), ref isClaimDetailDefaulted);

                // Validate that the ClaimDetailRef is unique for this ClaimDetailID 
                isUniqueClaimDetail = ClaimDetailReferenceDefaulterPlugin.IsUniqueClaimDetailReference(claimDetail, point, isClaimDetailDefaulted);
                if (!isUniqueClaimDetail)
                {
                    processResults = ClaimsBusinessLogicHelper.AddError(processResults, MessageConstants.ClaimDetailRefNotUnique, point, component);
                }
            }

            return processResults;
        }

        private string AllocateClaimDetailReference(ClaimDetail claimDetail, ProductXML.ProductClaimDetail productClaimDetail, ref bool isClaimDetailDefaulted)
        {
            try
            {
                if (productClaimDetail != null)
                {
                    int clmDetRef = 0;
                    var num = claimDetail.ClaimHeader.ClaimDetails.Max(c => int.TryParse(c.ClaimDetailReference, out clmDetRef) == false? 0 : clmDetRef);
                    claimDetail.ClaimDetailReference = (num + 1).ToString();
                    isClaimDetailDefaulted = true;
                }
            }
            catch
            {
                //// do nothing no need for showing any messages.
            }

            return claimDetail.ClaimDetailReference;
        }

        /// <summary>
        /// This method will validate that the ClaimDetailRef is unique for this ClaimDetailID 
        /// </summary>
        /// <param name="claimDetail">Claim Detail Business Component</param>
        /// <param name="point">Process Invocation Point</param>
        /// <param name="isClaimDetailDefaulted">Has the claim detail reference been defaulted</param>
        /// <returns>True if the ClaimDetailReference is unique for the ClaimDetailID else false</returns>
        internal static bool IsUniqueClaimDetailReference(ClaimDetail claimDetail, ProcessInvocationPoint point, bool isClaimDetailDefaulted)
        {
            bool isUniqueClaimDetailRef = false;
            if (!String.IsNullOrEmpty(claimDetail.ClaimDetailReference) && (claimDetail.DirtyPropertyList.ContainsKey("ClaimDetailReference") || isClaimDetailDefaulted || point == ProcessInvocationPoint.Validation))
            {
                isUniqueClaimDetailRef = claimDetail.ClaimHeader.ClaimDetails.FirstOrDefault(c => c.DataId != claimDetail.DataId && c.ClaimDetailReference == claimDetail.ClaimDetailReference) == null;
            }

            return isUniqueClaimDetailRef;
        }

        internal static void IsUniqueClaimDetailReference(ReadOnlyCollection<ClaimDetail> claimDetails, PluginHelper<ClaimHeader> pluginHelper)
        {
            List<string> duplicateClaimDetailReferences = new List<string>();
            foreach (ClaimDetail claimDetail in claimDetails)
            {
                if (!String.IsNullOrEmpty(claimDetail.ClaimDetailReference))
                {
                    ClaimDetail duplicateclaimDetail = claimDetail.ClaimHeader.ClaimDetails.Where(c => c.DataId != claimDetail.DataId && c.ClaimDetailReference == claimDetail.ClaimDetailReference).FirstOrDefault();
                    if (duplicateclaimDetail != null && !duplicateClaimDetailReferences.Any(x => x.Equals(claimDetail.ClaimDetailReference)))
                    {
                        duplicateClaimDetailReferences.Add(duplicateclaimDetail.ClaimDetailReference);
                        ClaimsBusinessLogicHelper.AddError(pluginHelper.ProcessResults, MessageConstants.ClaimDetailRefNotUnique, pluginHelper.InvocationPoint, pluginHelper.Component);
                    }
                }
            }
        }
    }
}
