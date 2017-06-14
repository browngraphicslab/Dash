using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using DashShared;

namespace Dash
{
    public class ReferenceFieldModel : FieldModel
    {
        /// <summary>
        /// ID of document that this FieldModel references
        /// </summary>
        public string DocId { get; set; }

        /// <summary>
        /// Key of field within document that is referenced
        /// </summary>
        public Key FieldKey { get; set; }

        /// <summary>
        /// Cached type of field
        /// </summary>
        public string Type { get; set; }
        
        public override UIElement MakeView(TemplateModel template)
        {
            throw new NotImplementedException();
        }

        public override bool Equals(object obj)
        {
            ReferenceFieldModel refFieldModel = obj as ReferenceFieldModel;
            if (refFieldModel == null)
            {
                return false;
            }

            return refFieldModel.DocId.Equals(DocId) && refFieldModel.FieldKey.Equals(FieldKey);
        }

        public override int GetHashCode()
        {
            return DocId.GetHashCode() ^ FieldKey.GetHashCode();
        }
    }
}
