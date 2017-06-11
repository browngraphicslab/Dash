using System.Web.Mvc;

namespace DashServer
{
    public class FilterConfig
    {
        /// <summary>
        /// Used to add global filters which will be checked before any action
        /// </summary>
        /// <param name="filters"></param>
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());

            ////Uncomment the line below to add global authorization to all actions
            //filters.Add(new AuthorizeAttribute());
        }
    }
}
