using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DashShared
{

    /// <summary>
    /// Encapsulate the hub methods that the server has to respond to 
    /// </summary>
    public interface IServerContractBasicHub
    {
        void DoSomething();
        void DoSomethingWithParam(int id);
        Task DoSomethingAsync();
        int DoSomethingWithParamAndResult(int id);
    }
}
