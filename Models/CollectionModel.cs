﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;

namespace Dash.Models
{
    public class CollectionModel
    {
        public ObservableCollection<DocumentController> Documents;
        public DocumentController Context;

        public CollectionModel(ObservableCollection<DocumentController> documents, DocumentController context)
        {
            Documents = documents;
            Context = context;
        }

    }
}
