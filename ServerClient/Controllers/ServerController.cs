using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class ServerController
    {

        public ServerController()
        {
            Connection = InitializeConnection();
        }

        /// <summary>
        /// The connection to the server, we'll try to reuse this but may run into issues where defaults need to change,
        /// some people recommend having one connection per request type i.e. accounts | documents | admin | etc.
        /// </summary>
        public readonly HttpClient Connection;

        /// <summary>
        /// Initialize the connection to the server
        /// </summary>
        /// <returns></returns>
        private HttpClient InitializeConnection()
        {
            return new HttpClient();
        }

        /// <summary>
        /// Set the connection to use the authorization token which is passed in
        /// </summary>
        /// <param name="token"></param>
        public void SetAuthorizationToken(AuthenticationTokenModel token)
        {
            Connection.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(token.Token_type,
                token.Access_token);
        }

        /// <summary>
        /// Makes a post request to the server
        /// </summary>
        /// <param name="path">The path the post request is performed on, this path is appended to the base url</param>
        /// <param name="bodyObject">The object which is serialized in json as the body of the post request</param>
        /// <returns>An HttpResponseMessage upon success or throws an HttpRequestException upon error</returns>
        public HttpResponseMessage Post(string path, object bodyObject)
        {
            try
            {
                // make the post request and get the result
                var response = Connection.PostAsJsonAsync(DashConstants.ServerBaseUrl + path, bodyObject).Result;

                // if the response failed throw an exception
                if (!response.IsSuccessStatusCode)
                {
                    // create api exception wrapper which lets us print a useful message to debug output
                    var ex = new ApiException(response);
                    Debug.WriteLine(ex.ApiExceptionMessage());

                    // then throw the HttpRequestException
                    response.EnsureSuccessStatusCode();
                    //TODO we should think about how post requests should work, maybe we want post requests to happen in individual controllers
                }

                // otherwise return the result
                return response;
            }
            // catch exceptions which occur when we were unable to connect to the server!
            catch (AggregateException ex)
            {
                Debug.WriteLine("One or more exceptions has occurred:");
                foreach (var exception in ex.InnerExceptions)
                {
                    Debug.WriteLine("  " + exception.Message);
                }

                throw;
            }
        }

        /// <summary>
        /// Method to get an item from the server
        /// </summary>
        /// <typeparam name="T">The type of the item we are getting from the server</typeparam>
        /// <param name="apiPath">The path in the api that we are getting from</param>
        /// <returns>An item of type T, or throws and HttpRequestError or an AggregateException</returns>
        public async Task<T> GetItem<T>(string apiPath)
        {
            try
            {
                // make the get request and get the result
                var response = Connection.GetAsync(DashConstants.ServerBaseUrl + apiPath).Result;

                // if the response failed throw an exception
                if (!response.IsSuccessStatusCode)
                {
                    // create api exception wrapper which lets us print a useful message to debug output
                    var ex = new ApiException(response);
                    Debug.WriteLine(ex.ApiExceptionMessage());

                    // then throw the HttpRequestException
                    response.EnsureSuccessStatusCode();
                    //TODO we should think about how get requests should work, maybe we want get requests to happen in individual controllers
                }

                // otherwise return the result
                return await response.Content.ReadAsAsync<T>();
            }
            // catch exceptions which occur when we were unable to connect to the server!
            catch (AggregateException ex)
            {
                Debug.WriteLine("One or more exceptions has occurred:");
                foreach (var exception in ex.InnerExceptions)
                {
                    Debug.WriteLine("  " + exception.Message);
                }

                throw;
            }
        }
    }

}
