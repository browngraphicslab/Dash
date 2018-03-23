using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Dash
{
    public interface ICollectionView
    {
        CollectionViewModel ViewModel { get; }
    }
}
