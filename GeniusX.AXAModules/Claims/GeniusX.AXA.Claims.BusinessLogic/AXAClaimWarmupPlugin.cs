using System;
using System.Collections.Generic;
using System.Linq;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.Caching;
using Xiap.Framework.Common;
using Xiap.Framework.Common.Product;
using Xiap.Framework.Logging;
using Xiap.Framework.Metadata;
using Xiap.Framework.Utils;
using Xiap.Framework.Warmup;
using Xiap.Metadata.BusinessComponent;
using Xiap.Metadata.Data;
using Xiap.Metadata.Data.Enums;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    public class AXAClaimWarmupPlugin:IWarmupTask
    {
        private static readonly ILogger _Logger = LoggerFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private string _Name = "AXAClaimWarmupPlugin";
     
        public string Name
        {
            get { return this._Name; }
        }

        /// <summary>
        /// Warm-up scripts for ProductWordings, ProductClaimDetails, ProductEvents, ProductNameInvolvements and ProductDocuments
        /// Exchange rates from Genius are also loaded for the company.
        /// </summary>
        /// <returns>true if successful</returns>
        public bool Process()
        {
            try
            {
                using (MetadataEntities entities = MetadataEntitiesFactory.GetMetadataEntities())
                {
                    var productversionids = entities.ProductClaimDefinition.Select(x => x.ProductVersionID);
                    foreach (long productVersionID in productversionids)
                    {
                        ProductService.GetProductWordingQuery().GetProductWordings(productVersionID);
                        ProductService.GetProductClaimDetailQuery().GetProductClaimDetails(productVersionID);
                        ProductService.GetProductEventQuery().GetProductEvents(productVersionID);
                        ProductService.GetProductNameInvolvementQuery().GetProductNameInvolvements(productVersionID);
                        ProductService.GetProductDocumentQuery().GetProductDocuments(productVersionID);
                    }

                    //// Fetch the ProductnameInvolvements where NameInvolvementType = Company
                    var productNameInvolvementIDs = entities.ProductNameInvolvement.Where(x => x.NameInvolvementType == (short)StaticValues.NameInvolvementType.Company).Select(y => y.ProductNameInvolvementID);
                                
                    foreach (long productNameInvolvementID in productNameInvolvementIDs)
                    {
                        //// Fetch the dafaulted NameID on the Company NameInvolvementType
                        var nameIDField = entities.ProductNameInvolvementField.Where(x => x.ProductNameInvolvement.ProductNameInvolvementID == productNameInvolvementID && x.ConfigurableField.ConfigurableFieldID == ClaimNameInvolvement.NameIDConfigurableFieldId).FirstOrDefault();
                        //// Fetch the dafualted NameUsage on the Company NameInvolvementType
                        var nameUsageField = entities.ProductNameInvolvementField.Where(x => x.ProductNameInvolvement.ProductNameInvolvementID == productNameInvolvementID && x.ConfigurableField.ConfigurableFieldID == ClaimNameInvolvement.NameUsageTypeCodeConfigurableFieldId).FirstOrDefault();

                        if (nameIDField != null && nameUsageField != null && nameIDField.DefaultValue != null && nameUsageField.DefaultValue!=null)
                        {
                            this.GetExchangeRates(Convert.ToInt64(nameIDField.DefaultValue), nameUsageField.DefaultValue, null, ClaimConstants.DEFAULT_CURRENCY_CODE);
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _Logger.Error(ex);
                _Logger.Info(ex.StackTrace);
                return false;
            }
        }

        private Dictionary<string, decimal> GetExchangeRates(long companyNameID, string nameUsageTypeCode, DateTime? dateOfLossFrom, string baseCurrency)
        {
            // TODO Load exchange rates just for the company (instead of all)
            Dictionary<string, decimal> rates = new Dictionary<string, decimal>();

            IExternalCurrencyConverter currencyConverter = ObjectFactory.Resolve<IExternalCurrencyConverter>();
            if (companyNameID > 0)
            {
                string cacheKey = string.Format("XIAP_CompanyBaseCurrency_ExchangeRate_Cache_{0}_{1}", companyNameID, baseCurrency);
                var cachedData = (Dictionary<string, decimal>)CacheManager.GetFromCache(cacheKey);

                if (cachedData == null)
                {
                    int dps = Convert.ToInt16(System.Configuration.ConfigurationManager.AppSettings["ExchangeRateDecimalScale"]);
                    var currencyCodes = SystemValueSetCache.GetSystemValues<ICurrencyData>(SystemValueSetCodeEnum.CurrencyCode);
                    foreach (var currency in currencyCodes)
                    {
                        decimal exchangeRate = currencyConverter.GetExchangeRate(companyNameID, nameUsageTypeCode, dateOfLossFrom.GetValueOrDefault(DateTimeUtils.GetDateTimeToday()), baseCurrency, currency.Code, baseCurrency, null);
                        if (!rates.ContainsKey(currency.Code))
                        {
                            rates.Add(currency.Code, Decimal.Round(exchangeRate, dps, MidpointRounding.AwayFromZero));
                        }
                    }

                    CacheManager.AddToCache(cacheKey, rates);
                }
                else
                {
                    rates = cachedData;
                }
            }

            return rates;
        }
    }
}
