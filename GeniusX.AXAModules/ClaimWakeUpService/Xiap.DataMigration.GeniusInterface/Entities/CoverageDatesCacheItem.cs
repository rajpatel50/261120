using System;
using System.Globalization;

namespace Xiap.DataMigration.GeniusInterface.AXACS.Entities
{
    public class CoverageDatesCacheItem
    {
        private decimal _gnsStartDate;
        public decimal GnsStartDate
        {
            get { return _gnsStartDate; }
            set 
            { 
                _gnsStartDate = value;
                _startDate = DateTime.ParseExact((19000000 + _gnsStartDate).ToString(CultureInfo.InvariantCulture), "yyyyMMdd", CultureInfo.CurrentCulture);
            }
        }

        private decimal _gnsEndDate ;
        public decimal GnsEndDate
        {
            get { return _gnsEndDate; }
            set
            {
                _gnsEndDate = value;
                _endDate = DateTime.ParseExact((19000000 + _gnsEndDate).ToString(CultureInfo.InvariantCulture), "yyyyMMdd", CultureInfo.CurrentCulture);
            }
        }


        private DateTime _startDate;
        public DateTime StartDate
        {
            get { return _startDate; }
        }

        private DateTime _endDate;
        public DateTime EndDate
        {
            get { return _endDate; }
        }

        public string CoverageKey { get; set; }

        public string PolicyReference { get; set; }
    }
}
