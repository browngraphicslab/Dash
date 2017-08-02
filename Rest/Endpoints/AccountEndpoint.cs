using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class AccountEndpoint
    {
        /// <summary>
        /// The connection to the server, provided by dependency injection
        /// </summary>
        private readonly ServerEndpoint _connection;

        /// <summary>
        /// The connection to the authorization service, provided by dependency injection
        /// </summary>
        private readonly AuthenticationEndpoint _authenticationEndpoint;

        public AccountEndpoint(ServerEndpoint connection, AuthenticationEndpoint authenticationEndpoint)
        {
            _connection = connection;
            _authenticationEndpoint = authenticationEndpoint;
        }

        /// <summary>
        /// Registers a new user with the passed in email and password
        /// </summary>
        /// <param name="username">The username of the user who is registering</param>
        /// <param name="pass">The password of the user who is registering</param>
        /// <param name="confirmPass">The confirmation password of the user who is registering</param>
        /// <exception cref="ApiException">Throws an ApiExcpetion</exception>
        public void Register(string username, string pass, string confirmPass)
        {
            // create the user
            var user = new RegisterBindingModel
            {
                Email = username,
                Password = pass,
                ConfirmPassword = confirmPass
            };

            // post it to the endpoint for registration, this endpoint will throw error or return Succesful
            _connection.Post("api/Account/Register", user);
        }

        /// <summary>
        /// Tries to register the user which will cause the user to be logged in if the registration is succesful
        /// </summary>
        /// <param name="user"></param>
        /// <param name="pass"></param>
        /// <param name="confirmPass"></param>
        /// <returns></returns>
        public async Task<Result> TryRegister(string user, string pass, string confirmPass)
        {
            try
            {
                // try to register
                Register(user, pass, confirmPass);
                // if we registered without error then we try to login with the same credentials
                return await _authenticationEndpoint.TryLogin(user, pass);
            }
            catch (ApiException e)
            {
                // return the error message
                return new Result(false, string.Join("\n", e.Errors));
            }
        }
    }
}
