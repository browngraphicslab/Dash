using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    class Search
    {
        public static IEnumerable<DocumentController> GetAllDocs()
        {
            ContentController<FieldModel>.GetControllers<DocumentController>();
        }
    }
}
