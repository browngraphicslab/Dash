using DashShared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public interface IPushController
    {
        void PushUpdate(EntityBase model);
        void PushDelete(EntityBase model);
    }
}
