using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using log4net;
using log4net.Config;


namespace GeniusX.AXACS.WindowsConsole.Facade
{    
    public class ProcessChequeSynchronisation
    {
         private static readonly ILog logger = LogManager.GetLogger(typeof(ProcessChequeSynchronisation));
         private GeniusXHelper geniusXHelper;
         private GeniusHelper geniusHelper;
         static ProcessChequeSynchronisation()
         {
             DOMConfigurator.Configure();
         }
                
        public string GeniusXConnectionString { get; set; }                
        public string MaxBatchSize { get; set; }

        public string GeniusConnectionString { get; set; }        
        public string GeniusQuery { get; set; }
        public string GeniusEncryptedPassword { get; set; }
        public string GeniusSchema { get; set; }
        public int CommandTimeout { get; set; }
        

        public string DoSendEmail { get; set; }
        public string SMTPServer { get; set; }
        public string SMTPPort { get; set; }
        public string EmailRecipients { get; set; }
        public string EmailSender { get; set; }
        public string SMTPDomain { get; set; }
        public string SMTPUser { get; set; }
        public string SMTPEncryptedPassword { get; set; }

      
      
        public void InitiateTransaction()
        {          
            try
            {
                if (logger.IsDebugEnabled)
                {
                    logger.Debug("Begin-InitiateTransaction()");
                }

                this.geniusXHelper = new GeniusXHelper();
                this.geniusXHelper.GeniusXConnectionString = this.GeniusXConnectionString;                

                List<long> paymentReferenceIDs = this.geniusXHelper.GetPaymentRequestsWithNoChequeOrBACSRef();

                List<string> toBeUpdate = this.GetListTobeUpdate(paymentReferenceIDs);

                this.geniusHelper = new GeniusHelper();
                this.geniusHelper.GeniusConnectionString = this.GeniusConnectionString;
                this.geniusHelper.GeniusQuery = this.GeniusQuery;
                this.geniusHelper.GeniusEncryptedPassword = this.GeniusEncryptedPassword;
                this.geniusHelper.GeniusSchema = this.GeniusSchema;
                this.geniusHelper.CommandTimeout = this.CommandTimeout;


                foreach (string paymentRef in toBeUpdate)
                {
                    long id = this.ProceedToUpdate(paymentRef);
                    if (id > 0)
                    {
                        this.HandleErrorAndSendMail(id, string.Empty);
                    }
                }

                if (logger.IsDebugEnabled)
                {
                    logger.Debug("End-InitiateTransaction");
                }
            }
            catch (Exception ex)
            {
                logger.Error(string.Format("InitiateTransaction - {0}", ex.Message));
                this.HandleErrorAndSendMail(0, ex.Message);
            }
        }    

        private long ProceedToUpdate(string paymentReferences)
        {            
            if (logger.IsDebugEnabled)
            {
                logger.Debug(string.Format("ProceedToUpdate({0})", paymentReferences));
            }

            long batchControlLogId = 0;

            try
            {
                List<PaymentDetail> paymentDetail = this.geniusHelper.GetGeniusCheques(paymentReferences);

                if (paymentDetail != null && paymentReferences.Count() > 0)
                {
                    XElement xml = this.BuildXml(paymentDetail);
                    batchControlLogId = this.geniusXHelper.UpdatePaymentRequest(xml);                    
                }
            }
            catch (Exception ex)
            {
                logger.Error(string.Format("ProceedToUpdate - {0}", ex.Message));
                throw ex;
            }

            if (logger.IsDebugEnabled)
            {
                logger.Debug(string.Format("ProceedToUpdate({0}) => returns: {1}", paymentReferences, batchControlLogId));
            }

            return batchControlLogId;
        }      

        private XElement BuildXml(List<PaymentDetail> paymentDetail)
        {
            XElement xml = new XElement("PRS",
                               from p in paymentDetail
                               select new XElement("PR",
                                           new XElement("ID", p.PaymentRequestID),
                                           new XElement("Cheque", p.ChequeNumber)));
                                           
            return xml;  
        }

        private List<string> GetListTobeUpdate(List<long> paymentReferenceIDs)
        {
            List<string> toBeUpdate = new List<string>();
            Int32 count = 0;
            string paymentReferences = null;
            ArgumentCheck.ArgumentNullOrEmptyCheck(this.MaxBatchSize, Constants.ERRORMESSAGE_MAX_BATCH_SIZE);
            foreach (long paymentReferenceID in paymentReferenceIDs)
            {
                paymentReferences += paymentReferenceID + ",";
                count++;
                if (count == Convert.ToInt32(this.MaxBatchSize))
                {
                    toBeUpdate.Add(paymentReferences.Trim(','));
                    paymentReferences = string.Empty;
                    count = 0;
                }              
            }

            if (!string.IsNullOrEmpty(paymentReferences))
            {
                toBeUpdate.Add(paymentReferences.Trim(','));
            }

            return toBeUpdate;
        }

        private void HandleErrorAndSendMail(long errorBatchID, string errorMessage)
        {
            if (logger.IsDebugEnabled)
            {
                logger.Debug(string.Format("HandleErrorAndSendMail({0})", errorBatchID));
            }

            try
            {
                EmailHelper emailInteractions = new EmailHelper();
                emailInteractions.DoSendEmail = this.DoSendEmail == null || this.DoSendEmail.Length <= 0 ? false : Convert.ToBoolean(this.DoSendEmail);
                emailInteractions.EmailRecipients = this.EmailRecipients;
                emailInteractions.EmailSender = this.EmailSender;

                emailInteractions.SMTPDomain = this.SMTPDomain;
                emailInteractions.SMTPUser = this.SMTPUser;
                emailInteractions.SMTPEncryptedPassword = this.SMTPEncryptedPassword;
                emailInteractions.SMTPPort = this.SMTPPort == null ? 0 : Convert.ToInt16(this.SMTPPort);
                emailInteractions.SMTPServer = this.SMTPServer;
                emailInteractions.SendEmail(errorBatchID, errorMessage);
            }
            catch (Exception ex)
            {
                logger.Error(string.Format("HandleErrorAndSendMail - {0}", ex.Message));
            }
        }
    }
}
