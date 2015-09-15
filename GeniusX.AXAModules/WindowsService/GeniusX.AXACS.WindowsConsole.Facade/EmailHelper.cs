using System;
using System.Net.Mail;
using System.Security.Principal;
using log4net;
using Xiap.Framework.Logging;

namespace GeniusX.AXACS.WindowsConsole.Facade
{
    public class EmailHelper
    {
        private static readonly ILogger logger = LoggerFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);        

        public string SMTPServer { get; set; }
        public int SMTPPort { get; set; }
        public string EmailRecipients { get; set; }
        public string EmailSender { get; set; }
  
       
        /// <summary>
        /// This method send email with an attached document.
        /// </summary>
        /// <param name="errorMessage">error Message</param>
        /// <param name="attachmentlink">attachment link</param>
        /// <param name="transactionStatus">transaction Status</param>
        /// <param name="fileProcessed">file Processed</param>
        public void SendEmail(string errorMessage, string attachmentlink, string transactionStatus, string fileProcessed)
        {
                try
                {
                    if (logger.IsInfoEnabled)
                    {
                        logger.Info(Constants.SENDING_EMAIL);
                    }

                    string errorSubject = null;
                    string errorBody = null;
                    string emailbody = null;
                    if (!string.IsNullOrEmpty(errorMessage))
                    {
                        if (string.Equals(transactionStatus, Constants.TRANSACTIONSTATUSSUCCESS))
                        {
                            errorSubject = fileProcessed + Constants.ADD_TO_EML_SUBJCT_SUCCESS;
                            errorBody = fileProcessed + Constants.ADD_TO_EML_BODY_SUCCESS;
                        }
                        else
                        {
                            errorSubject = fileProcessed + Constants.ADD_TO_EML_SUBJCT_FAIL;
                            errorBody = string.Format(Constants.ADD_TO_EML_BODY_FAIL, fileProcessed);
                        }

                        emailbody = errorBody + "\r\n" + errorMessage;
                    }

                    MailMessage mail = new MailMessage(this.EmailSender, this.EmailRecipients, errorSubject, emailbody);
                    Attachment attachment = new Attachment(attachmentlink);

                    mail.Attachments.Add(attachment);
                    SmtpClient smtpClient = new SmtpClient(this.SMTPServer, this.SMTPPort);
                    smtpClient.UseDefaultCredentials = true;
                    smtpClient.Send(mail);
                    if (logger.IsInfoEnabled)
                    {
                        logger.Info(string.Format(Constants.INFOMESSAGE_EMAIL_SENT_FOR, fileProcessed));
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                    throw ex;
                }
            }
        }
    }
