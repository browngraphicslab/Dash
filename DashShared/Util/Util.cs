using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DashShared
{
    public static class Util
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
    }
}
