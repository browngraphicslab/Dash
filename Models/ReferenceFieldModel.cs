using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Dash
{
    public class ReferenceFieldModel : FieldModel
    {
        /// <summary>
        /// ID of document that this FieldModel references
        /// </summary>
        public int DocId { get; set; } = -1;

        /// <summary>
        /// Key of field within document that is referenced
        /// </summary>
        public string FieldKey { get; set; }

        /// <summary>
        /// Cached type of field
        /// </summary>
        public string Type { get; set; }
        
        public override UIElement MakeView(TemplateModel template)
        {
            throw new NotImplementedException();
        }
    }
}
