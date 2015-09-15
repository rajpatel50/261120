using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;
using System.Threading;
////using GeniusX.AXACS.WindowsConsole.Facade;
////using Xiap.Framework.Logging;
using System.Configuration;
using System.Linq;

namespace GeniusX.AXACS.FileWatcher
{
    public partial class FileWatcherService : ServiceBase
    {
        //// private static readonly ILogger logger = LoggerFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        //// private FileWatcherServiceManager serviceManager;

        public FileWatcherService()
        {
            this.InitializeComponent();
            //// this.serviceManager = new FileWatcherServiceManager();
        }

        protected override void OnStart(string[] args)
        {
            /*
            this.ValidateAppConfigSettings();
            
            this.serviceManager.FolderPath = ConfigurationManager.AppSettings["FolderPath"];
            this.serviceManager.TargetPath = ConfigurationManager.AppSettings["TargetPath"];
            this.serviceManager.LogFilename = ConfigurationManager.AppSettings["LogFileName"];
            this.serviceManager.CommandTimeOut = ConfigurationManager.AppSettings["CommandTimeout"];
            this.serviceManager.DoSendEmail = Convert.ToBoolean(ConfigurationManager.AppSettings["DoSendEmail"]);
            this.serviceManager.SMTPServer = ConfigurationManager.AppSettings["SMTPServer"];
            this.serviceManager.SMTPPort = ConfigurationManager.AppSettings["SMTPPort"];
            this.serviceManager.EmailRecepients = ConfigurationManager.AppSettings["EmailRecipients"];
            this.serviceManager.EmailSender = ConfigurationManager.AppSettings["EmailSender"];
            this.serviceManager.SMTPDomain = ConfigurationManager.AppSettings["SMTPDomain"];
            this.serviceManager.SMTPUser = ConfigurationManager.AppSettings["SMTPUser"];
            this.serviceManager.SMTPEncryptedPassword = ConfigurationManager.AppSettings["SMTPEncryptedPassword"];
            this.serviceManager.ValidateConfig = ConfigurationManager.AppSettings["VALIDATE_CONFIG"];
            this.serviceManager.Start();*/
            base.OnStart(args);
        }

        public static string GetConfiguration(string value)
        {
            string configValue = string.Empty;
            if (ConfigurationManager.AppSettings.Keys.OfType<string>().Contains(value))
            {
                configValue = ConfigurationManager.AppSettings[value].ToString();
            }
            else
            {
                /*if (logger.IsDebugEnabled)
                {
                    logger.Debug(string.Format("{0} configuration setting not found", value));
                }*/
            }

            return configValue;
        }

        /*
        private void ValidateAppConfigSettings()
        {
            try
            {
                if (ArgumentCheck.Equals(GetConfiguration(Constants.SB_VALIDATE_CONFIG), "Y"))
                {
                    ArgumentCheck.ArgumentNullOrEmptyCheck(GetConfiguration(Constants.SMTPServer), Constants.ERRORMESSAGE_SMTPSERVER);
                    ArgumentCheck.ArgumentNullOrEmptyCheck(GetConfiguration(Constants.SMTPPort), Constants.ERRORMESSAGE_SMTPPORT);
                    ArgumentCheck.ArgumentNullOrEmptyCheck(GetConfiguration(Constants.SMTPDomain), Constants.ERRORMESSAGE_SMTPDOMAIN);
                    ArgumentCheck.ArgumentNullOrEmptyCheck(GetConfiguration(Constants.FolderPath), Constants.ERRORMESSAGE_FolderPath);
                    ArgumentCheck.ArgumentNullOrEmptyCheck(GetConfiguration(Constants.LogFileName), Constants.ERRORMESSAGE_LogFileName);
                    ArgumentCheck.ArgumentNullOrEmptyCheck(GetConfiguration(Constants.TargetPath), Constants.ERRORMESSAGE_TargetPath);
                    ArgumentCheck.ArgumentNullOrEmptyCheck(GetConfiguration(Constants.SMTPUser), Constants.ERRORMESSAGE_SMTPUSER);
                    ArgumentCheck.ArgumentNullOrEmptyCheck(GetConfiguration(Constants.FolderPath), Constants.ERRORMESSAGE_FolderPath);
                    ArgumentCheck.ArgumentNullOrEmptyCheck(ConfigurationManager.ConnectionStrings["Config"], Constants.ERRORMESSAGE_CONNECTIONSTRING);
                }
            }
            catch (Exception e)
            {
                logger.Error(e);
            }
        }
        */
        protected override void OnStop()

        {
            base.OnStop();
            //this.serviceManager.Stop();            
        }
    }
}
