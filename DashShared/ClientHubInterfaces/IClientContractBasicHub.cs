using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DashShared
{

    /// <summary>
    /// Encapsulates the methods that the client has to respond to
    /// </summary>
    public interface IClientContractBasicHub
    {
        // All these methods must return void
        void SomeInformation();
        void SomeInformationWithParam(int id);
        void SomeInformationWithText(string text);
    }
}
