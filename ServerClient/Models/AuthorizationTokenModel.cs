using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    /// <summary>
    /// The authorization token returned from the server used to make requests
    /// </summary>
    public class AuthorizationTokenModel
    {
        /// <summary>
        /// The actual access token is the secret key which grants us access to the server
        /// </summary>
        public string Access_token;

        /// <summary>
        /// The type of this authorization token
        /// </summary>
        public string Token_type;

        /// <summary>
        /// Number of seconds until the token expires
        /// </summary>
        public int Expires_in;
    }
}
