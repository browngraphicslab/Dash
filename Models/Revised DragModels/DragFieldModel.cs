// ReSharper disable once CheckNamespace
using System.Collections.Generic;
using Windows.Foundation;

// ReSharper disable once CheckNamespace
namespace Dash {

    public class DragFieldModel : DragModelBase
    {
        public List<KeyController> DraggedKeys { get; }

        public DragFieldModel(List<DocumentController> draggedDocuments, List<KeyController> draggedKeys = null) : base(draggedDocuments) => DraggedKeys = draggedKeys;

        public override List<DocumentController> GetDropDocuments(Point where, bool forceShowViewCopy = false)
        {
            var dropDocuments = new List<DocumentController>();

            for (var i = 0; i < DraggedDocuments.Count; i++)
            {
                DocumentController document = DraggedDocuments[i];
                KeyController key = DraggedKeys?[i];

                FieldControllerBase field = key == null ? document : new DocumentReferenceController(document, DraggedKeys[i]) as FieldControllerBase;

                DocumentController dbox = new DataBox(field, where.X, where.Y).Document;
                dbox.SetField(KeyStore.DocumentContextKey, document, true);

                if (key != null)
                {
                    dbox.Tag = $"Dragged Field Doc => Key: {key.Name}";
                    dbox.SetTitle(key.Name);
                }

                dropDocuments.Add(dbox);
            }

            return dropDocuments;
        }
    }
}
