using System.Linq;
using XIAP.FrontendModules.Claims.Search.Model;
using XIAP.FrontendModules.Search.Model;

namespace GeniusX.AXA.FrontendModules.Claims.Model
{
    public class AXASearchResultModel : SearchResultModel
    {
        public AXASearchResultModel()
        { 
        }

        public override string TabHeaderTitle
        {
            get
            {
                int rowCount = 0;
                if (this.GlobalSearchRows != null && this.GlobalSearchRows.Count > 0 && this.GlobalSearchRows.FirstOrDefault().SearchRow != null)
                {
                    rowCount = (this.GlobalSearchRows.FirstOrDefault().SearchRow as ClaimSearchRow).GetColumnValue<int>("TotalClaimsCount");
                }

                if (this.IsLoadingData == false)
                {
                    return base.TabHeaderTitle.Split('(')[0] + "(" + rowCount.ToString("#,##0") + ")";
                }
                else
                {
                    return base.TabHeaderTitle;
                }
            }

            set
            {
                base.TabHeaderTitle = value;
                this.OnPropertyChanged("TabHeaderTitle");
            }
        }
    }
}
