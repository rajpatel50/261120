using System.Linq;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.Common;
using Xiap.Framework.ProcessHandling;
using Xiap.Metadata.Data.Enums;
using Xiap.Claims.Data;
using Xiap.Metadata.BusinessComponent;
using System.Collections.Generic;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    /// <summary>
    /// Defaults in document data from the claim, if applicable.
    /// </summary>
    public class ClaimDocumentPlugin : AbstractComponentPlugin
    {
        private const string WORDINGCODE_LETGEN = "LETGEN";
        private const string WORDINGCODE_FORMGEN = "FORMGEN";
        private const string WORDINGCODE_LIBEMLH1 = "LIBEMLH1";
        private const string WORDINGCODE_LIBEMLH2 = "LIBEMLH2";
        private const string WORDINGCODE_LIBEMLH3 = "LIBEMLH3";
        private const string WORDINGCODE_LIBEMLH4 = "LIBEMLH4";
        private const string WORDINGCODE_LIBEMLH5 = "LIBEMLH5";
        private const string WORDINGCODE_MTREMLH1 = "MTREMLH1";
        private const string WORDINGCODE_MTREMLH2 = "MTREMLH2";
        private const string WORDINGCODE_MTREMLH3 = "MTREMLH3";
        private const string WORDINGCODE_MTREMLH4 = "MTREMLH4";
        private const string WORDINGCODE_MTREMLH5 = "MTREMLH5";

        public override ProcessResultsCollection ProcessComponent(IBusinessComponent component, ProcessInvocationPoint point, int pluginId)
        {
            PluginHelper<ClaimDocument> pluginHelper = new PluginHelper<ClaimDocument>(point, component as ClaimDocument, new ProcessResultsCollection());

            switch (point)
            {
                case ProcessInvocationPoint.PreValidationDefaulting:
                    this.UpdateDocumentData(component);
                    this.SetDefaultValues(component);
                    break;
            }

            return pluginHelper.ProcessResults;
        }

        /// <summary>
        /// Returns the value of 'Your Reference', that will contain the concatenated references from each recipient..
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="claimHeader">The claim header.</param>
        /// <returns>Your Reference. Note, returns NULL if there are no recipients for this document.</returns>
        private string  GetYourReference(ClaimDocument document, ClaimHeader claimHeader)
        {
            // Set your Reference
            string yourReference = null;
            if (document.DocumentRecipients != null)
            {
                List<IDocumentRecipient> docuemntRecipients = document.DocumentRecipients.Where(r => r.RecipientType == (short)StaticValues.RecipientType.To || r.RecipientType == (short)StaticValues.RecipientType.Cc).ToList<IDocumentRecipient>();
                // If we have one recipient then 'Your Reference' is only Custom Reference 01 from the Document Recipient Name Involvement.
                // Otherwise, we cycle through each recipient and prefix their reference with their name, putting a new line before each.
                if (docuemntRecipients.Count > 1)
                {
                    foreach (ClaimDocumentRecipient dr in docuemntRecipients)
                    { 
                        ClaimNameInvolvement ni = (ClaimNameInvolvement)claimHeader.NameInvolvements.Where(a => a.DataId == dr.DocumentRecipientNameInvolvementID).FirstOrDefault();

                        if (ni != null)
                        {
                            yourReference = yourReference + System.Environment.NewLine + dr.RecipientName + " " + "Reference: " + ni.CustomReference01;   // UI Label = Reference; Reference common to all named involvements
                        }
                    }
                }
                else if (docuemntRecipients.Count  == 1)
                {
                    ClaimDocumentRecipient dr = docuemntRecipients.First() as ClaimDocumentRecipient;
                    ClaimNameInvolvement ni = (ClaimNameInvolvement)claimHeader.NameInvolvements.Where(a => a.DataId == dr.DocumentRecipientNameInvolvementID).FirstOrDefault();
                    if (ni != null)
                    {
                        yourReference = ni.CustomReference01;   // UI Label = Reference; Reference common to all named involvements
                    }
                }
            }

           return yourReference; 
        }

        /// <summary>
        /// Gets the client, the Major Insured's list name.
        /// </summary>
        /// <param name="claimHeader">The claim header.</param>
        /// <returns>List Name of Major Insured or an Empty string if no Major Insured.</returns>
        private string GetClient(ClaimHeader claimHeader)
        {
            // Attempt to retrieve the Major Insured name involvement.
            ClaimNameInvolvement majorInsured = (ClaimNameInvolvement)claimHeader.NameInvolvements.Where(a => a.NameID != null && a.NameInvolvementType == (short)StaticValues.NameInvolvementType.MajorInsured && ((ClaimNameInvolvement)a).NameInvolvementMaintenanceStatus == (short)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest).FirstOrDefault();
            if (majorInsured != null && majorInsured.NameID != null)
            {
                long nameId = (long)majorInsured.NameID;
                return ClaimsBusinessLogicHelper.GetListName(nameId);
            }

            return string.Empty;
        }

        /// <summary>
        /// Gets the driver's list name
        /// </summary>
        /// <param name="claimHeader">The claim header.</param>
        /// <returns>List Name of Driver or an Empty string if no driver found.</returns>
        private string GetDriver(ClaimHeader claimHeader)
        {
            ClaimNameInvolvement driver = (ClaimNameInvolvement)claimHeader.NameInvolvements.Where(a => a.NameID != null && a.NameInvolvementType == (short)StaticValues.NameInvolvementType.Driver && ((ClaimNameInvolvement)a).NameInvolvementMaintenanceStatus == (short)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest).FirstOrDefault();
            if (driver != null && driver.NameID != null)
            {
                long nameId = (long)driver.NameID;
                return ClaimsBusinessLogicHelper.GetListName(nameId);
            }

            return string.Empty;
        }

      
        /// <summary>
        /// Gets our vehicle reg number for the insured driver's vehicle.
        /// The method reads from the list of associated Insured Objects, checking if any link to a Driver name involvement
        /// and if they are a vehicle. The first Insured Object meeting this criteria has the Vehicle Registration Number returned.
        /// </summary>
        /// <param name="claimHeader">The claim header.</param>
        /// <returns>Registration number (empty, if none found).</returns>
        private string GetOurVehicalRegNumber(ClaimHeader claimHeader)
        {
            string ourVehicalRegNumber = string.Empty;
            // Get a list of claim involvements relating to 'Insured Objects' (e.g. vehicles) for this Claim
            List<ClaimInvolvement> claimInvolvements = claimHeader.ClaimInvolvements.Where(ni => ni.ClaimInvolvementType == (short)StaticValues.LinkableComponentType.InsuredObject).ToList<ClaimInvolvement>();

            foreach (ClaimInvolvement cl in claimInvolvements)
            {
                // Find if another Claim Involvement for this Claim Header links to this Insured Object 
                ClaimInvolvementLink claimInvLink = claimHeader.ClaimInvolvementLinks.Where(c => c.ClaimInvolvementTo.ClaimInvolvementID == cl.ClaimInvolvementID && c.ClaimInvolvementTo.DataId == cl.DataId).FirstOrDefault();

                if (claimInvLink != null)
                {
                    // We have a link so find the Claim Involvment that links to this Insured Object
                    long id = claimInvLink.ClaimInvolvementFrom.ClaimInvolvementID;
                    ClaimInvolvement ci = claimHeader.ClaimInvolvements.FirstOrDefault(n => n.ClaimInvolvementID == id && n.DataId == claimInvLink.ClaimInvolvementFrom.DataId);

                    if (ci != null)
                    {
                        // Is the linking Claim Involvment a current Driver?
                        ClaimNameInvolvement cnn = ci.ClaimNameInvolvements.Where(c => c.NameInvolvementType == (short)StaticValues.NameInvolvementType.Driver && c.NameInvolvementMaintenanceStatus == (short)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest).FirstOrDefault();
                        if (cnn != null)
                        {
                            // Get the vehicle from the Insured Object Claim Involvement.
                            ClaimIOVehicle vehical = cl.ClaimInsuredObjects.First(x => x.InternalIOType == (short)StaticValues.InternalIOType.Vehicle).ClaimIOVehicles.FirstOrDefault();
                            if (vehical != null)
                            {
                                // The insured object is a vehicle so store the registration number.
                                ourVehicalRegNumber = vehical.RegistrationNumber;
                                break;
                            }
                        }
                    }
                }
            }

            return ourVehicalRegNumber;
        }


        /// <summary>
        /// Gets Third Party's vehicle reg number(s).
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="claimHeader">The claim header.</param>
        /// <returns>Third Party's registration number(s), or blank if none found.</returns>
        private string GetYourVehicalRegNumber(ClaimDocument document, ClaimHeader claimHeader)
        {
            string yourVehicalRegNumber = string.Empty;
            // Cycle through all the Claim Involvements for the claim.
            foreach (ClaimInvolvement cl in claimHeader.ClaimInvolvements)
            {
                bool  cnexists = cl.ClaimNameInvolvements.Any(c => c.DataId.Equals(document.RelatedNameInvolvementID));
                if (cnexists)
                {
                    // If a claim involvement exists of Related Name Involvment Type then get a list of any over Claim Involvements it links to.
                    List<ClaimInvolvementLink> claimInvolvementLinks = claimHeader.ClaimInvolvementLinks.Where(c => c.ClaimInvolvementFrom.ClaimInvolvementID == cl.ClaimInvolvementID && c.ClaimInvolvementFrom.DataId == cl.DataId).ToList<ClaimInvolvementLink>();

                    if (claimInvolvementLinks != null)
                    {
                        // Cycle through any links to this Related Name Involvement
                        foreach (ClaimInvolvementLink clink in claimInvolvementLinks)
                        {
                            // Only look at linking Claim Involvments that are Insured Objects
                            ClaimInvolvement lst = claimHeader.ClaimInvolvements.Where(ni => ni.ClaimInvolvementType == (short)StaticValues.LinkableComponentType.InsuredObject && ni.DataId == clink.ClaimInvolvementTo.DataId  && ni.ClaimInvolvementID == clink.ClaimInvolvementTo.ClaimInvolvementID).FirstOrDefault();
                            if (lst != null)
                            {
                                // Only accept a Vehicle type Insured Object.
                                ClaimIOVehicle vehical = lst.ClaimInsuredObjects.First(x=>x.InternalIOType == (short)StaticValues.InternalIOType.Vehicle).ClaimIOVehicles.FirstOrDefault();
                                if (vehical != null)
                                {
                                    // Concatenate this Vehicle Registration number with any others already retrieved.
                                    if (yourVehicalRegNumber == string.Empty)
                                    {
                                        yourVehicalRegNumber = vehical.RegistrationNumber;
                                    }
                                    else
                                    {
                                        yourVehicalRegNumber = yourVehicalRegNumber + ", " + vehical.RegistrationNumber;
                                    }
                                }
                            }
                        }
                    }

                    break;
                }
            }

            return yourVehicalRegNumber;
        }

         /// <summary>
        /// Sets the default values associated with the document, if applicable.
        /// </summary>
        /// <param name="component">The component.</param>
        private void SetDefaultValues(IBusinessComponent component)
        {
            ClaimDocument document = (ClaimDocument)component;
            ClaimHeader claimHeader = (ClaimHeader)document.ParentClaimHeader;
            if (this.IsDataEntryAllowedOnDocument(document) == true)
            {
                foreach (ClaimDocumentTextSegment textSegment in document.DocumentTextSegments)
                {
                    if (textSegment.WordingCode == WORDINGCODE_LIBEMLH1 || textSegment.WordingCode == WORDINGCODE_LIBEMLH2 || textSegment.WordingCode == WORDINGCODE_LIBEMLH3 || textSegment.WordingCode == WORDINGCODE_LIBEMLH4 || textSegment.WordingCode == WORDINGCODE_LIBEMLH5 ||
                        textSegment.WordingCode == WORDINGCODE_MTREMLH1 || textSegment.WordingCode == WORDINGCODE_MTREMLH2 || textSegment.WordingCode == WORDINGCODE_MTREMLH3 || textSegment.WordingCode == WORDINGCODE_MTREMLH4 || textSegment.WordingCode == WORDINGCODE_MTREMLH5)
                    {
                        // Set your Reference
                        textSegment.CustomText01 = this.GetYourReference(document, claimHeader);

                        // Set Incident Date from Claim Header Date of Event From
                        textSegment.CustomDate01 = claimHeader.DateOfEventFrom;

                        // Set our Client if we don't have the LIBEMLH1 wording code
                        if (textSegment.WordingCode != WORDINGCODE_LIBEMLH1)
                        {
                            // Get Major Insured List Name
                            textSegment.CustomText02 = this.GetClient(claimHeader);
                        }

                        // set 'Your Client'
                        if (textSegment.WordingCode == WORDINGCODE_MTREMLH2 || textSegment.WordingCode == WORDINGCODE_MTREMLH5 || textSegment.WordingCode == WORDINGCODE_LIBEMLH1 || textSegment.WordingCode == WORDINGCODE_LIBEMLH2 || textSegment.WordingCode == WORDINGCODE_LIBEMLH4 || textSegment.WordingCode == WORDINGCODE_LIBEMLH5)
                        {
                            textSegment.CustomText03 = document.RelatedName;
                        }

                        if (textSegment.WordingCode == WORDINGCODE_MTREMLH1 || textSegment.WordingCode == WORDINGCODE_MTREMLH2 || textSegment.WordingCode == WORDINGCODE_MTREMLH3 || textSegment.WordingCode == WORDINGCODE_MTREMLH4 || textSegment.WordingCode == WORDINGCODE_MTREMLH5)
                        {
                            // Set Driver
                            textSegment.CustomText04 = this.GetDriver(claimHeader);

                            // Set Our Vehicle Reg no
                            textSegment.CustomReference01 = this.GetOurVehicalRegNumber(claimHeader);

                            // Set Your vehicle reg no
                            if (textSegment.WordingCode == WORDINGCODE_MTREMLH2 || textSegment.WordingCode == WORDINGCODE_MTREMLH3)
                            {
                                textSegment.CustomReference02 = this.GetYourVehicalRegNumber(document, claimHeader);
                            }

                            // set Incident Location
                            if (textSegment.WordingCode != WORDINGCODE_MTREMLH5)
                            {
                                textSegment.CustomText05 = claimHeader.CustomText02;
                            }
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Updates the document Print action and the Finalise action, if applicable.
        /// </summary>
        /// <param name="component">The component.</param>
        private void UpdateDocumentData(IBusinessComponent component)
        {
            ClaimDocument document = (ClaimDocument)component;

            if (this.IsDataEntryAllowedOnDocument(document) == true)
            {
                foreach (ClaimDocumentTextSegment textSegment in document.DocumentTextSegments)
                {
                    if (textSegment.WordingCode == WORDINGCODE_LETGEN || textSegment.WordingCode == WORDINGCODE_FORMGEN)
                    {
                        // GetValueOrDefault() takes a default value to supply if none if found in the field.
                        // UI Label = Print & Finalise?; Wording Code LETGEN/FORMGEN
                        // Set action to 'Print' if this segment requests it.
                        if (textSegment.CustomBoolean04.GetValueOrDefault(false) == true)
                        {
                            document.DocumentPrintAction = (short)StaticValues.DocumentPrintAction.Print;
                        }
                        else
                        {
                            document.DocumentPrintAction = (short)StaticValues.DocumentPrintAction.DoNotPrint;
                        }

                        // Set action to 'Finalise' if print or finalise was requested.
                        if (textSegment.CustomBoolean04.GetValueOrDefault(false) == true || textSegment.CustomBoolean05.GetValueOrDefault(false) == true)
                        {
                            document.DocumentFinalizationAction = (short)StaticValues.DocumentFinalizationAction.Finalize;
                        }
                        else
                        {
                            document.DocumentFinalizationAction = (short)StaticValues.DocumentFinalizationAction.DoNotFinalize;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Determines whether data entry is allowed on the specified document.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <returns>True if data entry is allowed, false otherwise.</returns>
        private bool IsDataEntryAllowedOnDocument(ClaimDocument document)
        {
            bool result = false;

            // Checks if the document isn't at status 'final', it has DocumentTextSegments and any segments have been updated (are 'dirty')
            if (document.DocumentDraftStatus != (short)StaticValues.DocumentDraftStatus.Final 
                && document.DocumentTextSegments != null && document.DocumentTextSegments.Any(a => ((ClaimDocumentTextSegment)a).DirtyPropertyList.Count > 0))
            {
                IDocumentControlLog dcl = document.LatestDocumentControlLog;
                if (dcl == null)
                {
                    // There is no latest document control log in existence so data entry is allowed.
                    result = true;
                }
                else if ((document.IsDocumentResubmitted && dcl.DocumentStatus != (short)StaticValues.DocumentStatus.Uploaded)
                            || dcl.DocumentStatus == (short)StaticValues.DocumentStatus.DataEntryPending
                            || dcl.DocumentStatus == (short)StaticValues.DocumentStatus.Unprocessed)
                {
                    // Allow editing if document has been resubmitted, uploaded, is awaiting Data Entry or is unprocessed.
                    result = true;
                }
            }            

            return result;
        }
    }
}
