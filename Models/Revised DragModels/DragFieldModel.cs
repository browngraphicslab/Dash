using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;

// ReSharper disable once CheckNamespace
namespace Dash {

    public class DragFieldModel : DragModelBase
    {
        public List<DocumentFieldReference> DraggedRefs { get; }

        public DragFieldModel(List<DocumentFieldReference> draggedRefs) => DraggedRefs = draggedRefs;

        public DragFieldModel(params DocumentFieldReference[] draggedRefs) => DraggedRefs = draggedRefs.ToList();

        public override List<DocumentController> GetDropDocuments(Point where, Windows.UI.Xaml.FrameworkElement target)
        {
            var dropDocuments = DraggedRefs.Select(RefToDBox).ToList();

            DocumentController RefToDBox(DocumentFieldReference reference)
            {
                var dbox = new DataBox(reference.GetReferenceController(), where.X, where.Y).Document;

                if (reference.FieldKey != null)
                {
                    dbox.Tag = $"Dragged Field Doc => Key: {reference.FieldKey.Name}";
                    dbox.SetTitle(reference.FieldKey.Name);
                }
                reference.GetReferenceController().GetDocumentController(null).Link(dbox, LinkBehavior.Annotate, "KeyValue");

                return dbox;
            }

            return dropDocuments;
        }
    }
}
