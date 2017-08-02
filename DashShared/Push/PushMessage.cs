using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DashShared
{
    public enum PushType
    {
        Create,
        Update,
        Delete
    }

    public class PushMessage
    {
        /// <summary>
        /// The type of the push.
        /// </summary>
        public PushType PushType;

        /// <summary>
        /// Needed for delete operations.
        /// </summary>
        public string Id;

        /// <summary>
        /// The model to push.
        /// </summary>
        public EntityBase Model;
    }
}
