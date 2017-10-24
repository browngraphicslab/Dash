using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace DashShared
{
    public static class UtilShared
    {
        /// <summary>
        /// Generates a new id using a consistent format which can be stored in the database. This is better than generating id's in 100 different places
        /// so use this
        /// </summary>
        /// <returns></returns>
        public static string GenerateNewId()
        {
            return Guid.NewGuid().ToString();
        }

        public static string GetDeterministicGuid(string input)
        {
            //use MD5 hash to get a 128 bit hash of the string:
            var md5 = MD5.Create();

            var inputBytes = Encoding.UTF8.GetBytes(input);

            var hashBytes = md5.ComputeHash(inputBytes);

            //generate a guid from the hash:
            var hashGuid = new Guid(hashBytes);

            return hashGuid.ToString();
        }
    }
}
