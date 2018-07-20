using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    interface ILinkHandler
    {
        bool HandleLink(DocumentController linkDoc);
    }
}
