using System;
using System.Collections.Generic;
using System.ServiceProcess;
using Xiap.Framework.Logging;

namespace GeniusX.AXACS.FileWatcher
{
   public static class Program
    {
        private static readonly ILogger logger = LoggerFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static void Main()
        {
            try
            {
               if (logger.IsDebugEnabled)
                {
                    logger.Debug("Starting Service");
                }
               
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[] 
                {
                    new FileWatcherService()
                };
                
                ServiceBase.Run(ServicesToRun);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }
    }
}
