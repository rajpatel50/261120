using System;
using System.Collections;
using System.Configuration;
using GeniusX.AXACS.WindowsConsole.Facade;


namespace GeniusX.AXACS.WindowsConsole
{
    public class ChequeSynchronisation
    {      
        public static void Main(string[] args)
        {           
            ProcessChequeSynchronisation processChequeSynchronisation = new ProcessChequeSynchronisation();
            processChequeSynchronisation.GeniusXConnectionString = ConfigurationManager.ConnectionStrings["GeniusX"].ToString();
            processChequeSynchronisation.GeniusConnectionString = ConfigurationManager.ConnectionStrings["Genius"].ToString();

            Hashtable config = ConfigurationManager.GetSection("AXA") as Hashtable;
            processChequeSynchronisation.DoSendEmail = Convert.ToString(config["DoSendEmail"]);
            processChequeSynchronisation.MaxBatchSize = Convert.ToString(config["MaxBatchSize"]);
            processChequeSynchronisation.GeniusQuery = Convert.ToString(config["GeniusQuery"]);
            processChequeSynchronisation.CommandTimeout = Convert.ToInt16(config["CommandTimeout"]);
            
            processChequeSynchronisation.SMTPServer = Convert.ToString(config["SMTPServer"]);
            processChequeSynchronisation.SMTPPort = Convert.ToString(config["SMTPPort"]);
            processChequeSynchronisation.EmailRecipients = Convert.ToString(config["EmailRecipients"]);
            processChequeSynchronisation.EmailSender = Convert.ToString(config["EmailSender"]);
            processChequeSynchronisation.SMTPDomain = Convert.ToString(config["SMTPDomain"]);
            processChequeSynchronisation.SMTPUser = Convert.ToString(config["SMTPUser"]);
            processChequeSynchronisation.SMTPEncryptedPassword = Convert.ToString(config["SMTPEncryptedPassword"]);

            processChequeSynchronisation.GeniusEncryptedPassword = Convert.ToString(config["GeniusEncryptedPassword"]);
            processChequeSynchronisation.GeniusSchema = Convert.ToString(config["GeniusSchema"]);
         
            processChequeSynchronisation.InitiateTransaction();
        }
    }
}
