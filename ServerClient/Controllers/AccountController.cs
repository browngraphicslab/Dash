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

        public AccountController(ServerController connection)
        {
            _connection = connection;
        }

        /// <summary>
        /// Registers a new user with the passed in email and password
        /// </summary>
        /// <param name="email"></param>
        /// <param name="pass"></param>
        public void Register(string email, string pass)
        {
            var user = new RegisterModel()
            {
                Email = email,
                Password = pass,
                ConfirmPassword = pass
            };

            var result = _connection.Post("api/Account/Register", user);

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
