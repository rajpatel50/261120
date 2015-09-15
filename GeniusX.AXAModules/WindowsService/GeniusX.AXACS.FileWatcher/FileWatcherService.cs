using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using GeniusX.AXACS.WindowsConsole.Facade;
using Xiap.Framework.Logging;
using System.Configuration;
using System.Linq;

namespace GeniusX.AXACS.FileWatcher
{
    public partial class FileWatcherService : ServiceBase
    {
        private static readonly ILogger logger = LoggerFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private FileWatcherServiceManager serviceManager;
        /// <summary>
        /// constructor,initializes the component and FilewatcherServiceManager
        /// </summary>
        public FileWatcherService()
        {
            this.InitializeComponent();
            this.serviceManager = new FileWatcherServiceManager();
        }

        /// <summary>
        /// this will be called on start of the service
        /// </summary>
        /// <param name="args">string arguments </param>
        protected override void OnStart(string[] args)
        {
            if (logger.IsInfoEnabled)
            {
                logger.Debug("Starting  FileWatcherService");
            }

            this.ValidateAppConfigSettings();
            this.serviceManager.Start();
            base.OnStart(args);

            if (logger.IsInfoEnabled)
            {
                logger.Debug("Started  FileWatcherService");
            }
        }

        /// <summary>
        /// Reads configuration from the config file,based on the key
        /// </summary>
        /// <param name="value">key to read the config</param>
        /// <returns>config values</returns>
        public static string GetConfiguration(string value)
        {
            string configValue = string.Empty;
            if (ConfigurationManager.AppSettings.Keys.OfType<string>().Contains(value))
            {
                configValue = ConfigurationManager.AppSettings[value].ToString();
            }
            else
            {
                if (logger.IsInfoEnabled)
                {
                    logger.Debug(string.Format("{0} configuration setting not found", value));
                }
            }

            return configValue;
        }

        /// <summary>
        /// This method validates all configuration settings.
        /// </summary>
        private void ValidateAppConfigSettings()
        {
            if (logger.IsInfoEnabled)
            {
                logger.Debug("Validating FileWatcherService configurations");
            }

            try
            {
                string folderPath = GetConfiguration(Constants.FolderPath);
                string archivePath = GetConfiguration(Constants.ArchivePath);
                string logFilePath = GetConfiguration(Constants.LogFilePath);
                string emailSMTPServer = GetConfiguration(Constants.SMTPServer);
                string emailSMTPPort = GetConfiguration(Constants.SMTPPort);
                string emailRecepients = GetConfiguration(Constants.EmailRecipients);
                string emailSender = GetConfiguration(Constants.EmailSender);
                string numberOfFileAccessRetries = GetConfiguration(Constants.NumberOfFileAccessRetries);
                string durationOfSleepForFileAccessRetries = GetConfiguration(Constants.DurationOfSleepForFileAccessRetries);
                bool doSendEmails = Convert.ToBoolean(GetConfiguration(Constants.DoSendEmails));
                string connString = ConfigurationManager.ConnectionStrings[Constants.ConnectionString].ToString();

                ArgumentCheck.ArgumentNullOrEmptyCheck(folderPath, Constants.ERRORMESSAGE_FolderPath);
                ArgumentCheck.ArgumentNullOrEmptyCheck(logFilePath, Constants.ERRORMESSAGE_LogFilePath);
                ArgumentCheck.ArgumentNullOrEmptyCheck(archivePath, Constants.ERRORMESSAGE_ArchivePath);
              
                ArgumentCheck.ArgumentNullOrEmptyCheck(connString, Constants.ERRORMESSAGE_CONNECTIONSTRING);
                ArgumentCheck.ArgumentNullOrEmptyCheck(doSendEmails, Constants.ERRORMESSAGE_DOSENDEMAIL);

                if (doSendEmails)
                {
                    ArgumentCheck.ArgumentNullOrEmptyCheck(emailRecepients, Constants.ERRORMESSAGE_EmailRecipients);
                    ArgumentCheck.ArgumentNullOrEmptyCheck(emailSender, Constants.ERRORMESSAGE_EmailSender);
                    ArgumentCheck.ArgumentNullOrEmptyCheck(emailSMTPServer, Constants.ERRORMESSAGE_SMTPSERVER);
                    ArgumentCheck.ArgumentNullOrEmptyCheck(emailSMTPPort, Constants.ERRORMESSAGE_SMTPPORT);
                }

                this.serviceManager.FolderPath = folderPath;
                this.serviceManager.ArchivePath = archivePath;
                this.serviceManager.LogFilePath = logFilePath;
                this.serviceManager.SMTPServer = emailSMTPServer;
                this.serviceManager.SMTPPort = emailSMTPPort;
                this.serviceManager.EmailRecepients = emailRecepients;
                this.serviceManager.EmailSender = emailSender;
                this.serviceManager.ConnectionStrings = connString;
                this.serviceManager.DoSendEmails = doSendEmails;

                if (!string.IsNullOrEmpty(durationOfSleepForFileAccessRetries))
                {
                    this.serviceManager.DurationOfSleepForFileAccessRetries = Convert.ToInt32(durationOfSleepForFileAccessRetries);
                }

                if (!string.IsNullOrEmpty(numberOfFileAccessRetries))
                {
                    this.serviceManager.NumberOfFileAccessRetries = Convert.ToInt32(numberOfFileAccessRetries);
                }
            }
            catch (Exception e)
            {
                logger.Error(e);
            }

            if (logger.IsInfoEnabled)
            {
                logger.Debug("Validated FileWatcherService configurations");
            }
        }
        
        /// <summary>
        ///  Stops the executing service.
        /// </summary>
        protected override void OnStop()
        {
            if (logger.IsInfoEnabled)
            {
                logger.Debug("Stopping FileWatcherService");
            }

            this.serviceManager.Stop();
            base.OnStop();
            if (logger.IsInfoEnabled)
            {
                logger.Debug("Stopped FileWatcherService");
            }
        }
    }
}
