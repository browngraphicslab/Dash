using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Shapes;

namespace Dash
{
    /// <summary>
    /// A path subclass that knows which document it references and which document it is referencing
    /// (it knows which documents are at the end of the links); allows easier manipulation of 
    /// references in editing existing links
    /// </summary>
    class UserCreatedLink:Path
    {
        public DocumentController referencingDocument;
        public DocumentController referencedDocument;
        public KeyController referencingKey;
        public KeyController referencedKey;
    }
}
