using System;
using System.Collections.Generic;
using IBM.Data.DB2.iSeries;
using log4net;

namespace GeniusX.AXACS.WindowsConsole.Facade
{
   public class GeniusHelper
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(GeniusHelper));
        private const string SCHEMA = "[Schema]";
        public string GeniusConnectionString { get; set; }
        public string GeniusQuery { get; set; }
        public string GeniusEncryptedPassword { get; set; }
        public string GeniusSchema { get; set; }
        public int CommandTimeout { get; set; }
        
        public List<PaymentDetail> GetGeniusCheques(string paymentIDs)
        {
            if (logger.IsDebugEnabled)
            {
                logger.Debug(string.Format("GetGeniusCheques({0})", paymentIDs));
            }

            this.BuildConnectionString();

            List<PaymentDetail> paymentDetails = null;

            ArgumentCheck.ArgumentNullOrEmptyCheck(this.GeniusConnectionString, Constants.ERRORMESSAGE_GENIUS_CONNECTIONSTRING);
            ArgumentCheck.ArgumentNullOrEmptyCheck(this.GeniusQuery, Constants.ERRORMESSAGE_GENIUS_QUERY);
                
            try
            {
                using (iDB2Connection conn = new iDB2Connection(this.GeniusConnectionString))
                {
                    string selectSql = String.Format(this.GeniusQuery, this.GeniusSchema, paymentIDs);
                    using (iDB2Command cmd = new iDB2Command(selectSql, conn))
                    {
                        if (logger.IsDebugEnabled)
                        {
                            logger.Debug(string.Format("Genius Sql:", selectSql));
                        }

                        iDB2DataReader reader = null;
                        try
                        {
                            cmd.CommandTimeout = this.CommandTimeout ;
                            conn.Open();
                            if (logger.IsDebugEnabled)
                            {
                                logger.Debug("Connection opened");
                            }

                            reader = cmd.ExecuteReader();

                            while (reader.Read())
                            {
                                PaymentDetail paymentDetail = new PaymentDetail();
                                paymentDetail.PaymentRequestID = reader.GetInt64(0);
                                paymentDetail.ChequeNumber = reader.GetString(1).TrimEnd();
                                if (paymentDetails == null)
                                {
                                    paymentDetails = new List<PaymentDetail>();
                                }

                                paymentDetails.Add(paymentDetail);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.Error(string.Format("GetGeniusCheques - {0}", ex.Message));
                            throw ex;
                        }
                        finally
                        {
                            if (reader != null)
                            {
                                reader.Close();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(string.Format("GetGeniusCheques - {0}", ex.Message));
                throw ex;
            }

            if (logger.IsDebugEnabled)
            {
                logger.Debug(string.Format("AddErrorLogHeader({0}) => returns: paymnet details",paymentIDs));
            }

            return paymentDetails;            
        }

        private void BuildConnectionString()
        {
            if (logger.IsDebugEnabled)
            {
                logger.Debug("BuildConnectionString");
            }

            if (!string.IsNullOrEmpty(this.GeniusEncryptedPassword))
            {
                string password = Decryptor.DecryptPassword(this.GeniusEncryptedPassword);
                int passwordPosition = this.GeniusConnectionString.IndexOf(";password=");
                if (passwordPosition > 0)
                {
                    int passwordEndPosition = this.GeniusConnectionString.IndexOf(';', passwordPosition + 1);
                    this.GeniusConnectionString = this.GeniusConnectionString.Substring(0, passwordPosition) + ";password=" + password + this.GeniusConnectionString.Substring(passwordEndPosition);
                }
                else
                {
                    this.GeniusConnectionString = this.GeniusConnectionString + ";password=" + password + ";";
                }
            }
        }
    }
}
