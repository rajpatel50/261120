using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Xml.Linq;
using log4net;

namespace GeniusX.AXACS.WindowsConsole.Facade
{
    public class GeniusXHelper
    {              
        private static readonly ILog logger = LogManager.GetLogger(typeof(GeniusXHelper));
        public string GeniusXConnectionString { get; set; } 

        public List<long> GetPaymentRequestsWithNoChequeOrBACSRef()
        {
            ArgumentCheck.ArgumentNullOrEmptyCheck(this.GeniusXConnectionString, Constants.ERRORMESSAGE_GENIUSX_CONNECTIONSTRING);
            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(this.GeniusXConnectionString))
                {
                    string selectString = "SELECT PR.PaymentRequestId FROM Acc.PaymentRequest PR INNER JOIN claims.ClaimTransactionToPaymentRequestLink CTPRL ON CTPRL.PaymentRequestId = PR.PaymentRequestId where PR.CustomReference01 is Null";

                    SqlCommand mySqlCommand = sqlConnection.CreateCommand();
                    mySqlCommand.CommandText = selectString;
                    SqlDataAdapter mySqlDataAdapter = new SqlDataAdapter();
                    mySqlDataAdapter.SelectCommand = mySqlCommand;
                    DataSet myDataSet = new DataSet();
                    sqlConnection.Open();
                    mySqlDataAdapter.Fill(myDataSet, "PaymentRequest");
                    DataTable myDataTable = myDataSet.Tables["PaymentRequest"];
                    List<long> returnval = new List<long>();
                    foreach (DataRow row in myDataTable.Rows)
                    {
                        returnval.Add(Convert.ToInt64(row[0].ToString()));
                    }

                    sqlConnection.Close();
                    return returnval;
                }
            }
            catch (Exception ex)
            {
                logger.Error(string.Format("GetPaymentRequestsWithNoChequeOrBACSRef - {0}", ex.Message));
                throw ex;
            }
        }

        public long UpdatePaymentRequest(XElement xml)
        {
            if (logger.IsDebugEnabled)
            {
                logger.Debug(string.Format("UpdatePaymentRequest {0}", xml));
            }

            long batchControlLogId = 0;

            using (SqlConnection conn = new SqlConnection(this.GeniusXConnectionString))
            {
                conn.Open();

                // insert record in BatchControllog (Status = In Progress)
                batchControlLogId = this.AddBatchEntry(BatchControlConstant.BatchType.Custom, "Description", xml.ToString(), conn);

                try
                {
                    // update Paymenet request
                    using (SqlCommand cmd = new SqlCommand("UpdatePaymentRequestAXA", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add("@PaymentRequests", SqlDbType.Xml).Value = xml.ToString();                        
                        cmd.ExecuteScalar();
                    }

                    // update BatchControlLog (Status = completed, CompletionStatus = Clear)
                    this.UpdateBatchStatus(batchControlLogId, BatchControlConstant.BatchStatus.Completed, BatchControlConstant.CompletionStatus.Clear, conn);
                }
                catch (Exception ex)
                {
                    // insert record into BatchErrorLogHeader                    
                    // add error detail into BatchErrorLogDetail
                    this.AddErrorLog(batchControlLogId, BatchControlConstant.MessageLevel.Error, ex.Message, string.Empty, conn);

                    // update BatchControlLog (Status = completed, CompletionStatus= Errors)
                    this.UpdateBatchStatus(batchControlLogId, BatchControlConstant.BatchStatus.Completed, BatchControlConstant.CompletionStatus.Errors, conn);                    

                    logger.Error(string.Format("UpdatePaymentRequest - {0}", ex.Message));

                    return batchControlLogId;
                }
            }

            if (logger.IsDebugEnabled)
            {
                logger.Debug(string.Format("UpdatePaymentRequest {0} => returns: {1}", xml,batchControlLogId));
            }

            return 0;
        }

        private long AddErrorLog(long batchControlLogId, BatchControlConstant.MessageLevel messageLevel, string message, string messageAlias, SqlConnection conn)
        {
            if (logger.IsDebugEnabled)
            {
                logger.Debug(string.Format("AddErrorLog({0}, {1}, {2}, {3})", batchControlLogId, messageLevel, message, messageAlias));
            }

            long id = 0;

            message = message.Substring(0, Math.Min(255, message.Length));


            using (SqlCommand cmd = new SqlCommand("AddErrorLog", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@batchControlLogId", batchControlLogId);
                cmd.Parameters.AddWithValue("@messageLevel", messageLevel);
                cmd.Parameters.AddWithValue("@message", message);
                cmd.Parameters.AddWithValue("@messageAlias", messageAlias);
                id = (Int32)cmd.ExecuteScalar();
            }

            if (logger.IsDebugEnabled)
            {
                logger.Debug(string.Format("AddErrorLog({0}, {1}, {2}, {3}) => returns {4}", batchControlLogId, messageLevel, message, messageAlias, id));
            }

            return id;
        }

        private long AddBatchEntry(BatchControlConstant.BatchType batchType, string description, string batchParameters, SqlConnection conn)
        {
            if (logger.IsDebugEnabled)
            {
                logger.Debug(string.Format("AddBatchEntry( {0}, {1}, {2}, {3} )", batchType,description,batchParameters,conn));
            }

            long batchControlLogId = 0;

            string batchParams = batchParameters;
            if (batchParameters.Length > 255)
            {
                batchParams = batchParameters.Substring(0, 254);
            }

            using (SqlCommand cmd = new SqlCommand(BatchControlConstant.ADD_BATCH_ENTRY, conn))
            {
                cmd.Parameters.AddWithValue("@batchType", batchType);
                cmd.Parameters.AddWithValue("@batchStatus", BatchControlConstant.BatchStatus.InProgress);
                cmd.Parameters.AddWithValue("@batchParameters", batchParams != null ? (object)batchParams : System.DBNull.Value);
                cmd.Parameters.AddWithValue("@runDescription", description != null ? (object)description : System.DBNull.Value);
                batchControlLogId = (Int32)cmd.ExecuteScalar();                
            }

            if (logger.IsDebugEnabled)
            {
                logger.Debug(string.Format("AddBatchEntry({0}, {1}) => returns: {2}", batchType, batchParameters, batchControlLogId));
            }

            return batchControlLogId;
        }

        private void UpdateBatchStatus(long batchControlLogId, BatchControlConstant.BatchStatus batchStatus, BatchControlConstant.CompletionStatus? completionStatus, SqlConnection conn)
        {
            if (logger.IsDebugEnabled)
            {
                logger.Debug(string.Format("UpdateBatchStatus( {0}, {1}, {2} )", batchControlLogId, batchStatus, completionStatus));
            }

            string sql;
            switch (batchStatus)
            {
                case BatchControlConstant.BatchStatus.InProgress: sql = BatchControlConstant.UPDATE_BATCH_STATUS_RUN_START;
                    break;
                case BatchControlConstant.BatchStatus.Completed: sql = BatchControlConstant.UPDATE_BATCH_STATUS_RUN_END;
                    break;
                default: sql = BatchControlConstant.UPDATE_BATCH_STATUS;
                    break;
            }

            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@batchControlLogId", batchControlLogId);
                cmd.Parameters.AddWithValue("@batchStatus", batchStatus);
                if (batchStatus == BatchControlConstant.BatchStatus.InProgress || batchStatus == BatchControlConstant.BatchStatus.Completed)
                {
                    cmd.Parameters.AddWithValue("@runTime", DateTime.Now);
                }

                cmd.Parameters.AddWithValue("@completionStatus", completionStatus != null ? (object)completionStatus : DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }      
    }
}
