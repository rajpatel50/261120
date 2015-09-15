
namespace GeniusX.AXACS.WindowsConsole.Facade
{
    public class Constants
    {                  
        public const string ERRORMESSAGE_FolderPath = "FolderPath is not configured.";
        public const string ERRORMESSAGE_ArchivePath = "ArchivePath is not configured.";
        public const string ERRORMESSAGE_LogFilePath = "LogFilePath is not configured.";
        public const string ERRORMESSAGE_EmailRecipients = "EmailRecipient/EmailRecipients not configured.";        
        public const string ERRORMESSAGE_EmailSender = "EmailSender is not configured.";
        public const string ERRORMESSAGE_SMTPSERVER = "SMTP server is not configured.";
        public const string ERRORMESSAGE_SMTPPORT = "SMTP port is not configured.";
        public const string ERRORMESSAGE_DOSENDEMAIL = "DoSendEmail is not configured";
        public const string ERRORMESSAGE_CLAIMTRANSACTION_NOT_FOUND = "Row {0}: Payment {1} update failed - the Payment could not be found.";
        public const string ERRORMESSAGE_PAYMENTISSUEDATEINVALID = "Row {0}: Payment {1} update failed - the Payment Issued Date is not valid.";
        public const string ERRORMESSAGE_AMOUNT_INVALID = "Row {0}: Payment {1} update failed - the Payment Amount is not valid.";
        public const string ERRORMESSAGE_NOT_ENOUGH_DATA = "Row {0}: Payment update failed - the row does not contain enough data.";
        public const string INFOMESSAGE_PROCESSING_COMPLETE = "ProcessFile completed for File {0}";
        public const string INFOMESSAGE_PROCESSING_STARTED = "File watching for Folder {0} started.";
        public const string ERRORMESSAGE_DIRECTORY_NOT_FOUND = "Directory Path - {0} - does not exists or is unaccessible. Please contact the administrator.";
        public const string INFOMESSAGE_PROCESSING_CALLED = "ProcessFile called for File {0}.";
        public const string INFOMESSAGE_ARCHIVE_FILE_COPIED_AND_REPLACED = "Log File {0} copied and replaced.";
        public const string INFOMESSAGE_PROCESSING_ROWS = "Processing Rows.";
        public const string INFOMESSAGE_EMAIL_SENT_FOR = "Email sent for : {0}.";
        public const string SENDING_EMAIL = "Sending Email";
        public const string ADD_TO_EML_BODY_SUCCESS = " has been processed successfully and summary is as below :";
        public const string ADD_TO_EML_SUBJCT_SUCCESS = ": Processed Successfully";
        public const string ADD_TO_EML_SUBJCT_FAIL = " : Processing Failed";
        public const string ADD_TO_EML_BODY_FAIL = "Processing of {0} failed and summary is as below:";
        public const string TRANSACTIONSTATUSSUCCESS = "Successful";
        public const string TRANSACTIONSTATUSFAIL = "Failed";
        public const string INFO_MESSAGE_PROCESS_ROWS_COMPLETE = "Processed Rows";
        public const string DateFormat = "ddMMyyyy";

        public const string ERRORMESSAGE_CONNECTIONSTRING = "Connection String is not configured.";
        public const string CTH_QUERY = @"(SELECT CTTPRL.[PaymentRequestID], PR.[CustomReference01]
                                                                   from [Claims].[ClaimTransactionHeader] CTH 
                                                                   join [Claims].[ClaimTransactionToPaymentRequestLink] CTTPRL 
                                                                   on CTH.[ClaimTransactionHeaderID] = CTTPRL.[ClaimTransactionHeaderID] 
                                                                   join [Acc].[PaymentRequest] PR 
                                                                   on PR.[PaymentRequestID] = CTTPRL.[PaymentRequestID] 
                                                                   where CTH.[ClaimTransactionHeaderReference] = '{0}')";
        public const string PRQST_QUERY = @"UPDATE [Acc].[PaymentRequest] SET CustomReference01 = @ChequeNo, CustomDate01 = @PaymentIssuedDate where PaymentRequestID = '{0}'";

        internal const string ERRORMSG_CHEQUE_EXISTS = "Row {0}: Payment {1} update failed - the Cheque Number is already populated.";
        internal const string ERRORMSG_NOTUPDATED = "Errors encountered during processing - no updates performed.";
        internal const string STATUS_MESSAGE = "All {0} rows processed successfully. Total payments: {1}.";
        internal const string PAYMENTFOUND = "Row {0}: Payment {1} found. Cheque Number {2}, Payment Issued Date {3}.";
        
        public const string FolderPath = "FolderPath";
        public const string ArchivePath = "ArchivePath";
        public const string LogFilePath = "LogFilePath";        
        public const string SMTPServer = "SMTPServer";
        public const string SMTPPort = "SMTPPort";
        public const string EmailRecipients = "EmailRecipients";
        public const string EmailSender = "EmailSender";
        public const string ConnectionString = "config";
        public const string DoSendEmails = "DoSendEmail";
        public const string NumberOfFileAccessRetries = "NumberOfFileAccessRetries";
        public const string DurationOfSleepForFileAccessRetries = "DurationOfSleepForFileAccessRetries";
    }
}

