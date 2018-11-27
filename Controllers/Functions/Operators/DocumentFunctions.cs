using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash.Controllers.Functions.Operators
{
    public static class DocumentFunctions
    {
        public static DocumentController Instance(DocumentController doc)
        {
            return doc.GetDataInstance();
        }

        public static DocumentController ViewCopy(DocumentController doc)
        {
            return doc.GetViewCopy();
        }

        [OperatorFunctionName("copy")]
        public static DocumentController DocumentCopy(DocumentController doc)
        {
            return doc.GetCopy();
        }
    }
}
