using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xiap.Framework.Logging;
using System.Threading.Tasks;
using System.Threading;
using GeniusX.AXACS.WindowsConsole.Facade;

namespace GeniusX.AXACS.WindowsConsole.Facade
{    
    public class FileWatcherServiceManager
    {
        private static readonly ILogger logger = LoggerFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);      
        private ProcessFile processFileService;        
        private Thread thread;        
        public string FolderPath { get; set; }
        public string ArchivePath { get; set; }
        public string LogFilePath { get; set; }
        public string SMTPServer { get; set; }
        public string SMTPPort { get; set; }
        public string EmailRecepients { get; set; }
        public string EmailSender { get; set; }
        public string ConnectionStrings { get; set; }
        public bool DoSendEmails { get; set; }
        public int? NumberOfFileAccessRetries { get; set; }
        public int? DurationOfSleepForFileAccessRetries { get; set; }
    

        /// <summary>
        /// Launches the file watcher service
        /// </summary>             
        private void LaunchProcess()
        {
            try
            {
                this.processFileService = new ProcessFile(this);
                if (logger.IsInfoEnabled)
                {
                    logger.Info("File Watcher Service Starting... ");
                }

                this.processFileService.WatchFolder();
                if (logger.IsInfoEnabled)
                {
                    logger.Info("File Watcher Service Started");
                }
            }
            catch (Exception e)
            {
                logger.Error(e);
                throw e;
            }
        }

        /// <summary>
        /// Start the File Watcher Service
        /// </summary>
        public void Start()
        {
            try
            {
                if (logger.IsInfoEnabled)
                {
                    logger.Info("Launching FileWatcherService ");
                }

                this.thread = new Thread(this.LaunchProcess);
                this.thread.Start();
                if (logger.IsInfoEnabled)
                {
                    logger.Info("Launched FileWatcherService ");
                }
            }
            catch (Exception e)
            {
                logger.Error(e);
                throw e;
            }
        }

        /// <summary>
        /// stops the file watcher service. Forced stop after 30 seconds
        /// </summary>
        public void Stop()
        {
            try
            {
                if (logger.IsInfoEnabled)
                {
                    logger.Info(string.Format("Stopping the FileWatcher Service"));
                }

                // only wait for a maximum of 30 secs before aborting
                Thread.Sleep(30000);
                this.thread.Abort();
                if (logger.IsInfoEnabled)
                {
                    logger.Debug(string.Format("FileWatcher Service Stopped"));
                }
            }
            catch (Exception e)
            {
                logger.Error(e);
                throw e;
            }
        }
    }
}
