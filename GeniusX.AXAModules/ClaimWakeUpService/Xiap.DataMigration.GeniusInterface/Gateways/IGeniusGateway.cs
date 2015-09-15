using System;
using System.Collections.Generic;
using Xiap.DataMigration.GeniusInterface.AXACS.Entities;

namespace Xiap.DataMigration.GeniusInterface.AXACS.Gateways
{
    public interface IGeniusGateway
    {
        IEnumerable<CoverageDatesCacheItem> GetCoverageInformationFromGenius(string[] policyReferences);

        IEnumerable<SectionDetailCacheItem> GetSectionDetailInformationFromGenius(string[] policyReferences);

        bool CheckDates(string coverageKey, DateTime date);

        string GetSectionDetailDateOfLossTypeCode(string sectionDetailKey);
    }
}
