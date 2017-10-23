using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public interface TabItemViewModel
    {
        string Title { get; set; }

        void ExecuteFunc(); 

    }
}
