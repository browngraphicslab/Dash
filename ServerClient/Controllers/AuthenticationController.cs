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
    /// <summary>
    /// The authentication controller lets us login the user
    /// </summary>
    public class AuthenticationController
    {
        /// <summary>
        /// A class which provides a connection to the server
        /// </summary>
        private readonly ServerController _connection;

        /// <summary>
        /// Creates a new instance of the authentication controller
        /// </summary>
        /// <param name="connection">A class which provides a connection to the server, provided through dependency injection</param>
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
        private async Task<AuthenticationTokenModel> RequestToken(string username, string pass)
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

            // request the token from the serrver
            var result = _connection.Post(DashConstants.TokenEndpoint, content, false);
            return await result.Content.ReadAsAsync<AuthenticationTokenModel>();
        }

        public async Task<Result> TryLogin(string username, string pass)
        {
            try
            {
                var token = await RequestToken(username, pass);
                _connection.SetAuthorizationToken(token);
                return new Result(true);
            }
            catch (ApiException e)
            {
                return new Result(false, e.Errors[1]);
            }
        }
    }
}
