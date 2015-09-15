using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using Xiap.Framework.Logging;

namespace GeniusX.AXACS.WindowsConsole.Facade
{
    public class ProcessFile
    {
        private static readonly ILogger _Logger = LoggerFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);      
        private FileWatcherServiceManager serviceManager;

        private List<string> str = new List<string>();       

        /// <summary>
        /// construtor of the class
        /// </summary>
        /// <param name="serviceManager">Object of FileWatcherServiceManager loaded with data</param>
        public  ProcessFile(FileWatcherServiceManager serviceManager)
        {
            this.serviceManager = serviceManager;
        }

        /// <summary>
        /// Method used to watch the folder Drop.
        /// </summary>
        public void WatchFolder()
        {
            try
            {
                string directoryPath = this.serviceManager.FolderPath;
                if (_Logger.IsInfoEnabled)
                {
                    _Logger.Info(string.Format(Constants.INFOMESSAGE_PROCESSING_STARTED, directoryPath));
                }

                if (Directory.Exists(directoryPath))
                {
                    FileSystemWatcher watcher = new FileSystemWatcher();
                    watcher.Path = directoryPath;
                    watcher.NotifyFilter = NotifyFilters.CreationTime |
                    NotifyFilters.FileName | NotifyFilters.LastWrite;
                    watcher.Filter = "*.txt";
                    watcher.Created += new FileSystemEventHandler(this.ProcessFiles);
                    watcher.EnableRaisingEvents = true;

                    System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
                }
                else
                {
                    _Logger.Info(string.Format(Constants.ERRORMESSAGE_DIRECTORY_NOT_FOUND,directoryPath));
                }
            }
            catch (Exception e)
            {
                _Logger.Error(e);
            }
        }

        /// <summary>
        /// When a file comes into a folder, method will be called, process the file data
        /// </summary>
        /// <param name="sender">Object sender</param>
        /// <param name="e">File System EventArgs</param>
        private void ProcessFiles(object sender, FileSystemEventArgs e)
        {
            string logFileName = this.serviceManager.LogFilePath;
            string statusMessage = null;
            string trasactionStatus = null;
            if (_Logger.IsInfoEnabled)
            {
                _Logger.Info(String.Format(Constants.INFOMESSAGE_PROCESSING_CALLED, e.Name));
            }

            string  logFilePath = logFileName + "\\" + e.Name.Replace(".txt", string.Empty) + "_" + DateTime.Now.ToString("dd-MM-yyyy_hh-mm-ss tt") + ".txt";

            string archivePath = this.serviceManager.ArchivePath;
            int maxNoofRetries = this.serviceManager.NumberOfFileAccessRetries.HasValue ? this.serviceManager.NumberOfFileAccessRetries.Value : 3;
            int durationOfSleep = this.serviceManager.DurationOfSleepForFileAccessRetries.HasValue ? this.serviceManager.DurationOfSleepForFileAccessRetries.Value : 1000; 
            int numberOfRetires = 0;

            using (PerfLogger _LG = new PerfLogger(typeof(ProcessFile), "ProcessFile"))
            {
                while (true)
                {
                    try
                    {
                        if (_Logger.IsInfoEnabled)
                        {
                            _Logger.Info("Before File Stream");
                        }

                        this.ReadFileAndProcess(e, logFileName, logFilePath, out statusMessage, out trasactionStatus);
                        break;
                    }
                    catch (IOException exc)
                    {
                        numberOfRetires++;
                        if (numberOfRetires < maxNoofRetries)
                        {
                            Thread.Sleep(durationOfSleep);
                            if (_Logger.IsInfoEnabled)
                            {
                                _Logger.Info(string.Format("Unable to access File {0} retrying {1} time(s)", e.FullPath, numberOfRetires + 1));
                                _Logger.Info(exc.Message);
                                _Logger.Info(exc.StackTrace);
                            }

                            continue;
                        }

                        this.HandleError(e, trasactionStatus, exc, statusMessage, logFilePath, e.Name, archivePath);
                        break;
                    }
                    catch (Exception exc)
                    {
                        this.HandleError(e, trasactionStatus, exc, statusMessage, logFilePath, e.Name, archivePath);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Reads the file data and process it
        /// </summary>
        /// <param name="e"> file system event agrs</param>
        /// <param name="logFileName"> log file name</param>
        /// <param name="logFilePath"> log file path</param>
        /// <param name="statusMessage">transaction status message </param>
        /// <param name="trasactionStatus"> transaction status</param>
        private void ReadFileAndProcess(FileSystemEventArgs e,string logFileName, string logFilePath, out string statusMessage, out string trasactionStatus)
        {
            statusMessage = null;
            trasactionStatus = null;
            string processsedFileName = e.Name;
            if (_Logger.IsInfoEnabled)
            {
                _Logger.Info(String.Format(Constants.INFOMESSAGE_PROCESSING_CALLED, e.Name));
            }

            List<Tuple<string, string, string, double?, string, string, DateTime?>> readData = new List<Tuple<string, string, string, double?, string, string, DateTime?>>();
            string date = DateTime.Now.ToShortDateString();
            string dateTime = DateTime.Now.ToShortTimeString();
            
            string archivePath = this.serviceManager.ArchivePath;
            string line = null;
            double? amount = null;
            double amtFromFile; 
            DateTime formattedDate;
            DateTime? formatDate;

            using (FileStream fileStream = new FileStream(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                try
                {
                    if (_Logger.IsInfoEnabled)
                    {
                        _Logger.Info("File Stream Created");
                    }

                    using (System.IO.StreamReader file = new StreamReader(fileStream))
                    {
                        if (_Logger.IsInfoEnabled)
                        {
                            _Logger.Info("File Stream Read");
                        }

                        int i = 0;
                        //// Read a file line by line and separate it with comma and after reading adding the value in readData.
                        while (!string.IsNullOrEmpty(line = file.ReadLine()))
                        {
                            var items = line.Split(',');
                            i = i + 1;

                            ////check to determine if the row has enough data
                            if (items.Count() < 7)
                            {
                                throw new Exception(string.Format(Constants.ERRORMESSAGE_NOT_ENOUGH_DATA, i.ToString()));
                            }

                            ////check for claimtransactionheaderreference existance in the Curent row being procesed
                            if (string.IsNullOrEmpty(items[5].Trim()))
                            {
                                throw new Exception(string.Format(Constants.ERRORMESSAGE_CLAIMTRANSACTION_NOT_FOUND, i.ToString(), items[5]));
                            }

                            ////check it the payment amount is valid and non empty
                            if (!string.IsNullOrEmpty(items[3]) && double.TryParse(items[3], out amtFromFile))
                            {
                                amount = amtFromFile;
                            }
                            else
                            {
                                throw new Exception(string.Format(Constants.ERRORMESSAGE_AMOUNT_INVALID, i.ToString(), items[5]));
                            }
                            ////check for payment issue date
                            if (string.IsNullOrEmpty(items[6]))
                            {
                                formatDate = null;
                            }
                            else if (DateTime.TryParseExact(items[6], Constants.DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out formattedDate))
                            {
                                formatDate = formattedDate;
                            }
                            else
                            {
                                throw new Exception(string.Format(Constants.ERRORMESSAGE_PAYMENTISSUEDATEINVALID, i.ToString(), items[5]));
                            }

                            Tuple<string, string, string, double?, string, string, DateTime?> tuple = Tuple.Create(items[0], items[1], items[2], amount, items[4], items[5], formatDate);
                            readData.Add(tuple);
                        }
                    }


                    this.ProcessRows(readData, logFilePath, e.Name);

                    this.MoveFileToArchive(e.FullPath, archivePath, processsedFileName);

                    if (_Logger.IsInfoEnabled)
                    {
                        _Logger.Info(String.Format(Constants.INFOMESSAGE_PROCESSING_COMPLETE, e.Name));
                    }
                }
                catch (Exception exc)
                {
                    this.HandleError(e, trasactionStatus, exc, statusMessage, logFilePath, processsedFileName, archivePath);
                }
            }
        }

        /// <summary>
        /// Handles error raised during processing of files
        /// </summary>
        /// <param name="e">File System Event Args</param>
        /// <param name="trasactionStatus">trasaction Status</param>
        /// <param name="exc">Exception raised </param>
        /// <param name="statusMessage"> Transaction status message</param>
        /// <param name="logFilePath"> log file path</param>
        /// <param name="processsedFileName"> name of the file being processed</param>
        /// <param name="archivePath"> archive folder path</param>
        private void HandleError(FileSystemEventArgs e, string trasactionStatus, Exception exc, string statusMessage, string logFilePath, string processsedFileName, string archivePath)
        {
            trasactionStatus = Constants.TRANSACTIONSTATUSFAIL;
            _Logger.Error(exc.Message);
            _Logger.Error(exc);
            this.str.Add(string.Format(exc.Message));
            this.str.Add(Constants.ERRORMSG_NOTUPDATED);
            statusMessage = Constants.ERRORMSG_NOTUPDATED;
            this.CreateLogFile(this.str, logFilePath);
            if (this.serviceManager.DoSendEmails)
            {
                this.SendEmail(statusMessage, trasactionStatus, logFilePath, processsedFileName);
            }

            this.MoveFileToArchive(e.FullPath, archivePath, processsedFileName);
            this.str.Clear();
        }

        /// <summary>
        /// Move file from 'Drop' folder to 'Archive' folder.
        /// </summary>
        /// <param name="fullPath"> path of the file to be moved</param>
        /// <param name="targetPath">path where the file has to be moved</param>
        /// <param name="fileName"> file name</param>
        public void MoveFileToArchive(string fullPath ,string targetPath , string fileName)
        {
            try
            {
                if (_Logger.IsInfoEnabled)
                {
                    _Logger.Info("In MoveFileToArchive for " + fullPath);
                }

                string destinationPath = targetPath + "\\" + fileName;
                if (System.IO.File.Exists(destinationPath))
                {
                    if (_Logger.IsInfoEnabled)
                    {
                        _Logger.Info(string.Format(Constants.INFOMESSAGE_ARCHIVE_FILE_COPIED_AND_REPLACED, fileName));
                    }

                    System.IO.File.Delete(destinationPath);

                    if (_Logger.IsInfoEnabled)
                    {
                        _Logger.Info("File Deleted:" + destinationPath);
                    }
                }

                if (_Logger.IsInfoEnabled)
                {
                    _Logger.Info("Moving File to Archive");
                }

                System.IO.File.Move(fullPath, destinationPath);

                if (_Logger.IsInfoEnabled)
                {
                    _Logger.Info("File moved to Archive");
                }
            }
            catch (Exception ex)
             {
                 _Logger.Error(ex.Message);
                  throw ex;
             }
          }


        /// <summary>
        /// Processing the file by picking up row by row.        
        /// </summary>
        /// <param name="readData">Tuple of row from the file</param>
        /// <param name="logFilePath">Path of log file</param>
        /// <param name="processsedFileName">Processed file name</param>
        private void ProcessRows(List<Tuple<string, string, string, double?, string, string, DateTime?>> readData, string logFilePath, string processsedFileName)
        {
            if (_Logger.IsInfoEnabled)
            {
                _Logger.Info(string.Format(Constants.INFOMESSAGE_PROCESSING_ROWS));
            }

            string chequeNo = null;            
            string claimTransactionHeaderReference = null;
            double? paymentAmount = 0.0;
            DateTime? paymentIssuedDate;
            long? paymentRequestID = null;
            int rowCount = 0;
            SqlTransaction transaction = null;
            string trasactionStatus = null;
            string statusMessage = null;
            string chequeNumber = null;

            try
            {
                using (SqlConnection conn = new SqlConnection(this.serviceManager.ConnectionStrings))
                {
                    using (SqlCommand sqlCommand = new SqlCommand())
                    {
                        conn.Open();
                        transaction = conn.BeginTransaction("ProcessFileTransaction");
                        try
                        {
                            sqlCommand.Connection = conn;
                            sqlCommand.Transaction = transaction;
                            sqlCommand.CommandType = CommandType.Text;

                            foreach (var row in readData)
                            {
                                if (_Logger.IsInfoEnabled)
                                {
                                    ////TO DO : Print all row numbers being processed
                                    _Logger.Info("Processing Rows");
                                }

                                rowCount = rowCount + 1;
                                chequeNo = row.Item5;
                                claimTransactionHeaderReference = row.Item6;
                                paymentIssuedDate = row.Item7;

                                //// sql query to check whether chequeNo already exists in table for correnponding ClaimTransactionHeaderRefrence.
                                sqlCommand.CommandText = String.Format(Constants.CTH_QUERY, claimTransactionHeaderReference);
                                SqlDataReader rdr1 = sqlCommand.ExecuteReader();
                                if (rdr1.HasRows)
                                {
                                    while (rdr1.Read())
                                    {
                                        if (rdr1["PaymentRequestID"] != null)
                                        {
                                            paymentRequestID = Convert.ToInt64(rdr1["PaymentRequestID"]);
                                        }

                                        if (!string.IsNullOrEmpty(rdr1["CustomReference01"].ToString()))
                                        {
                                            chequeNumber = rdr1["CustomReference01"].ToString();
                                        }
                                    }
                                }

                                rdr1.Close();
                                if (!string.IsNullOrEmpty(chequeNumber))
                                {
                                    ////Write error message in Log file and rollback whole transaction.
                                    string errMessage = string.Format(Constants.ERRORMSG_CHEQUE_EXISTS, rowCount.ToString(), claimTransactionHeaderReference);
                                    this.str.Add(errMessage);
                                    throw new Exception(errMessage);
                                }
                                else
                                {
                                    if (paymentRequestID != null || paymentRequestID > 0)
                                    {
                                        ////update due date and cheque no in db table and Write Successful update message in Log file.
                                        sqlCommand.CommandText = String.Format(Constants.PRQST_QUERY, paymentRequestID);
                                        if (!string.IsNullOrEmpty(chequeNo))
                                        {
                                            sqlCommand.Parameters.AddWithValue("@ChequeNo", chequeNo);
                                        }
                                        else
                                        {
                                            sqlCommand.Parameters.AddWithValue("@ChequeNo", DBNull.Value);
                                        }

                                        if (!string.IsNullOrEmpty(paymentIssuedDate.ToString()))
                                        {
                                            sqlCommand.Parameters.AddWithValue("@PaymentIssuedDate", paymentIssuedDate);
                                        }
                                        else
                                        {
                                            sqlCommand.Parameters.AddWithValue("@PaymentIssuedDate", DBNull.Value);
                                        }

                                        sqlCommand.ExecuteNonQuery();
                                        sqlCommand.Parameters.Clear();
                                        paymentAmount = paymentAmount + row.Item4;
                                        if (_Logger.IsInfoEnabled)
                                        {
                                            _Logger.Info(string.Format(Constants.PAYMENTFOUND, rowCount.ToString(), claimTransactionHeaderReference, chequeNo, paymentIssuedDate));
                                        }

                                        this.str.Add(string.Format(Constants.PAYMENTFOUND, rowCount.ToString(), claimTransactionHeaderReference, chequeNo, paymentIssuedDate));
                                    }
                                    else
                                    {
                                        /////Write error message in Log file and rollback whole transaction.
                                        string errMessage = string.Format(Constants.ERRORMESSAGE_CLAIMTRANSACTION_NOT_FOUND, rowCount.ToString(), claimTransactionHeaderReference);
                                        this.str.Add(errMessage);
                                        throw new Exception(errMessage);
                                    }
                                }
                            }

                            transaction.Commit();
                            ////Write Successful update message in Log file file was processed successfully.
                            statusMessage = string.Format(Constants.STATUS_MESSAGE, rowCount, paymentAmount);
                            trasactionStatus = Constants.TRANSACTIONSTATUSSUCCESS;
                            if (_Logger.IsInfoEnabled)
                            {
                                _Logger.Info(statusMessage);
                            }

                            this.str.Add(statusMessage);
                        }
                        catch (Exception ex)
                        {
                            this.str.Add(Constants.ERRORMSG_NOTUPDATED);
                            trasactionStatus = Constants.TRANSACTIONSTATUSFAIL;
                            statusMessage = Constants.ERRORMSG_NOTUPDATED;
                            transaction.Rollback();
                            _Logger.Error(ex);
                        }
                        finally
                        {
                            transaction.Dispose();
                            conn.Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.str.Add(Constants.ERRORMSG_NOTUPDATED);
                trasactionStatus = Constants.TRANSACTIONSTATUSFAIL;
                this.str.Add(ex.Message);
                statusMessage = Constants.ERRORMSG_NOTUPDATED;
                _Logger.Error(ex);
            }
            finally
            {                                                
                this.CreateLogFile(this.str,logFilePath);
                if (this.serviceManager.DoSendEmails)
                {
                    this.SendEmail(statusMessage, trasactionStatus, logFilePath, processsedFileName);
                }

                this.str.Clear();
                if (_Logger.IsInfoEnabled)
                {
                    _Logger.Info("Processed Rows");
                }
            }
        }

        /// <summary>
        /// Sends email having the summary of the processsed file.
        /// </summary>
        /// <param name="errorMessage"> error message for email body</param>
        /// <param name="transactionStatus">Transation status</param>
        /// <param name="logFilePath">Path of log file</param>
        /// <param name="processedFileName">Preocessed file name</param>
        private void SendEmail(string errorMessage, string transactionStatus, string logFilePath, string processedFileName)
        {
            try
            {
                EmailHelper emlHelper = new EmailHelper();
                emlHelper.EmailRecipients = this.serviceManager.EmailRecepients;
                emlHelper.EmailSender = this.serviceManager.EmailSender;
                emlHelper.SMTPPort = Convert.ToInt32(this.serviceManager.SMTPPort);
                emlHelper.SMTPServer = this.serviceManager.SMTPServer;
                emlHelper.SendEmail(errorMessage, logFilePath, transactionStatus, processedFileName);
            }
            catch (Exception ec)
            {
                _Logger.Error(ec.Message);
            }
        }


        /// <summary>s
        /// Create a *.txt Log file.
        /// </summary>
        /// <param name="str">List of String Type</param>
        /// <param name="logFilePath">Path of log file</param>
        private void CreateLogFile(List<string> str, string logFilePath)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(new FileStream(logFilePath, FileMode.Create)))
                {
                    foreach (var s in this.str)
                    {
                        writer.WriteLine("{0}", s);
                    }
                }

                logFilePath = null;
            }
            catch (Exception excp)
            {
                _Logger.Error(excp);
                this.str.Add(excp.Message);
            }
        }
    }
}
