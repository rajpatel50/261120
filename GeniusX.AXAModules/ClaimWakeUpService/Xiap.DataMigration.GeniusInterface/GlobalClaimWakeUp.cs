using System.Linq;

using Xiap.Metadata.BusinessComponent;

namespace Xiap.DataMigration.GeniusInterface.AXACS
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Configuration;
    using Entities;
    using Framework.Security;
    using Microsoft.Practices.Unity;
using System.Web;

    public static class GlobalClaimWakeUp
    {
       // public static HttpContext context = HttpContext.Current;

        public static readonly IUnityContainer Container = new UnityContainer();

        public static readonly Lazy<long> ActingUserId = new Lazy<long>(() => XiapSecurity.GetUser().UserID);
        
        public static readonly ConcurrentDictionary<int, List<Claim>> PostedClaims = new ConcurrentDictionary<int, List<Claim>>();

        public static readonly ConcurrentDictionary<string, List<FlattenedTransaction>> MappedTransactionDetails = new ConcurrentDictionary<string, List<FlattenedTransaction>>();

        public static readonly ConcurrentDictionary<string, bool> GeniusXPolicyState = new ConcurrentDictionary<string, bool>();

        public static readonly List<string> NameReferences = new List<string>();

        public static readonly string[] NameUsageTypeCodes = new[] { "UCL", "WIT", "DRV" };

        public static readonly ConcurrentDictionary<string, TimeSpan> Statistics = new ConcurrentDictionary<string, TimeSpan>();

        public static Lazy<string> MotorProductCode = new Lazy<string>(() => ConfigurationManager.AppSettings["MotorProductCode"]);

        public static Lazy<string> LiabilityProductCode = new Lazy<string>(() => ConfigurationManager.AppSettings["LiabilityProductCode"]);

        public static Lazy<string> ReviewEventTypeCode = new Lazy<string>(() => ConfigurationManager.AppSettings["ReviewEventTypeCode"]);

        private static readonly ConcurrentDictionary<string, Dictionary<Type, object>> _attachedData = new ConcurrentDictionary<string, Dictionary<Type, object>>();

        public static Lazy<string[]> DeductibleMovementTypes = new Lazy<string[]>(() =>
                                                                                      {
                                                                                          var metadataEntities =
                                                                                              MetadataEntitiesFactory.
                                                                                                  GetMetadataEntities();
                                                                                          return metadataEntities.
                                                                                                  MovementType.Where(
                                                                                                      x =>
                                                                                                      x.Code.StartsWith(
                                                                                                          "X")).Select(
                                                                                                              x =>
                                                                                                              x.Code).
                                                                                                  ToArray();
                                                                                      }); 

        public static void AddAttachedData<T>(string key, T data) where T : class
        {
            Dictionary<Type, object> values;
            List<T> list;
            if (_attachedData.TryGetValue(key, out values))
            {
                object o;
                if (!values.TryGetValue(typeof(T), out o))
                {
                    o = new List<T>();
                    values.Add(typeof(T), o);
                }
                list = (List<T>) o;
            }
            else
            {
                list = new List<T>();
                _attachedData[key] = new Dictionary<Type, object>{{typeof(T), list}};
            }

            list.Add(data);
        }

        public static List<T> GetAttachedData<T>(string key) where T : class
        {
            Dictionary<Type, object> values;
            List<T> list;
            if (_attachedData.TryGetValue(key, out values))
            {
                object o;
                if (!values.TryGetValue(typeof(T), out o))
                {
                    o = new List<T>();
                    values.Add(typeof(T), o);
                }
                list = (List<T>)o;
            }
            else
            {
                list = new List<T>();
                _attachedData[key] = new Dictionary<Type, object> { { typeof(T), list } };
            }

            return list;
        }

        public static void ClearAttachedData(string key)
        {
            Dictionary<Type, object> values;
            _attachedData.TryRemove(key, out values);
        }
    }
}
