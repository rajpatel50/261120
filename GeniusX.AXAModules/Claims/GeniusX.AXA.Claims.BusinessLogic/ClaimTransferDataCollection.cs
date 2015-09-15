using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.Common;
using Xiap.Framework.Logging;
using Xiap.Framework.Validation;
using Xiap.Metadata.BusinessComponent;
using Xiap.Metadata.Data.Enums;
using XiapClaim = Xiap.Claims.Data.XML;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    /// <summary>
    /// This class handles the XML data for the claim used in transfering it.
    /// XiapClaim is an XML serialised version of the Claim.
    /// </summary>
	public class ClaimTransferDataCollection : IDataCollection
	{
		private static readonly ILogger _Logger = LoggerFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private const string ReferenceArgument = "reference";
		private const string DetailReferenceArgument = "detailReference";
		private const string DateTimeArgument = "dateTime";

        /// <summary>
        /// Returns the claim data detailed in the input parameters as an XML object
        /// with the deductibles as a separate block at the bottom.
        /// </summary>
        /// <param name="parameters">dictionary collection</param>
        /// <returns>xml element</returns>
		public XmlElement GetData(IDictionary<string, object> parameters)
		{
			if (_Logger.IsInfoEnabled)
			{
				_Logger.Info("ClaimTransferDataCollection.GetData");
			}

			ArgumentCheck.ArgumentNullCheck(parameters, "parameters");

            // Get the values from the passed in parameters dictionary
			string reference = (string)parameters[ReferenceArgument];
			string detailReference = (string)parameters[DetailReferenceArgument];
			long claimTransactionHeaderID;
			long.TryParse(detailReference, out claimTransactionHeaderID);
			DateTime dateTime = (DateTime)parameters[DateTimeArgument];
		
			if (_Logger.IsInfoEnabled)
			{
				_Logger.Info(string.Format("Loading claim {0}", reference));
			}

            // Load the XML claim targetClaim from the claim reference parameter
			XiapClaim.ClaimHeader targetClaim = LoadClaim(reference);
            // Then load the claim details on the target claim
			targetClaim.ClaimDetails = ClaimTransferDataTransform.FilterClaimDetails(targetClaim);

			Dictionary<string, string> fundedDeductibles = new Dictionary<string, string>();
			Dictionary<string, string> fundedDeductiblePolicies = new Dictionary<string, string>();
			List<long> policyGenericDataTypeVersions;
			string riskProductCode = null;

              XiapClaim.ClaimTransactionHeader cth = new XiapClaim.ClaimTransactionHeader();
            // If there are ClaimTransactionHeaders on the claim we loaded, filter them down to only the one matching the CTH Id passed in.
			if (targetClaim.ClaimTransactionHeaders != null)
			{
				targetClaim.ClaimTransactionHeaders = targetClaim.ClaimTransactionHeaders.Where(a => a.ClaimTransactionHeaderID == claimTransactionHeaderID).ToArray();
            }

			// Restructure the Claim Transaction Details to rollup non funded deductible payments and reserves
			cth = targetClaim.ClaimTransactionHeaders.FirstOrDefault();

			using (MetadataEntities entities = MetadataEntitiesFactory.GetMetadataEntities())
			{
				riskProductCode = ClaimTransferDataTransform.GetRiskProductCode(entities, targetClaim.ClaimProductVersionID);

                // Generic data type Code = "AND4" and gdt.CustomCode02 = "Deductible Type 02"
				policyGenericDataTypeVersions = (from gdt in entities.GenericDataType
												 join gdtv in entities.GenericDataTypeVersion on gdt.Code equals gdtv.GenericDataType.Code
											where gdt.CustomCode02 == "P"
											select gdtv.GenericDataTypeVersionID).ToList();

                // iF there are Funded Deductible types against the Policy for our Product
                // collect all the funded deductible policies entered against the claim.
				if (targetClaim.GenericDataSetID != null && policyGenericDataTypeVersions.Count > 0)
				{
					fundedDeductiblePolicies = GetFundedDeductiblePolicies(targetClaim.GenericDataSetID.Value, policyGenericDataTypeVersions);
				}

				if (fundedDeductiblePolicies.Count() == 0)
				{
					if (_Logger.IsInfoEnabled)
					{
						_Logger.Info(string.Format("No deductible policy references on claim {0}", targetClaim.ClaimReference));
					}
				}

                // If we have a claim transaction header and it has a valid source get the funded deductible movment types from Claim Transactoin Header
                if (cth != null && cth.ClaimTransactionSource != null)
                {
                    fundedDeductibles = GetFundedDeductibleMovementTypes(entities, (long)targetClaim.ClaimProductVersionID, riskProductCode, targetClaim.UWHeaderExternalReference, fundedDeductiblePolicies, cth);
                }

				if (fundedDeductibles.Count == 0)
				{
					if (_Logger.IsInfoEnabled)
					{
						_Logger.Info(string.Format("No deductible movements on claim {0}", targetClaim.ClaimReference));
					}
				}
			}

			XmlNode deductibles = null;

          

            // Decode the claim transaction details on each Claim Transaction group on the Claim Transaction Header, 
            // for the funded deductible movement types stored previously.
            if (cth != null)
            {
                foreach (XiapClaim.ClaimTransactionGroup ctg in cth.ClaimTransactionGroups)
                {
                    ctg.ClaimTransactionDetails = ClaimTransferDataTransform.ConstructTransactiondetails(cth, ctg, fundedDeductibles);
                }
            }

            // Create the deductibles XML node of claim transaction details from the Claim Transaction Header
			deductibles = ClaimTransferDataTransform.ExtractDeductibleTransactions(cth, fundedDeductibles, fundedDeductiblePolicies, riskProductCode, targetClaim.ClaimHeaderID);

			// Remove any claim transaction detail from the risk claim if it is funded or has a zero movement amount
            if (cth != null)
            {
                cth = RemoveFundedAndNetZeroTransactions(fundedDeductibles, cth);
                // if there is no claim transaction details in a group then remove the claim transaction group 
                cth.ClaimTransactionGroups = cth.ClaimTransactionGroups.Where(ctg => ctg.ClaimTransactionDetails.Count() > 0).ToArray();
                // if claim transaction header has no claim transaction group, replace it with a blank, new Claim Transaction Header
                if (cth.ClaimTransactionGroups.Count() == 0)
                {
                    cth = new XiapClaim.ClaimTransactionHeader();
                }
            }

			if (_Logger.IsInfoEnabled)
			{
				_Logger.Info(string.Format("Serializing claim {0}", targetClaim.ClaimReference));
			}

            // Serialise the claim.
			XmlSerializer serializer = new XmlSerializer(typeof(XiapClaim.ClaimHeader));
			StringWriter sw = new StringWriter();
			serializer.Serialize(sw, targetClaim);
			string XmlClaim = sw.ToString();

            // Create a new document to add this claim to
			XmlDocument document = new XmlDocument();
			XmlElement xmlNode = null;

            // Create the claim xml and add it to the document
			xmlNode = document.CreateElement("Claims");
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(XmlClaim);
			XmlNode newNode = document.ImportNode(doc.DocumentElement, true);
			xmlNode.AppendChild(newNode);

			if (deductibles != null)
			{
                // If we have a deductibles XML, add this to the XML document too.
				newNode = document.ImportNode(deductibles, true);
				xmlNode.AppendChild(newNode);
			}

			return xmlNode;
		}

        /// <summary>
        /// Method used to remove Funded and net zero transaction from claim transaction.
        /// </summary>
        /// <param name="fundedDeductibles">dictionary collection</param>
        /// <param name="cth">Claim Transaction Header Type</param>
        /// <returns>Claim Transaction Header</returns>
		private static XiapClaim.ClaimTransactionHeader RemoveFundedAndNetZeroTransactions(Dictionary<string, string> fundedDeductibles, XiapClaim.ClaimTransactionHeader cth)
		{
            bool isReservesWithNonZeroMovementsPresent = false;
			foreach (XiapClaim.ClaimTransactionGroup ctg in cth.ClaimTransactionGroups)
			{
                // Find all claim transaction details in each claim transaction group, where there the movement type ISN'T in the list of funded deductibles passed in.
                ctg.ClaimTransactionDetails = ctg.ClaimTransactionDetails.Where(a => !fundedDeductibles.ContainsKey(a.MovementType)).ToArray();
                // Find out if there are any resereves with non-zero movments in this filtered list of claim transaction details
                isReservesWithNonZeroMovementsPresent = ClaimTransferDataTransform.IsReserveWithNonZeroMovementsPresent(ctg.ClaimTransactionDetails.ToList());
                // if the claimtransaction header isn't a Payment Cancellation, process for zero movement amounts
                if (cth.ClaimTransactionSource != (short)StaticValues.ClaimTransactionSource.PaymentCancellation)
                {
                    // Remove all ClaimTransactionDetails which have zero movement amounts
                    ctg.ClaimTransactionDetails = ctg.ClaimTransactionDetails.Where(a => !(a.MovementAmountOriginal == 0 && a.TransactionAmountOriginal == 0)).ToArray();
                }
                else if (!isReservesWithNonZeroMovementsPresent)
                {
                    // Payment cancellation can occur with only reserves. We need to check for this and then change the transaction source appropriately.
                    isReservesWithNonZeroMovementsPresent = ClaimTransferDataTransform.AreThereOnlyReservesOnClaimTransactionDetails(ctg.ClaimTransactionDetails.ToList());
			}
            }

            // Possibly amend the claim transaction source to reflect the presence of reserves with non-zero movements present
            cth.ClaimTransactionSource = GetTransactionSource(cth, isReservesWithNonZeroMovementsPresent);

			// Remove any claim transaction group with no remaining claim transaction details
			cth.ClaimTransactionGroups = cth.ClaimTransactionGroups.Where(a => a.ClaimTransactionDetails.Any()).ToArray();

			return cth;
		}

        /// <summary>
        /// Amends the Claim Transaction Header's transaction source where necessary.
        /// </summary>
        /// <param name="cth">Claim Transaction Header Type</param>
        /// <param name="isReservesWithNonZeroMovementsPresent">vool value</param>
        /// <returns>short value</returns>
        private static short GetTransactionSource(XiapClaim.ClaimTransactionHeader cth, bool isReservesWithNonZeroMovementsPresent)
		{
            short transSource = (short)cth.ClaimTransactionSource;

            // If we we have Reserves with Non-Zero Movements on them, return the transaction source as 'Reserve'.
            if (isReservesWithNonZeroMovementsPresent)
            {
                return transSource = (short)StaticValues.ClaimTransactionSource.Reserve;
            }

            // If the ClaimTransactionHeader is a payment but now only contains reserves, change it to a reserve transaction
			if (cth.ClaimTransactionSource == (short)StaticValues.ClaimTransactionSource.Payment)
			{
				var hasPayments = (from ctg in cth.ClaimTransactionGroups
								   where ctg.ClaimTransactionDetails != null
								   from ctd in ctg.ClaimTransactionDetails
								   where ctd.AmountType == (short)StaticValues.AmountType.Payment
								   select ctd.AmountType)
							 .Any();

				if (!hasPayments)
				{
					transSource = (short)StaticValues.ClaimTransactionSource.Reserve;
				}
			}

            // If the ClaimTransactionHeader is a RecoveryReceipt but now only conatans Recovery Reserves, set to a Recovery Reserve type.
			if (cth.ClaimTransactionSource == (short)StaticValues.ClaimTransactionSource.RecoveryReceipt)
			{
				var hasReceipts = (from ctg in cth.ClaimTransactionGroups
								   where ctg.ClaimTransactionDetails != null
								   from ctd in ctg.ClaimTransactionDetails
								   where ctd.AmountType == (short)StaticValues.AmountType.RecoveryReceipt
								   select ctd.AmountType)
							 .Any();

				if (!hasReceipts)
				{
					transSource = (short)StaticValues.ClaimTransactionSource.RecoveryReserve;
				}
			}

			return transSource;
		}

        /// <summary>
        /// method used to get funded deductible policies.
        /// A dictionary is returned of format Key = {deductible}, Value = {deductible policy reference}
        /// </summary>
        /// <param name="genericdataSetID">Generic Data Set ID</param>
        /// <param name="policyGenericDatasetVersions">List of long values for the Generic Dataset Versions</param>
        /// <returns>dictionary collection</returns>
		private static Dictionary<string, string> GetFundedDeductiblePolicies(long genericdataSetID, List<long> policyGenericDatasetVersions)
		{
			if (_Logger.IsInfoEnabled)
			{
				_Logger.Info("ClaimTransferDataCollection.GetFundedDeductiblePolicies");
			}

			Dictionary<string, string> policies = new Dictionary<string, string>();

            // Get a list of Data Items from via the Claims Query
            // then filter to only contain only the DataItem matching the GenericDataSetID value passed in.
            IEnumerable<IClaimGenericDataItem> dataItems = ObjectFactory.Resolve<IClaimsQuery>().GetGenericDataSetItems(genericdataSetID);
			IClaimGenericDataItem dataItem = dataItems.SingleOrDefault(a => policyGenericDatasetVersions.Contains((long)a.GenericDataTypeVersionID));

            // If the DataItem isn't null, add a policy to the list of Funded Deductible Policies for each that is found
            // on the DataItem, Deductible Policy references being stored in CustomReference01 to 05.
			if (dataItem != null)
			{
				if (dataItem.CustomReference01 != null)
				{
                    policies.Add(ClaimConstants.DED_DEDUCTIBLE01, dataItem.CustomReference01); // dataItem.CustomReference01 = deductible policy ref 1 
				}

				if (dataItem.CustomReference02 != null)
				{
                    policies.Add(ClaimConstants.DED_DEDUCTIBLE02, dataItem.CustomReference02); // dataItem.CustomReference02 = deductible policy ref 2
				}

				if (dataItem.CustomReference03 != null)
				{
                    policies.Add(ClaimConstants.DED_DEDUCTIBLE03, dataItem.CustomReference03); // dataItem.CustomReference03 = deductible policy ref 3
				}

				if (dataItem.CustomReference04 != null)
				{
                    policies.Add(ClaimConstants.DED_DEDUCTIBLE04, dataItem.CustomReference04); // dataItem.CustomReference04 = deductible policy ref 4
				}

				if (dataItem.CustomReference05 != null)
				{
                    policies.Add(ClaimConstants.DED_DEDUCTIBLE05, dataItem.CustomReference05); // dataItem.CustomReference05 = deductible policy ref 5
				}
			}

            // A dictionary is returned of format Key = {deductible}, Value = {deductible policy reference}
			return policies;
		}

        /// <summary>
        /// method used to get funded deductible movement types.
        /// </summary>
        /// <param name="entities">Metadata Entity</param>
        /// <param name="productVersion">long value</param>
        /// <param name="riskProductCode">string value</param>
        /// <param name="attachedPolicyRef">string value</param>
        /// <param name="fundedDeductiblePolicies">Dictionary collection</param>
        /// <param name="cth">Claim Transaction Header</param>
        /// <returns>Dictionary collection</returns>
        private static Dictionary<string, string> GetFundedDeductibleMovementTypes(MetadataEntities entities, long productVersion, string riskProductCode, string attachedPolicyRef, Dictionary<string, string> fundedDeductiblePolicies, XiapClaim.ClaimTransactionHeader cth)
        {
            string reserveMovementType;
           var movementTypes = new Dictionary<string, string>();

            // Get the claim definition from the Product Version passed in.
            ProductClaimDefinition pcd = (from claimDefinition in entities.ProductClaimDefinition
                                          where claimDefinition.ProductVersionID == productVersion
                                          select claimDefinition).FirstOrDefault();


            // Add funded deductible movement types for Reserve, Payment and RecoveryReceipt, if this isn't a Recovery Reserve Claim Transaction Header
            if (cth.ClaimTransactionSource != (short)StaticValues.ClaimTransactionSource.RecoveryReserve)
            {
                GetFundedDeductibleMovementTypes(entities, productVersion, attachedPolicyRef, fundedDeductiblePolicies, pcd, null, null, false, ref movementTypes);
            }

            // Add funded deductible movement types for recovery reserve if the CTH is a RecoveryReserve or Recovery Receipt
            if (cth.ClaimTransactionSource == (short)StaticValues.ClaimTransactionSource.RecoveryReserve || cth.ClaimTransactionSource == (short)StaticValues.ClaimTransactionSource.RecoveryReceipt)
            {
                // Cycle through all Claim Transaction Groups
                foreach (XiapClaim.ClaimTransactionGroup ctg in cth.ClaimTransactionGroups)
                {
                    // Cycle through each Claim Transaction Detail on each group
                    foreach (XiapClaim.ClaimTransactionDetail ctd in ctg.ClaimTransactionDetails)
                    {
                        // For any Recovery Reserve type amounts
                        if (ctd.AmountType == (short)StaticValues.AmountType.RecoveryReserve)
                        {
                            // Retrieve funded deductible reserve movement type on the basis of recovery reserve movement type
                            ClaimsBusinessLogicHelper.TryGetReserveMovementType(ctd.MovementType, riskProductCode, out reserveMovementType);

                            // Add to the list of types if we get a value.
                            if (!string.IsNullOrWhiteSpace(reserveMovementType))
                            {
                                GetFundedDeductibleMovementTypes(entities, productVersion, attachedPolicyRef, fundedDeductiblePolicies, pcd, ctd.MovementType, reserveMovementType, true, ref movementTypes);
                            }
                        }
                    }
                }
            }

            return movementTypes;
        }

        /// <summary>
        /// method used to get funded deductible movement types from the Metadata.
        /// </summary>
        /// <param name="entities">Metadata Entity value</param>
        /// <param name="productVersion">long value</param>
        /// <param name="attachedPolicyRef">string value</param>
        /// <param name="fundedDeductiblePolicies">Dictionary collection</param>
        /// <param name="pcd">Product Claim Definition Type</param>
        /// <param name="recoveryReserveMovementType">string value</param>
        /// <param name="reserveMovementType">string value</param>
        /// <param name="isRecoveryReserve">bool value</param>
        /// <param name="movementTypes">Dictionary collection - Passed by reference</param>
        /// <returns>Dictionary collection</returns>
        private static Dictionary<string, string> GetFundedDeductibleMovementTypes(MetadataEntities entities, long productVersion, string attachedPolicyRef, Dictionary<string, string> fundedDeductiblePolicies, ProductClaimDefinition pcd, string recoveryReserveMovementType, string reserveMovementType, bool isRecoveryReserve, ref Dictionary<string, string> movementTypes)
		{
			if (_Logger.IsInfoEnabled)
			{
				_Logger.Info("ClaimTransferDataCollection.GetFundedDeductibleMovementTypes");
			}

            // For each of the five Deductible codes, check if we are adding to the list of Dedctible moment type codes.
            AddDeductibleMovemnentTypeCodes(ClaimConstants.DED_DEDUCTIBLE01, pcd.InsurerFundedDeductible01MovementTypeCode, attachedPolicyRef, fundedDeductiblePolicies, recoveryReserveMovementType, reserveMovementType, isRecoveryReserve, ref movementTypes);
            AddDeductibleMovemnentTypeCodes(ClaimConstants.DED_DEDUCTIBLE02, pcd.InsurerFundedDeductible02MovementTypeCode, attachedPolicyRef, fundedDeductiblePolicies, recoveryReserveMovementType, reserveMovementType, isRecoveryReserve, ref movementTypes);
            AddDeductibleMovemnentTypeCodes(ClaimConstants.DED_DEDUCTIBLE03, pcd.InsurerFundedDeductible03MovementTypeCode, attachedPolicyRef, fundedDeductiblePolicies, recoveryReserveMovementType, reserveMovementType, isRecoveryReserve, ref movementTypes);
            AddDeductibleMovemnentTypeCodes(ClaimConstants.DED_DEDUCTIBLE04, pcd.InsurerFundedDeductible04MovementTypeCode, attachedPolicyRef, fundedDeductiblePolicies, recoveryReserveMovementType, reserveMovementType, isRecoveryReserve, ref movementTypes);
            AddDeductibleMovemnentTypeCodes(ClaimConstants.DED_DEDUCTIBLE05, pcd.InsurerFundedDeductible05MovementTypeCode, attachedPolicyRef, fundedDeductiblePolicies, recoveryReserveMovementType, reserveMovementType, isRecoveryReserve, ref movementTypes);

			return movementTypes;
		}

        /// <summary>
        /// method used to add deductible movement type code.
        /// </summary>
        /// <param name="deductible">string value</param>
        /// <param name="deductibleMovementTypeCode">string value</param>
        /// <param name="attachedPolicyRef">string value</param>
        /// <param name="fundedDeductiblePolicies">Dictionary collection</param>
        /// <param name="recoveryReserveMovementType">string value</param>
        /// <param name="reserveMovementType">string value</param>
        /// <param name="isRecoveryReserve">bool value</param>
        /// <param name="movementTypes">Dictionary collection - passed by reference</param>
        private static void AddDeductibleMovemnentTypeCodes(string deductible, string deductibleMovementTypeCode, string attachedPolicyRef, Dictionary<string, string> fundedDeductiblePolicies, string recoveryReserveMovementType, string reserveMovementType, bool isRecoveryReserve, ref Dictionary<string, string> movementTypes)
        {
            // If this is a funded deductible type (checks the fundedDeductiblePolicies dictionary)
            if (ClaimTransferDataTransform.IsFundedDeductibleType(deductible, fundedDeductiblePolicies, attachedPolicyRef))
            {
                // and if the deductible movement type code passed in isn't null
                if (deductibleMovementTypeCode != null)
                {
                    // for a recovery reserve we only add if the reserve moment type is the same as the deductible type
                    if (isRecoveryReserve)
                    {
                        if (reserveMovementType == deductibleMovementTypeCode)
                        {
                            movementTypes.Add(recoveryReserveMovementType, deductible);
                        }
                    }
                    else
                    {
                        // We add all other types we find.
                        movementTypes.Add(deductibleMovementTypeCode, deductible);
                    }
                }
            }
        }
		
        /// <summary>
        /// method used to load claim.
        /// </summary>
        /// <param name="reference">string value</param>
        /// <returns>claim header component</returns>
		private static XiapClaim.ClaimHeader LoadClaim(string reference)
		{
			XiapClaim.ClaimHeader targetClaim = null;
			
            // Connect to the GeniusX system to retrieve Claim Entities.
			using (ClaimsEntities entities = ClaimsEntitiesFactory.GetClaimsEntities())
			{
				try
				{
                    // Open a connection, if necessary
					if (entities.Connection.State == System.Data.ConnectionState.Closed)
					{
						entities.Connection.Open();
					}

                    // Begin a transaction to read the claim
					var trans = entities.Connection.BeginTransaction(System.Data.IsolationLevel.Snapshot);

                    // Get claim header, via the claim reference passed in
					ClaimHeader claimHeader = (from header in entities.ClaimHeader
											   where header.ClaimReference.Equals(reference)
											   select header).Single();

                    // Map the entity to the XML claim, XiapClaim.
					IMappingStrategy<ClaimHeader, XiapClaim.ClaimHeader> strategy = new ClaimGenericMappingStrategy();
					targetClaim = strategy.MapSourceToTarget(claimHeader);

                    // Rollback our transaction
					trans.Rollback();
				}
				finally
				{
                    // Close our connection to GeniusX safely.
					if (entities != null && entities.Connection != null)
					{
						if (entities.Connection.State == System.Data.ConnectionState.Open)
						{
							entities.Connection.Close();
						}

						entities.Connection.Dispose();
					}
				}
			}

			return targetClaim;
		}
	}
}
