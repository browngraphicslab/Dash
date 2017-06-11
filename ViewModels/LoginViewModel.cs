using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public class LoginViewModel : ViewModelBase
    {
        /// <summary>
        /// The authentication controller is used to check if the passed in
        /// username and password are valid
        /// </summary>
        private readonly AuthenticationController _authenticationController;

        /// <summary>
        /// The account controller is used to register new accounts, change passwords etc.
        /// </summary>
        private readonly AccountController _accountController;

        public LoginViewModel(AuthenticationController authenticationController, AccountController accountController)
        {
            _authenticationController = authenticationController;
            _accountController = accountController;
        }

        public async Task<Result> TryLogin(string user, string password)
        {
            var result = await _authenticationController.TryLogin(user, password);


            return result;
        }

        public async Task<Result> TryRegister(string user, string password, string confirmPassword)
        {
            var result = await _accountController.TryRegister(user, password, confirmPassword);


            return result;
        }
    }
}
