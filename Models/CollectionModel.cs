using System;
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
    /// <summary>
    /// Models a co
    /// </summary>
    public class CollectionModel
    {
        public ObservableCollection<DocumentModel> Documents;

        public CollectionModel(ObservableCollection<DocumentModel> documents)
        {
            Documents = documents;
        }

    }
}
