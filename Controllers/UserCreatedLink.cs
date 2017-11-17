using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Shapes;

namespace Dash
{
    /// <summary>
    /// A path subclass that knows its 
    /// </summary>
    class UserCreatedLink:Path
    {
        public DocumentController referencingDocument;
        public DocumentController referencedDocument;
        public KeyController referencingKey;
        public KeyController referencedKey;
    }
}
