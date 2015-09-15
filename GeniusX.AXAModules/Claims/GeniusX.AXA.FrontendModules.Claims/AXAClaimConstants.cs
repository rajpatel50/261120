
namespace GeniusX.AXA.FrontendModules.Claims
{
    internal static class AXAClaimConstants
    {
        public const string NAMEUSAGETYPECODE_MAJORCLAIMANT = "UCL";
        public const string NAMEUSAGETYPECODE_LOSSBROKER = "BRK";
        public const string NAMEUSAGETYPECODE_MAINCLAIMHANDLER = "CMH";
        public const string NAMEUSAGETYPECODE_MAJORINSURED = "UIN";

        public const string CLAIM_STATUS_CLAIM_OPENED_UNCONFIRMED = "CON";
        public const string CLAIM_STATUS_CLAIM_OPENED = "COU";

        public const string LIABILITY_CLAIM_PRODUCT = "CGBIPC";
        public const string MOTOR_CLAIM_PRODUCT = "CGBIMO";
        public const string CLAIMREFERENCEPREFIX_DECISIONTABLE = "CLMPREFIX";
        public const string EmptyGroupCode = "XEMPTY";
        public const string DefaultBulkEventType = "POST";

        public const string CLAIMDETAILTYPE_AD = "AD";
        public const string CLAIMDETAILTYPE_TPVD = "TPVD";
        public const string CLAIMDETAILTYPE_TPPD = "TPPD";
        public const string CLAIMDETAILTYPE_TPI = "TPI";
        public const string CLAIMDETAILTYPE_LIA = "LIA";

        public const string LITIGATIONTYPE_LIT = "LIT";
        public const string LITIGATIONTYPE_OTH = "OTH";


        public const string AUTHENTICATION_TASK_NOTIFICAION = "PaymentAuthenticationTaskNotificaion";
        public const string AUTHENTICATION_TASK_NOTIFICATION_CHECKINTERVAL = "CheckInterval";
        public const string AUTHENTICATION_TASK_NOTIFICATION_ATTEMPTS = "NotificationAttempts";
        public const string REPORT_ONLY_NO_ESTIMATE_STATUS = "CRO";
        public const string REPORT_ONLY_ESTIMATE_MADE_STATUS = "CRE";
        public const string CONST_VARIOUS = "Various";

        public const string POLICY_VERIFIED_HEADERSTATUS = "PolicyVerifiedHeaderStatus";

       public const string CUSTOM_MESSAGE_ALIAS = "AXA_CUSTOM_MESSAGE";
       public const string CUSTOM_MANDATORY_FIELD_MESSAGE_ID = "2192";
      
       public const string CLAIM_HEADER_ANALYSIS_CODE09 = "ClaimHeaderAnalysisCode09";
       public const string INACT_EVENTTYPE_CODE = "INACT";
       public const string REC_INACT_EVENTTYPE_CODE = "RINACT";
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
    }
}
