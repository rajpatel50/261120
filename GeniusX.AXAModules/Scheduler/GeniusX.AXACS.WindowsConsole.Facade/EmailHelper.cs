using System;
using System.Net.Mail;
using System.Security.Principal;
using log4net;

namespace GeniusX.AXACS.WindowsConsole.Facade
{
    public class EmailHelper
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(GeniusXHelper));

        public bool DoSendEmail { get; set; }
        public string SMTPServer { get; set; }
        public int SMTPPort { get; set; }
        public string EmailRecipients { get; set; }

        public string EmailSender { get; set; }
        public string SMTPDomain { get; set; }
        public string SMTPUser { get; set; }
        public string SMTPEncryptedPassword { get; set; }

        public void SendEmail(long batchControlLogID, string errorMessage)
        {
            if (logger.IsDebugEnabled)
            {
                logger.Debug(string.Format("SendEmail({0},{1}", batchControlLogID, errorMessage));
            }

            if (!this.DoSendEmail)
            {
                return;
            }

            ArgumentCheck.ArgumentNullOrEmptyCheck(this.EmailRecipients, Constants.ERRORMESSAGE_EMAILRECIPIENTS);
            ArgumentCheck.ArgumentNullOrEmptyCheck(this.EmailSender, Constants.ERRORMESSAGE_EMAILSENDER);

            ////ArgumentCheck.ArgumentNullOrEmptyCheck(this.SMTPDomain, Constants.ERRORMESSAGE_SMTPDOMAIN);
            ////ArgumentCheck.ArgumentNullOrEmptyCheck(this.SMTPUser, Constants.ERRORMESSAGE_SMTPUSER);            
            ArgumentCheck.ArgumentNullOrEmptyCheck(this.SMTPPort, Constants.ERRORMESSAGE_SMTPPORT);
            ArgumentCheck.ArgumentNullOrEmptyCheck(this.SMTPServer, Constants.ERRORMESSAGE_SMTPSERVER);

            try
            {
                string errorSubject = null;
                string errorBody = null;
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    errorSubject = "Error Processing Payments ";
                    errorBody = errorMessage;
                }
                else if (batchControlLogID > 0)
                {
                    errorSubject = "Error Processing Payments, Batch ID: " + batchControlLogID.ToString();
                    errorBody = errorSubject;
                }

                MailMessage mail = new MailMessage(this.EmailSender, this.EmailRecipients, errorSubject, errorBody);

                SmtpClient smtpClient = new SmtpClient(this.SMTPServer, this.SMTPPort);
                smtpClient.UseDefaultCredentials = true;
                if (string.IsNullOrEmpty(this.SMTPEncryptedPassword) || string.IsNullOrEmpty(this.SMTPDomain) || string.IsNullOrEmpty(this.SMTPUser))
                {                   
                    smtpClient.Send(mail);
                }
                else
                {                    
                    string password = this.SMTPEncryptedPassword; // Decryptor.DecryptPassword(this.SMTPEncryptedPassword);
                    ImpersonateUser impersonateUser = new ImpersonateUser();
                    WindowsImpersonationContext impersonatedUser = impersonateUser.SetImpersonatedUser(this.SMTPUser, this.SMTPDomain, password);
                    {
                        smtpClient.Send(mail);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(string.Format("SendEmail - {0}", ex.Message));                
            }
        }
    }
}
