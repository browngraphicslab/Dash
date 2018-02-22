using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash.Models.DragModels
{
    class DragCollectionFieldModel
    {

        DocumentReferenceController _collectionReference;
        KeyController _collectionKey;
        CollectionView.CollectionViewType _viewType;
        List<DocumentController> _draggedItems;
        double? _width;
        double? _height;
        public DragCollectionFieldModel(
            List<DocumentController> draggedItems,
            DocumentReferenceController collectionReference, 
            KeyController fieldKey, 
            CollectionView.CollectionViewType viewType,
            double ?width = null,
            double ?height = null
            )
        {
            _draggedItems = draggedItems;
            _collectionReference = collectionReference;
            _collectionKey = fieldKey;
            _viewType = viewType;
            _width = width;
            _height = height;
        }

        public DocumentReferenceController       CollectionReference { get => _collectionReference; }
        public KeyController                     FieldKey            { get => _collectionKey; }
        public List<DocumentController>          DraggedItems        { get => _draggedItems; }
        public CollectionView.CollectionViewType ViewType            { get => _viewType; }
        public double?                           Width => _width;
        public double?                           Height=> _height;
    }
}
