using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Dash.Annotations;

namespace Dash
{
    public sealed class InkManager : INotifyPropertyChanged
    {
        private InkCanvas _currentInkCanvas;
        private InkSubToolbar _toolBar;
        private bool _onMainPage = false;

        public InkCanvas CurrentInkCanvas
        {
            get => _currentInkCanvas;
            set
            {
                _currentInkCanvas = value;
                OnPropertyChanged();
            }
        }

        public InkManager()
        {
            _toolBar = new InkSubToolbar(this);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void ShowToolbar()
        {
            if (!_onMainPage)
            {
                MainPage.Instance.xCanvas.Children.Add(_toolBar);
                _onMainPage = true;
                // hack until i figure it out lol
                if (SplitFrame.ActiveFrame.ViewModel.Content is CollectionView collection)
                {
                    if (collection.CurrentView is CollectionFreeformBase cfv)
                    {
                        CurrentInkCanvas = cfv.XInkCanvas;
                    }
                }
            }
        }

        public void HideToolbar()
        {
            if (_onMainPage)
            {
                MainPage.Instance.xCanvas.Children.Remove(_toolBar);
                _onMainPage = false;
            }
        }
    }
}
