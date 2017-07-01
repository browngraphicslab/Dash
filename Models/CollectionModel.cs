using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;

namespace Dash
{
    public class CollectionModel
    {
        public DocumentCollectionFieldModel DocumentCollectionFieldModel;
        public DocumentController Context;

        public CollectionModel(DocumentCollectionFieldModel docCollectionFieldModel, DocumentController context)
        {
            DocumentCollectionFieldModel = docCollectionFieldModel;
            Context = context;
        }

    }
}
