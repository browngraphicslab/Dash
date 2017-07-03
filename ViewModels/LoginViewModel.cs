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
        private readonly AuthenticationEndpoint _authenticationEndpoint;

        /// <summary>
        /// The account controller is used to register new accounts, change passwords etc.
        /// </summary>

        private readonly AccountEndpoint _accountEndpoint;

        public LoginViewModel(AuthenticationEndpoint authenticationEndpoint, AccountEndpoint accountEndpoint)
        {
            _authenticationEndpoint = authenticationEndpoint;
            _accountEndpoint = accountEndpoint;
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
            //TODO hookup the rememberLogin to the LocalCredentialHelper
            //var result = await _authenticationEndpoint.TryLogin(user, password);

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
            ////TODO hookup the rememberLogin to the LocalCredentialHelper
            //var result = await _accountEndpoint.TryRegister(user, password, confirmPassword);

            //return result;
            return new Result(true);
        }
    }
}
