using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public class AccountController
    {

        private readonly ServerController _connection;
        private readonly AuthenticationController _authenticationController;

        public AccountController(ServerController connection, AuthenticationController authenticationController)
        {
            _connection = connection;
            _authenticationController = authenticationController;
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
            var user = new RegisterModel()
            {
                Email = username,
                Password = pass,
                ConfirmPassword = confirmPass
            };

            _connection.Post("api/Account/Register", user);
        }

        public async Task<Result> TryRegister(string user, string pass, string confirmPass)
        {
            try
            {
                Register(user, pass, confirmPass);
                return await _authenticationController.TryLogin(user, pass);
            }
            catch (ApiException e)
            {
                return new Result(false, string.Join("\n", e.Errors));
            }
        }

        public async Task<UserInfoModel> GetUserInfo()
        {
            return await _connection.GetItem<UserInfoModel>("api/Account/UserInfo");
        }
    }

    /// <summary>
    /// The model used to register a new user to the system
    /// </summary>
    public class RegisterModel
    {
        /// <summary>
        /// the email of the new user
        /// </summary>
        public string Email;

        /// <summary>
        /// The password of the new user
        /// </summary>
        public string Password;

        /// <summary>
        /// The confirmed password of the new user
        /// </summary>
        public string ConfirmPassword;
    }

    public class UserInfoModel
    {
        /// <summary>
        /// the email of the new user
        /// </summary>
        public string Email;

        /// <summary>
        /// Whether or not the user has registered
        /// </summary>
        public bool HasRegistered;

        /// <summary>
        /// The provider of the login for the user
        /// </summary>
        public string LoginProvider;
    }
}
