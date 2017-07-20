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
        public PushType PushType;
        public object Model;
        public Type Type;
    }
}
