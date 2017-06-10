using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DashShared
{
    public interface IClientInterface
    {
        Task NewMessage(string message);
    }
}
