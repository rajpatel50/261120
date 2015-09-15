using GeniusX.AXA.Claims.BusinessLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework.Common;
using Xiap.Framework.Data;
using Xiap.Framework.Metadata;
using Xiap.Metadata.Data.Enums;
using Xiap.Framework.Entity;
using Xiap.Metadata.BusinessComponent;
using Xiap.Framework.Configuration;
using System.Xml;
using System.Data;
using FrameworkSecurity = Xiap.Framework.Security;

namespace GeniusX.AXA.DPService
{
    public class AXADMSMetadataService: IDMSMetadataService
    {
        private const string CLAIM_REFERENCE_FIELDNAME = "Claim_Reference";
        private const string LOB_FIELDNAME = "LOB";
        private const string ENTITY_FIELDNAME = "Entity";
        private const string DOCUMENT_TYPE_FIELDNAME = "Document_Type";
        private const string CATEGORY_FIELDNAME = "Document_Category";
        private const string PRIORITY_FIELDNAME = "Document_Priority";
        private const string DESCRIPTION_FIELDNAME = "Document_Description";
        private const string SENDER_FIELDNAME = "Sender";
        private const string ORIGINAL_DOCUMENT_FIELDNAME = "Original_Document";
        private const string INCOMING_OUTGOING_FIELDNAME = "Incoming_Outgoing";
        private const string CONFIDENTIAL_FIELDNAME = "Document_Confidential";
        private const string ACTIVE_FIELDNAME = "Document_Active";
        private const string EXTERNAL_REFERENCE_FIELDNAME = "External_Reference";
        private const string ACQUISITION_CHANNEL_FIELDNAME = "Acquisition_Channel";
        private const string DIGITALIZATION_ID_FIELDNAME = "Digitalization_ID";
        private const string PROCESSING_DATE_FIELDNAME = "Processing_Date";
        private const string RECEPTION_DATE_FIELDNAME = "Reception_Date";
        private const string FILE_NAME_FIELDNAME = "File_Name";
        private const string UPLOAD_DATE_FIELDNAME = "Upload_Date";
        private const string UPLOADED_BY_FIELDNAME = "Uploaded_By";
        private const string RELATED_TO_FIELDNAME = "Related_To";
        private const string DOCUMENT_TEMPLATE_FIELDNAME = "Document_Template";
        private const string DOCUMENT_GROUP_REFERENCE_FIELDNAME = "Document_Group_Reference";
        private const string GENERATE_TASK_FIELDNAME = "Generate_Task";

        /// <summary>
        /// This method fetches the metadata of the passed in document.
        /// </summary>
        /// <param name="document">The document component</param>
        /// <param name="documentControlLog">The document control component</param>
        /// <param name="metadataDocument">Meta data</param>
        /// <param name="reference">Reference of the claim</param>
        /// <param name="detailReference">Reference of the claim detail</param>
        /// <returns>A dictionary object of all the metadata.</returns>
        public Dictionary<string, object> GetMetadata(Xiap.Metadata.Data.IDocumentData document, Xiap.Metadata.Data.IDocumentControlLogData documentControlLog, IMetadataDocument metadataDocument, string reference, string detailReference)
        {
            Dictionary<string, object> metadata = new Dictionary<string, object>();
            ClaimDocument claimDocument = null;
            if (document is ClaimDocument)
            {
                claimDocument = (ClaimDocument)document;
            }
            else
            {
                return null;
            }

            string documentType = claimDocument.DocumentTypeCode;
            if (documentType != null)
            {
                //// Add Long description of DocumentType field.
                string documentTypeLongDesc = claimDocument.DocumentTypeCodeField.AllowedValues(Xiap.Framework.Metadata.Enumerations.DescriptionType.LongDescription).FirstOrDefault(a => a.Code == documentType).Description;
                metadata.Add(DOCUMENT_TYPE_FIELDNAME, documentTypeLongDesc);
            }
            
            string customCode01 = document.CustomCode01;
            if (customCode01 != null)
            {
                //// Add Long description of CustomCode01 field.
                string customCode01LongDesc = claimDocument.CustomCode01Field.AllowedValues(Xiap.Framework.Metadata.Enumerations.DescriptionType.LongDescription).FirstOrDefault(a => a.Code == customCode01).Description;
                metadata.Add(CATEGORY_FIELDNAME, customCode01LongDesc);
            }
             
            string customCode02 = document.CustomCode02;
            if (customCode02 != null)
            {
                //// Add Long description of CustomCode02 field.
                string customCode02LongDesc = claimDocument.CustomCode02Field.AllowedValues(Xiap.Framework.Metadata.Enumerations.DescriptionType.LongDescription).FirstOrDefault(a => a.Code == customCode02).Description;
                metadata.Add(PRIORITY_FIELDNAME, customCode02LongDesc);
            }
            
            string description = document.Description;
            if (description != null)
            {
                metadata.Add(DESCRIPTION_FIELDNAME, description);
            }

            string customDescription = document.CustomDescription01;
            if (customDescription!=null)
            {
                metadata.Add(SENDER_FIELDNAME, customDescription);
            }

            string customCode03 = document.CustomCode03;
            if (customCode03 != null)
            {
                //// Add Long description of CustomCode03 field.
                string customCode03LongDesc = claimDocument.CustomCode03Field.AllowedValues(Xiap.Framework.Metadata.Enumerations.DescriptionType.LongDescription).FirstOrDefault(a => a.Code == customCode03).Description;
                metadata.Add(ORIGINAL_DOCUMENT_FIELDNAME, customCode03LongDesc);
            }

            string customCode04 = document.CustomCode04;
            if (customCode04 != null)
            {
                //// Add Long description of CustomCode04 field.
                string customCode04LongDesc = claimDocument.CustomCode04Field.AllowedValues(Xiap.Framework.Metadata.Enumerations.DescriptionType.LongDescription).FirstOrDefault(a => a.Code == customCode04).Description;
                metadata.Add(INCOMING_OUTGOING_FIELDNAME, customCode04LongDesc);
            }

            string customCode05 = document.CustomCode05;
            if (customCode05 != null)
            {
                //// Add Long description of CustomCode05 field.
                string customCode05LongDesc = claimDocument.CustomCode05Field.AllowedValues(Xiap.Framework.Metadata.Enumerations.DescriptionType.LongDescription).FirstOrDefault(a => a.Code == customCode05).Description;
                metadata.Add(CONFIDENTIAL_FIELDNAME, customCode05LongDesc);
            }

            string customCode06 = document.CustomCode06;
            if (customCode06 != null)
            {
                //// Add Long description of CustomCode06 field.
                string customCode06LongDesc = claimDocument.CustomCode06Field.AllowedValues(Xiap.Framework.Metadata.Enumerations.DescriptionType.LongDescription).FirstOrDefault(a => a.Code == customCode06).Description;
                metadata.Add(ACTIVE_FIELDNAME, customCode06LongDesc);
            }

            string customReference01 = document.CustomReference01;
            if (customReference01!=null)
            {
                metadata.Add(EXTERNAL_REFERENCE_FIELDNAME, customReference01);
            }

            string customCode07 = document.CustomCode07;
            if (customCode07 != null)
            {
                //// Add Long description of CustomCode07 field.
                string customCode07LongDesc = claimDocument.CustomCode07Field.AllowedValues(Xiap.Framework.Metadata.Enumerations.DescriptionType.LongDescription).FirstOrDefault(a => a.Code == customCode07).Description;
                if (customCode07LongDesc!= null)
                {
                    metadata.Add(ACQUISITION_CHANNEL_FIELDNAME, customCode07LongDesc);
                }
            }
             
            //// Claim Reference
            if (reference!=null)
            {
                metadata.Add(CLAIM_REFERENCE_FIELDNAME, reference);
            }

            string customReference02 = document.CustomReference02;
            if (customReference02!=null)
            {
                metadata.Add(DIGITALIZATION_ID_FIELDNAME, customReference02);
            }

            DateTime? customDate02 = document.CustomDate02;
            if (customDate02 != null)
            {
                metadata.Add(PROCESSING_DATE_FIELDNAME, customDate02);
            }

            
            string lob = null;
            ClaimHeader claimHeader = null;
            
            claimHeader = claimDocument.ClaimHeader;
            if (claimDocument.ClaimHeader != null)
            {
                lob = claimDocument.ClaimHeader.ClaimHeaderAnalysisCode01;
                
                if (lob != null)
                {
                    //// Add Long description of ClaimHeader.ClaimHeaderAnalysisCode01 field.
                    string claimHeaderAnalysisCode01LongDesc = claimHeader.ClaimHeaderAnalysisCode01Field.AllowedValues(Xiap.Framework.Metadata.Enumerations.DescriptionType.LongDescription).FirstOrDefault(a => a.Code == lob).Description;
                    if (claimHeaderAnalysisCode01LongDesc!=null)
                    {
                        metadata.Add(LOB_FIELDNAME, claimHeaderAnalysisCode01LongDesc);
                    }
                }
                
                string company = this.GetCompanyName(claimHeader);
                if (company != null)
                {
                    metadata.Add(ENTITY_FIELDNAME, company);
                }
            }
            

            DateTime? customDate01 = document.CustomDate01;
            if (customDate01 != null)
            {
                metadata.Add(RECEPTION_DATE_FIELDNAME, customDate01);
            }
   
            string documentName = documentControlLog.DocumentName;
            if (documentName!=null)
            {
                metadata.Add(FILE_NAME_FIELDNAME, documentName);
            }

            DateTime createdTime = documentControlLog.CreatedTime;
            metadata.Add(UPLOAD_DATE_FIELDNAME, createdTime);

            if (documentControlLog is ClaimDocumentControlLog)
            {
                ClaimDocumentControlLog claimDocumentControlLog = (ClaimDocumentControlLog)documentControlLog;
                if (claimDocumentControlLog.CreatedByUserID != null)
                {
                    //// Fetch User description
                    FrameworkSecurity.User userDetail = FrameworkSecurity.UserCacheService.GetUserById(claimDocumentControlLog.CreatedByUserID.GetValueOrDefault(0));
                    if (userDetail != null)
                    {
                        string createdByUser = userDetail.UserDescription;
                        if (createdByUser!=null)
                        {
                            metadata.Add(UPLOADED_BY_FIELDNAME, createdByUser);
                        }
                    }
                }
           }

            
            string relatedName = claimDocument.RelatedName;
            if (relatedName!=null)
            {
                metadata.Add(RELATED_TO_FIELDNAME, relatedName);
            }
        
            if (claimDocument!=null)
            {
                string documentTemplateCode = claimDocument.DocumentTemplateCode;
                if (documentTemplateCode != null)
                {
                    //// Add Long description of DocumentTemplateCode.
                    string documentTemplateCodeLongDesc = SystemValueSetCache.GetCodeDescription(claimDocument.DocumentTemplateCode, SystemValueSetCodeEnum.DocumentTemplate, true);
                    if (documentTemplateCodeLongDesc != null)
                    {
                        metadata.Add(DOCUMENT_TEMPLATE_FIELDNAME, documentTemplateCodeLongDesc);
                    }
                }
            }
            

            string documentGroupReference = document.DocumentGroupReference;
            if (documentGroupReference != null)
            {
                metadata.Add(DOCUMENT_GROUP_REFERENCE_FIELDNAME, documentGroupReference);
            }

            string customCode08 = claimDocument.CustomCode08;
            if (customCode08 != null)
            {
                //// Add Long description of customCode08 field.
                string customCode08LongDesc = claimDocument.CustomCode08Field.AllowedValues(Xiap.Framework.Metadata.Enumerations.DescriptionType.LongDescription).FirstOrDefault(a => a.Code == customCode08).Description;
                metadata.Add(GENERATE_TASK_FIELDNAME, customCode08LongDesc);
            }

            //// Need to add Productcode to the metadata as it is being used in AXASharepointHandler
            string productCode =claimHeader.ProductCode;
            if (productCode!=null)
            {
                metadata.Add("ProductCode", productCode);
            }
   
            return metadata;
        }

        private string GetCompanyName(ClaimHeader claimHeader)
        {
            if (!claimHeader.NameInvolvements.Any())
            {
                return null;
            }

            var claimNameInv = claimHeader.NameInvolvements.Where(a=> a.NameInvolvementType == 12 && ((ClaimNameInvolvement)a).NameInvolvementMaintenanceStatus == (short)StaticValues.ClaimNameInvolvementMaintenanceStatus.Latest).FirstOrDefault();
            if (claimNameInv ==null)
            {
                return null;
            }

            if (claimNameInv.NameID==null)
            {
                return null;
            }

            long nameId = (long)claimNameInv.NameID;
            return ClaimsBusinessLogicHelper.GetListName(nameId);
        }
    }
}
