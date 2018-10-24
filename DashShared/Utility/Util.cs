using System;
using System.Text;
using System.Security.Cryptography;
using Logos.Utility;

namespace DashShared
{
    public static class UtilShared
    {
        public static Guid DashNamespaceId { get; } = new Guid("45384BD5-731A-4DF2-9311-285633FE6FF2");

        /// <summary>
        /// Generates a new id using a consistent format which can be stored in the database. This is better than generating id's in 100 different places
        /// so use this
        /// </summary>
        /// <returns></returns>
        public static string GenerateNewId()
        {
            return Guid.NewGuid().ToString();
        }

        public static Guid GetDeterministicGuid(string input)
        {
            return GuidUtility.Create(DashNamespaceId, input);
        }
    }
}
