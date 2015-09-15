
namespace GeniusX.AXA.Claims.BusinessLogic
{
    public static class ClaimConstants
    {
        public const string SYSUPD_Event_Type = "SYSUPD";
        public const string Name_ID = "NameID";
        public const string Claim_Header_Status_Threshold = "ClaimHeaderStatusThreshold";
        public const string LITIGATION_OR_RECOVERY_NOT_ALLOWED = "LITIGATION_OR_RECOVERY_NOT_ALLOWED";
        public const string LITIGATION = "Litigation";
        public const string Recovery = "Recovery";
        public const string THRESHOLD_CAN_NOT_BE_LOWERED = "THRESHOLD_CAN_NOT_BE_LOWERED";
        public const string CLAIM_HEADERSTATUS_CODE = "ClaimHeaderStatusCode";
        public const string EVENT_TYPECODE_RESERVE = "ESTMD";
        public const string EVENT_TYPECODE_PAYMENT = "PAYMD";
        public const string EVENT_TYPECODE_RECOVERYRESERVE = "RCEMD";
        public const string EVENT_TYPECODE_RECOVERYRECEIPT = "RECMD";
        public const string EVENT_TYPECODE_PAYMENTCANCELLATION = "PAYCL";
        public const string CLAIM_DETAIL_CLOSED = "CDCL";
        public const string CLAIM_DETAIL_OPEN = "CDO";
        public const string CLAIM_DETAIL_TYPE_AD = "AD";
        public const string INSURED = "UIN";
        public const string BROKER = "UBK";
        public const string POLICY_HEADER_ID = "PolicyHeaderID";
        public const string POLICY_COVERAGE_ID = "PolicyCoverageID";
        public const string TRANSACTION_TYPE_CREATE_CLAIM = "CreateClaim";
        public const string TRANSACTION_TYPE_CHANGE_CLAIM_HANDLER = "ChangeClaimHandler";
        public const string TRANSACTION_TYPE_AMEND_CLAIM = "AmendClaim";
        public const string NoMaximumClaimDetailConfigured = "NoMaximumClaimDetailConfigured";
        public const string PAYMENTS = "Payment";
        public const string RECEIPT = "Receipt";
        public const string CLAIM_HANDLER_CHANGED = "Claim Handler Changed";
        public const string CLAIM_STATUS_CLAIM_OPENED_UNCONFIRMED = "CON";
        // MSG01
        public const string MANY_NOTIFIER_SPECIFIED_FOR_THE_CLAIM = "MANY_NOTIFIER_SPECIFIED_FOR_THE_CLAIM";
        // MSG02
        public const string NOTIFIER_MUST_BE_SPECIFIED_FOR_THE_CLAIM = "NOTIFIER_MUST_BE_SPECIFIED_FOR_THE_CLAIM";
        public const string CUSTOM_BOOLEAN_04 = "CustomBoolean04";
        public const string CUSTOM_NUMERIC_10 = "CustomNumeric10";
        public const string DED_DEDUCTIBLES_TAG = "<Deductibles></Deductibles>";
        public const string DED_POLICYREFERENCE = "PolicyReference";
        public const string DED_TRANSACTIONSOURCE = "TransactionSource";
        public const string DED_PRODUCT = "Product";
        public const string DED_PRODUCT_LIABCLAIM = "CGBIPC";
        public const string DED_PRODUCT_MOTOR = "CGBIMO";
        public const string DED_PRODUCT_GBIPC = "GBIPC";
        public const string DED_PRODUCT_GBSPC = "GBSPC";
        public const string DED_PRODUCT_GBIMO = "GBIMO";
        public const string DED_PRODUCT_GBSMO = "GBSMO";
        public const string DED_DEDUCTIBLE01 = "Deductible01";
        public const string DED_DEDUCTIBLE02 = "Deductible02";
        public const string DED_DEDUCTIBLE03 = "Deductible03";
        public const string DED_DEDUCTIBLE04 = "Deductible04";
        public const string DED_DEDUCTIBLE05 = "Deductible05";

        public const string CLAIMREFERENCEPREFIX_DECISIONTABLE = "CLMPREFIX";

        public const string CH_ANALYSISCODE_LIABILITY = "LIA";
        public const string CH_ANALYSISCODE_MOTOR = "MOT";
        public const string PRODUCT_LIABILITY_GRADESTRUCTURETYPE = "CGBLIA";
        public const string PRODUCT_MOTOR_GRADESTRUCTURETYPE_CLAIMDETAIL_TPI = "CGBMIN";
        public const string PRODUCT_MOTOR_GRADESTRUCTURETYPE_CLAIMDETAIL_NOTTPI = "CGBMTR";
        public const string CLAIMDETAILTYPECODE_TPI = "TPI";
        public const string PRODUCT_LIABCLAIM = "CGBIPC";
        public const string PRODUCT_MOTORCLAIM = "CGBIMO";
        public const string CLAIM_DETAILTYPECODE_LIA = "LIA";


        public const string COVERAGE_IS_ALREADY_ATTACHED = "COVERAGE_IS_ALREADY_ATTACHED";
        public const string POLICY_LINK_LEVEL = "PolicyLinkLevel";
        public const string DATE_OUTSIDE_THE_COVER_PERIOD = "DATE_OUTSIDE_THE_COVER_PERIOD";
        public const string DATE_OF_LOSS_TYPE_CODE = "DateOfLossTypeCode";
        public const string PAYMENT_ENTERED_POLICY_TRIGGER_TYPE_CHANGE_NOT_ALLOWED = "PAYMENT_HAVE_BEEN_ENTERED";
        public const string DATE_OF_EVENT_FROM = "DateOfEventFrom";
        public const string DATE_OF_LOSS_TYPE_CODE_OCCURRENCE = "O";

        public const string AMENDCLAIM = "AmendClaim";
        public const string EmptyGroup = "XEMPTY";
        public const string CLAIM_AMOUNT_UNAUTHORISED = "CLAIM_AMOUNT_UNAUTHORISED";

        public const string DEFAULT_CURRENCY_CODE = "GBP";
        public const string BUSINESS_SUPPORT_ROLE_SETTING = "BusinessSupportRole";

        public const string CONST_VARIOUS = "Various";

        public const string DATAFIELD_TASKONCLAIMFINALSETTLEMENT = "TaskOnClaimFinalSettlement";

        public const string FINANCIAL_AUTHORISATION_PROCESS = "FinancialAuthorisation";

        public const string CustomNumeric10 = "CustomNumeric10";
        public const string EVENT_TYPECODE_EXCESS = "CDXUPD";
        public const string EVENT_TYPECODE_DEDUCT = "CDDUPD";
        public const string EVENT_TYPECODE_TASKRSGN = "TASKRSGN";
        public const string PolicyDeductible01 = "PolicyDeductible01";
        public const string PolicyDeductible02 = "PolicyDeductible02";
        public const string PolicyDeductible03 = "PolicyDeductible03";
        public const string PolicyDeductible04 = "PolicyDeductible04";
        public const string PolicyDeductible05 = "PolicyDeductible05";
        public const string IsDeductible01PaidByInsurer = "IsDeductible01PaidByInsurer";
        public const string IsDeductible02PaidByInsurer = "IsDeductible02PaidByInsurer";
        public const string IsDeductible03PaidByInsurer = "IsDeductible03PaidByInsurer";
        public const string IsDeductible04PaidByInsurer = "IsDeductible04PaidByInsurer";
        public const string IsDeductible05PaidByInsurer = "IsDeductible05PaidByInsurer";
        public const string REOPENING_OF_CLAIM_NOT_ALLOWED = "REOPENING_OF_CLAIM_NOT_ALLOWED";
        public const string CLAIMS_MIGRATION_STATUS = "C";
        // ClaimWakeUp Processing
        public const string CLOSED_STATUS = "CCL";  
        public const string CLOSED_STATUS_REPORT_ONLY = "CRL";
        public const string MIGRATED_CLOSED_CLAIM_BEING_PROCESSED = "P01";
        public const string FAILED_DUE_TO_INTERNAL_SERVICE_CONNECTION_ISSUES = "F00";
        public const string FAILED_POLICY_DOES_NOT_EXIST_IN_GENIUSX = "F01";  
        public const string FAILED_POLICY_EXISTS_BUT_COULD_NOT_ATTACH_TO_CLAIM = "F02";
        public const string REOPENING_OF_UNPROCESSED_CLAIM_NOT_ALLOWED = "REOPENING_OF_UNPROCESSED_CLAIM_NOT_ALLOWED";
        public const string REOPENING_OF_A_CLAIM_ALREADY_BEING_PROCESSED_NOT_ALLOWED = "REOPENING_OF_A_CLAIM_ALREADY_BEING_PROCESSED_NOT_ALLOWED";
        public const string REOPENING_OF_A_CLAIM_THAT_FAILED_PROCESSING_NOT_ALLOWED = "REOPENING_OF_A_CLAIM_THAT_FAILED_PROCESSING_NOT_ALLOWED";
        public const string REOPENING_OF_CLAIM_NOT_ALLOWED_WITHOUT_POLICY = "REOPENING_OF_CLAIM_NOT_ALLOWED_WITHOUT_POLICY";
        public const string REOPENING_OF_CLAIM_NOT_ALLOWED_POLICY_FAILURE = "REOPENING_OF_CLAIM_NOT_ALLOWED_POLICY_FAILURE";
        public const string FAILED_TO_INITIATE_MIGRATION_PROCESSING = "FAILED_TO_INITIATE_MIGRATION_PROCESSING";  
        // ClaimWakeUp Processing Ends

        public const string APP_SETTING_KEY_HEADERSTATUSESFORINVALIDCLAIMTRANSFER = "HeaderStatusesForInvalidClaimTransfer";
        public const string INACT_EVENT_DESC = "Estimate Reviewed - No Change";
        public const string REC_INACT_EVENT_DESC = "Recovery Estimate Reviewed - No Change";
        public const string CLAIM_HEADER_STATUS_ABANDONED = "CAB";
        public const string CLAIM_HEADER_STATUS_NO_ESTIMATE = "COU";
        public const string CLAIM_HEADER_STATUS_ESTIMATE_MADE = "CES";
        public const string CLAIM_HEADER_STATUS_PAYMENT_MADE = "CPY";
        public const string EVENT_PRIORITY_REC = "REC";
        public const string EVENT_CLAIM_CREATED_TYPECODE = "CRTCLM";
        public const string EVENT_INACTIVITY_REVIEW_TYPECODE = "INACT";
        public const string EVENT_PAYMENT_CANCELLATION_TYPECODE = "PAYCL";
        public const string EVENT_PHONE_TYPECODE = "PHONE";
        public const string EVENT_POST_TYPECODE = "POST";
        public const string EVENT_REPORT_REVIEW_TYPECODE = "REPORT";
        public const string EVENT_REVIEW_TYPECODE = "REVIEW";
        public const string EVENT_RECOVERY_INACTIVITY_REVIEW_TYPECODE = "RINACT";
        public const string EVENT_REOPEN_CLAIM_TYPECODE = "ROCLM";
        public const string NAME_INVOLVEMENT_MAINTENANCE_STATUS = "NameInvolvementMaintenanceStatus";
        public const string MAJOR_INSURED_TITLE = "Client";
        public const string AT_LEAST_ONE_UNATTACHED_CLAIM_DETAIL_MUST_EXISTS="At_Least_One_Unattached_Claim_Detail_Must_Exists";

        public const string POLICYNOTVERIFIED_CLAIMTRANSACTION_NOTALLOWED = "PolicyNotVerified_ClaimTransaction_NotAllowed";
        public const string POLICYNOTVERIFIED_COVERAGEVERIFICATION_NOTALLOWED = "POLICYNOTVERIFIED_COVERAGEVERIFICATION_NOTALLOWED";
        public const string TYPE_OF_LOSS_AND_COVERAGE_TYPE_DECISION_TABLE_CODE = "CLMTOLCTV";
        public const string POLICY_ATTACHMENT_NOT_ALLOWED = "POLICY_ATTACHMENT_NOT_ALLOWED";
        public const string VEHICLE_TYPE_AND_GENIUS_VEHICLE_TYPE_MISMATCH = "VEHICLE_TYPE_AND_GENIUS_VEHICLE_TYPE_MISMATCH";
        public const string TYPE_OF_LOSS_AND_COVERAGE_TYPE_MISMATCH = "TYPE_OF_LOSS_AND_COVERAGE_TYPE_MISMATCH";
    }
}
