using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DashShared;
using Newtonsoft.Json;

namespace Dash
{
    public class AuthenticationController
    {
        private readonly ServerController _connection;

        public AuthenticationController(ServerController connection)
        {
            _connection = connection;
        }


        /// <summary>
        /// Request a token from the server for a user with the passed in username and password
        /// </summary>
        /// <param name="username">The username of the user</param>
        /// <param name="pass">The password of the user</param>
        /// <returns></returns>
        public async Task<AuthenticationTokenModel> RequestToken(string username, string pass)
        {
            // create list of key value pairs needed in the post request to get an authorization token
            var pairs = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>( "grant_type", "password" ),
                    new KeyValuePair<string, string>( "username", username ),
                    new KeyValuePair<string, string> ( "Password", pass )
                };

            // convert the key value pairs into form url encoded content
            var content = new FormUrlEncodedContent(pairs);

            // request the content from the server, this post request is performed here because the Token endpoint
            // doens't accept JSON which all other endpoints do.
            var result = _connection.Connection.PostAsync(DashConstants.ServerBaseUrl + "Token", content).Result;

            return await result.Content.ReadAsAsync<AuthenticationTokenModel>();
        }
    }
}
