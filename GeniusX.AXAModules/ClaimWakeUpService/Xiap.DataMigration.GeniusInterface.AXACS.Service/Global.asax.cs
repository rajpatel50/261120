using System;
using System.Configuration;
using System.Web;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.Configuration;
using Xiap.Framework;
using Xiap.Framework.Caching;
using Xiap.Framework.DataMapping;
using Xiap.Framework.Logging;

namespace Xiap.DataMigration.GeniusInterface.AXACS.Service
{
    public class Global : System.Web.HttpApplication
    {
        private static readonly ILogger logger = LoggerFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected void Application_Start(object sender, EventArgs e)
        {
            IXiapCache cache = ObjectFactory.Resolve<IXiapCache>();

            //TODO: Workaround for getting web cache working
            Xiap.Caching.WebCache xiapWebCache = cache as Xiap.Caching.WebCache;
            if (xiapWebCache != null)
            {
                xiapWebCache.CacheStore = this.Context.Cache;
                CacheManager.SetCache(xiapWebCache);
            }

            PropertyAccessorCache.InitializeCache();

            XiapStateManager.Init();

            try
            {
                var section = (UnityConfigurationSection)ConfigurationManager.GetSection("xiap/core/unity");
                section.Configure(GlobalClaimWakeUp.Container);
                section = (UnityConfigurationSection)ConfigurationManager.GetSection("xiap/custom/unity");
                section.Configure(GlobalClaimWakeUp.Container);
                GlobalClaimWakeUp.Container.RegisterType<TransferToGenius>();
            }
            catch (Exception)
            {
                 return;
            }

        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {
            var user = HttpContext.Current.User;
        }
    }
}
