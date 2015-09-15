using IBM.Data.DB2.iSeries;

namespace Xiap.DataMigration.GeniusInterface.AXACS
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Data.EntityClient;
    using System.Data.SqlClient;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Configuration;
    using Gateways;
    using Microsoft.Practices.Unity;
    using Operations.Blocks;

    public class ClaimProcessorConfiguration
    {
        

        public int StartingRecord { get; set; }

        public bool AutoSubmit { get; set; }

        public bool ReopeningClaims { get; set; }

        public bool FilterByPolicyReference { get; set; }

        public string[] PolicyReferencesToInclude { get; set; }

        public string[] PolicyReferencesToExclude { get; set; }

        public bool FilterByClaimReference { get; set; }

        public string[] ClaimsReferencesToInclude { get; set; }

        public string[] ClaimsReferencesToExclude { get; set; }

        public Dictionary<Type, bool> TaskIsEnabled { get; set; }
        
        public Dictionary<Type, BlockDescriptionAttribute> TaskDescriptions = new Dictionary<Type, BlockDescriptionAttribute>();

        private int _batchSize = 1;
        public int BatchSize
        {
            get
            {
                return _batchSize;
            }
        }

        public virtual bool ProcessClosedClaims
        {
            get
            {
               return true;
            }
        }

        public virtual bool ProcessOpenClaims
        {
            get
            {
                return false;
            }
        }

        public virtual bool ProcessMotorClaims
        {
            get
            {
                return true;
            }
        }

        public virtual bool ProcessLiabilityClaims
        {
            get
            {
               return true; // Always process Liability Claims
            }
        }

        public virtual bool MigratedOnly
        {
            get
            {
                return true; // Always process Migrated Claims
            }
        }
        
        private string _k2ConnectionDetailsHost;

        public virtual string K2ConnectionDetailsHost
        {
            get
            {
                return _k2ConnectionDetailsHost ?? (_k2ConnectionDetailsHost = ConfigurationManager.AppSettings["k2.connectionDetails.host"]);
            }
        }

        private string _stagingMotorConnectionString;

        public string StagingMotorConnectionString
        {
            get
            {
                return _stagingMotorConnectionString ?? (_stagingMotorConnectionString = ConfigurationManager.ConnectionStrings["StagingMotor"].ConnectionString);
            }
        }

        private string _stagingLiabConnectionString;

        public string StagingLiabConnectionString
        {
            get
            {
                return _stagingLiabConnectionString ?? (_stagingLiabConnectionString = ConfigurationManager.ConnectionStrings["StagingLiab"].ConnectionString);
            }
        }

        private string _geniusXConnectionString;

        public string GeniusXConnectionString
        {
            get
            {
                return _geniusXConnectionString ?? (_geniusXConnectionString = ConfigurationManager.ConnectionStrings["GeniusX"].ConnectionString);
            }
        }

        private string _geniusConnectionString;

        public string GeniusConnectionString
        {
            get
            {
                return _geniusConnectionString ?? (_geniusConnectionString = ConfigurationManager.ConnectionStrings["Genius"].ConnectionString);
            }
        }

        public ClaimProcessorConfiguration()
        {
            ClaimsReferencesToExclude = new string[0];
            ClaimsReferencesToInclude = new string[0];
            PolicyReferencesToExclude = new string[0];
            PolicyReferencesToInclude = new string[0];
            
            StartingRecord = 0;
            FilterByPolicyReference = false;
            FilterByClaimReference = true;
            TaskIsEnabled = new Dictionary<Type, bool>(); 
            var blockQuery = from t in typeof (ClaimProcessorConfiguration).Assembly.GetTypes()
                             where typeof (IBlock).IsAssignableFrom(t) && !t.IsInterface
                             select new { Type=t, Attribute=(BlockDescriptionAttribute)t.GetCustomAttributes(typeof (BlockDescriptionAttribute), false).Single()};

            foreach (var att in blockQuery)
            {
                TaskDescriptions[att.Type] = att.Attribute;
                TaskIsEnabled[att.Type] = true;
            }
        }

        //private Uri _messageServiceEndpoint;

        //public Uri MessageServiceEndpoint
        //{
        //    get 
        //    { 
        //        Configuration configuration;
        //        var clients = GetClientSection(out configuration);
        //        return _messageServiceEndpoint ?? (_messageServiceEndpoint = clients.Endpoints.Cast<ChannelEndpointElement>().First(e => e.Name ==  "MessageService").Address);
        //    }
        //}

        //private string _messageServiceBinding;

        //public string MessageServiceBinding
        //{
        //    get
        //    {
        //        Configuration configuration;
        //        var clients = GetClientSection(out configuration);
        //        return _messageServiceBinding ?? (_messageServiceBinding = clients.Endpoints.Cast<ChannelEndpointElement>().First(e => e.Name ==  "MessageService").BindingConfiguration);
        //    }
        //}

       
    }
}
