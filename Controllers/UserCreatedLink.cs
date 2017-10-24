using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Shapes;

namespace Dash
{
    class UserCreatedLink:Path
    {
        public DocumentController referencingDocument;
        public FieldReference reference;
        public KeyController referencingKey;
    }
}
