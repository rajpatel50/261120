using Xiap.Framework.Logging;
using XIAP.FrontendModules.Common.SearchService;
namespace GeniusX.AXA.FrontendModules.Claims.ClaimSummaryCalculation
{
    /// <summary>
    /// This class holds the search results for the historical amounts.
    /// </summary>
    public class ClaimFinancialAmount
    {
        private static readonly ILogger _Logger = LoggerFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public ClaimFinancialAmount(SearchResultRow data)
        {
            this._row = data;

            this.ClaimDetailID = (long?)this.GetColumnValue("ClaimDetailID");
            if (_Logger.IsDebugEnabled && this.ClaimDetailID.HasValue)
            {
                _Logger.Debug(string.Format("Claim detailID output value :{0}", this.ClaimDetailID.Value.ToString()));
            }

            this.TotalClaimLoss = (decimal?)this.GetColumnValue("TotalClaimLoss");
            if (_Logger.IsDebugEnabled && this.TotalClaimLoss.HasValue)
            {
                _Logger.Debug(string.Format("Total Claim Loss output value :{0}", this.TotalClaimLoss.Value.ToString()));
            }

            this.Excess = (decimal?)this.GetColumnValue("Excess");
            if (_Logger.IsDebugEnabled && this.Excess.HasValue)
            {
                _Logger.Debug(string.Format("Excess output value :{0}", this.Excess.Value.ToString()));
            }

            this.OutstandingEstimates = (decimal?)this.GetColumnValue("OutstandingEstimates");
            if (_Logger.IsDebugEnabled && this.OutstandingEstimates.HasValue)
            {
                _Logger.Debug(string.Format("Outstanding Estimates output value :{0}", this.OutstandingEstimates.Value.ToString()));
            }

            this.PaymentsInProgress = (decimal?)this.GetColumnValue("PaymentsInProgress");
            if (_Logger.IsDebugEnabled && this.PaymentsInProgress.HasValue)
            {
                _Logger.Debug(string.Format("Payments In Progress output value :{0}", this.PaymentsInProgress.Value.ToString()));
            }

            this.TotalPaymentsPaid = (decimal?)this.GetColumnValue("TotalPaymentsPaid");
            if (_Logger.IsDebugEnabled && this.TotalPaymentsPaid.HasValue)
            {
                _Logger.Debug(string.Format("Total Payments Paid output value :{0}", this.TotalPaymentsPaid.Value.ToString()));
            }

            this.OutstandingRecoveryEstimates = (decimal?)this.GetColumnValue("OutstandingRecoveryEstimates");
            if (_Logger.IsDebugEnabled && this.OutstandingRecoveryEstimates.HasValue)
            {
                _Logger.Debug(string.Format("Outstanding Recovery Estimates output value :{0}", this.OutstandingRecoveryEstimates.Value.ToString()));
            }

            this.OutstandingULREstimate = (decimal?)this.GetColumnValue("OutstandingULREstimate");
            if (_Logger.IsDebugEnabled && this.OutstandingULREstimate.HasValue)
            {
                _Logger.Debug(string.Format("Outstanding ULR Estimate output value :{0}", this.OutstandingULREstimate.Value.ToString()));
            }

            this.RecoveriesInProgress = (decimal?)this.GetColumnValue("RecoveriesInProgress");
            if (_Logger.IsDebugEnabled && this.RecoveriesInProgress.HasValue)
            {
                _Logger.Debug(string.Format("Recoveries In Progress output value :{0}", this.RecoveriesInProgress.Value.ToString()));
            }

            this.ULRInProgress = (decimal?)this.GetColumnValue("ULRInProgress");
            if (_Logger.IsDebugEnabled && this.ULRInProgress.HasValue)
            {
                _Logger.Debug(string.Format("ULR In Progress output value :{0}", this.ULRInProgress.Value.ToString()));
            }

            this.RecoveriesCompleted = (decimal?)this.GetColumnValue("RecoveriesCompleted");
            if (_Logger.IsDebugEnabled && this.RecoveriesCompleted.HasValue)
            {
                _Logger.Debug(string.Format("Recoveries Completed output value :{0}", this.RecoveriesCompleted.Value.ToString()));
            }

            this.ULRCompleted = (decimal?)this.GetColumnValue("ULRCompleted");
            if (_Logger.IsDebugEnabled && this.ULRCompleted.HasValue)
            {
                _Logger.Debug(string.Format("ULR Completed output value :{0}", this.ULRCompleted.Value.ToString()));
            }

            this.MovementDeductibleType = (short?)this.GetColumnValue("MovementDeductibleType");
            if (_Logger.IsDebugEnabled && this.MovementDeductibleType.HasValue)
            {
                _Logger.Debug(string.Format("Movement Deductible Type output value :{0}", this.MovementDeductibleType.Value.ToString()));
            }

            this.OrderShareCoinsurance = (short?)this.GetColumnValue("OrderShareCoinsurance");
            if (_Logger.IsDebugEnabled && this.OrderShareCoinsurance.HasValue)
            {
                _Logger.Debug(string.Format("OrderShareCoinsurance value :{0}", this.OrderShareCoinsurance.Value.ToString()));
            }           
        }

        public decimal? TotalClaimLoss { get; set; }
        public decimal? Excess { get; set; }
        public decimal? OutstandingEstimates { get; set; }
        public decimal? PaymentsInProgress { get; set; }
        public decimal? TotalPaymentsPaid { get; set; }
        public decimal? OutstandingRecoveryEstimates { get; set; }
        public decimal? OutstandingULREstimate { get; set; }
        public decimal? RecoveriesInProgress { get; set; }
        public decimal? ULRInProgress { get; set; }
        public decimal? RecoveriesCompleted { get; set; }
        public decimal? ULRCompleted { get; set; }
        public long? ClaimDetailID { get; set; }
        public short? MovementDeductibleType { get; set; }
        public short? OrderShareCoinsurance { get; set; }
      
        private SearchResultRow _row { get; set; }

        ////Get the column value based on the column name.
        protected object GetColumnValue(string columnName)
        {
            SearchResultColumn column = new SearchResultColumn();
            column.ColumnName = string.Empty;
            column.Value = string.Empty;

            if (this._row != null && this._row.Columns != null && this._row.Columns.Count > 0)
            {
                foreach (SearchResultColumn col in this._row.Columns)
                {
                    if (col.ColumnName.ToLower() == columnName.ToLower())
                    {
                        column = col;
                        break;
                    }
                }
            }

            return column.Value;
        }
    }
}
