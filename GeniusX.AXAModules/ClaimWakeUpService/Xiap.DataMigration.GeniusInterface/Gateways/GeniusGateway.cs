using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;
using Dapper;
using IBM.Data.DB2.iSeries;
using Xiap.DataMigration.GeniusInterface.AXACS.Entities;

namespace Xiap.DataMigration.GeniusInterface.AXACS.Gateways
{
    public class GeniusGateway : IGeniusGateway
    {
        private readonly string _connectionString;
        private Hashtable _coverageDatesCache;
        private Hashtable _sectionDetailCache;

        #region Queries
        private const string GetCoverageInformation = @"
SELECT 
    COMANU || COMASE || CORKRS || COSDSQ || COCVSQ AS CoverageKey, COCPST AS GnsStartDate, COCPED AS GnsEndDate, MAPORF AS PolicyReference
FROM ZUMADF00 AS a JOIN ZUCODF00 AS b ON a.mamanu = b.comanu AND a.mamase = b.comase
WHERE MAPORF in @PolicyReferences
";
        private const string GetSectionDetailInformation = @"
SELECT 
    SFMANU || SFMASE || SFRKRS || SFSDSQ AS SectionDetailKey
,   SFSFV3 AS Flag3
,   MAPORF AS PolicyReference
FROM ZUMADF00 AS a 
JOIN ZUSFDF00 AS b ON a.mamanu = b.sfmanu AND a.mamase = b.sfmase
WHERE MAPORF in @PolicyReferences
";
        #endregion

        public GeniusGateway(string connectionString)
        {
            _connectionString = connectionString;
            
        }

        public IEnumerable<CoverageDatesCacheItem> GetCoverageInformationFromGenius(string[] policyReferences)
        {
            using (var connection = new iDB2Connection(_connectionString))
            {
                connection.Open();
                var result = new List<CoverageDatesCacheItem>();
                foreach (var chunk in policyReferences.Chunk<string>(100))
                {
                    if (!chunk.Any()) break;
                    result.AddRange(connection.Query<CoverageDatesCacheItem>(
                        GetCoverageInformation,
                        new
                        {
                            PolicyReferences = chunk
                        },
                        null,
                        true,
                        600));
                }
                _coverageDatesCache = new Hashtable(result.ToDictionary(i => i.CoverageKey, i => i));
                return result;
            }
        }

        public IEnumerable<SectionDetailCacheItem> GetSectionDetailInformationFromGenius(string[] policyReferences)
        {
            using (var connection = new iDB2Connection(_connectionString))
            {
                connection.Open();
                var result = new List<SectionDetailCacheItem>();
                foreach (var chunk in policyReferences.Chunk<string>(100))
                {
                    if (!chunk.Any()) break; 
                    result.AddRange(connection.Query<SectionDetailCacheItem>(
                        GetSectionDetailInformation,
                        new
                        {
                            PolicyReferences = chunk
                        },
                        null,
                        true,
                        600));
                }
                _sectionDetailCache = new Hashtable(result.ToDictionary(i => i.SectionDetailKey, i => i));
                return result;
            }
        }

        public bool CheckDates(string coverageKey, DateTime date)
        {
            if (_coverageDatesCache == null) return false;
            var coverageDateCacheItem = (CoverageDatesCacheItem)_coverageDatesCache[coverageKey];
            if (coverageDateCacheItem == null) return false;
            return date >= coverageDateCacheItem.StartDate && date <= coverageDateCacheItem.EndDate;
        }

        public string GetSectionDetailDateOfLossTypeCode(string sectionDetailKey)
        {
            if (_sectionDetailCache == null) return string.Empty;
            var sectionDetailCacheItem = (SectionDetailCacheItem)_sectionDetailCache[sectionDetailKey];
            if (sectionDetailCacheItem == null) return string.Empty;
            return sectionDetailCacheItem.Flag3;
        }
    }
}
