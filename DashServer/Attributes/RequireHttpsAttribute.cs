using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace DashServer
{
    /// <summary>
    /// Filter which requires resources to access this method using https rather than http
    /// basically should be used on all secure resources, since the client will be sending an 
    /// access token which absolutely must be encrypted
    /// </summary>
    public class RequireHttpsAttribute : AuthorizationFilterAttribute
    {
        /// <summary>
        /// Automatically called when the process requests authorization
        /// </summary>
        /// <param name="actionContext">Information about the requesting process</param>
        public override void OnAuthorization(HttpActionContext actionContext)
        {
            if (actionContext.Request.RequestUri.Scheme != Uri.UriSchemeHttps)
            {
                actionContext.Response = new HttpResponseMessage(System.Net.HttpStatusCode.Forbidden)
                {
                    ReasonPhrase = "HTTPS Required"
                };
            }
            else
            {
                base.OnAuthorization(actionContext);
            }
        }
    }
}