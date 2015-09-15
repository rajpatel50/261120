using Xiap.Framework.Search;
namespace GeniusX.AXA.Claims.BusinessLogic
{
    ////ClaimSummaryAmountsSeacrh provider class.
    public class ClaimAmountsSearchService : Xiap.Search.Search
    {
        protected override void ModifySearchResults(SearchCriteria searchCriteria, SearchResults searchResults)
        {
            base.ModifySearchResults(searchCriteria, searchResults);
            ////Add custom code to modify the claim summary results.
        }
    }
}
