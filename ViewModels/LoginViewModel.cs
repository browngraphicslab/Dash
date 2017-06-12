using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

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

        /// <summary>
        /// Try to log the user in
        /// </summary>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <param name="rememberLogin"></param>
        /// <returns></returns>
        public async Task<Result> TryLogin(string user, string password, bool rememberLogin)
        {
            //TODO comment out the below lines
            ////TODO hookup the rememberLogin to the LocalCredentialHelper
            //var result = await _authenticationController.TryLogin(user, password);

            //return result;
            return new Result(true);
        }

        /// <summary>
        /// Try to register the user, which will in turn cause the user to be logged in
        /// </summary>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <param name="confirmPassword"></param>
        /// <param name="rememberLogin"></param>
        /// <returns></returns>
        public async Task<Result> TryRegister(string user, string password, string confirmPassword, bool rememberLogin)
        {
            //TODO comment out the below lines
            ////TODO hookup the rememberLogin to the LocalCredentialHelper
            //var result = await _accountController.TryRegister(user, password, confirmPassword);

            //return result;
            return new Result(true);
        }
    }
}
