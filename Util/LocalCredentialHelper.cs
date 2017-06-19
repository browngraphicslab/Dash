using System.Collections.Generic;
using Windows.Security.Credentials;
using DashShared;

namespace Dash
{
    /// <summary>
    /// Helper class used to access locally stored credentials. Based on https://docs.microsoft.com/en-us/windows/uwp/security/credential-locker
    /// </summary>
    public static class LocalCredentialHelper
    {
        //TODO hook this up to something


        /// <summary>
        /// Tried to get all the possible credentials from the locker, returns null if no credentials were found
        /// </summary>
        /// <returns></returns>
        public static IReadOnlyList<PasswordCredential> GetCredentialsFromLocker()
        {
            var vault = new PasswordVault();
            var credentialList = vault.FindAllByResource(DashConstants.ResourceName);

            // if we found credentials return them otherwise return null
            if (credentialList.Count > 0)
            {
                return credentialList;
            }
            return null;
        }

    }
}
