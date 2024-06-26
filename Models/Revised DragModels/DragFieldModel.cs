﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;

// ReSharper disable once CheckNamespace
namespace Dash {

    public class DragFieldModel : DragModelBase
    {
        public List<DocumentFieldReference> DraggedRefs { get; }

        public Dictionary<KeyController, FieldControllerBase> LayoutFields { get; set; }

        public DragFieldModel(List<DocumentFieldReference> draggedRefs) => DraggedRefs = draggedRefs;

        public DragFieldModel(params DocumentFieldReference[] draggedRefs) => DraggedRefs = draggedRefs.ToList();

        public override Task<List<DocumentController>> GetDropDocuments(Point? where, Windows.UI.Xaml.FrameworkElement target, bool dontMove = false)
        {
            var dropDocuments = DraggedRefs.Select(RefToDBox).ToList();

            DocumentController RefToDBox(DocumentFieldReference reference)
            {
                var type = reference.DereferenceToRoot(null);
                if (type is DocumentController docField)
                    return docField;
                var dbox = new DataBox(reference.GetReferenceController(), where?.X ?? 0, where?.Y ?? 0, type is TextController ? double.NaN : 300, type is TextController || type is ImageController ? double.NaN : 300).Document;
               
                foreach (var layoutfield in LayoutFields ?? new Dictionary<KeyController, FieldControllerBase>())
                {
                    dbox.SetField(layoutfield.Key, layoutfield.Value, true);
                }

                if (reference.FieldKey != null)
                {
                    dbox.Tag = $"Dragged Field Doc => Key: {reference.FieldKey.Name}";
                    dbox.SetTitle(reference.FieldKey.Name);
                }

                return dbox;
            }

            return Task.FromResult(dropDocuments);
        }
    }
}
