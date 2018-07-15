using System.Collections.ObjectModel;

// ReSharper disable once CheckNamespace
namespace Dash
{
    public class DishReplViewModel : ViewModelBase
    {
        public ObservableCollection<ReplLineViewModel> Items { get; set; } = new ObservableCollection<ReplLineViewModel>();
    }
}
