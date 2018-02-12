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
        public DragCollectionFieldModel(DocumentReferenceController collectionReference, KeyController fieldKey, CollectionView.CollectionViewType viewType)
        {
            _collectionReference = collectionReference;
            _collectionKey = fieldKey;
            _viewType = viewType;
        }

        public DocumentReferenceController CollectionReference { get => _collectionReference; }
        public KeyController FieldKey { get => _collectionKey; }
        public CollectionView.CollectionViewType ViewType { get => _viewType; }
    }
}
