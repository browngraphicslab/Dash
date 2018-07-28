using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Animation;

namespace Dash
{
    public class PresentationViewModel : ViewModelBase
    {
        public ObservableCollection<DocumentController> PinnedNodes
        {
            get => _pinnedNodes;
            set => SetProperty(ref _pinnedNodes, value);
        }

        public ObservableCollection<PresentationNumberViewModel> PinNumbers
        {
            get => _pinNumbers;
            set => SetProperty(ref _pinNumbers, value);
        }

        private ListController<DocumentController> _listController = null;
        private ObservableCollection<DocumentController> _pinnedNodes = new ObservableCollection<DocumentController>();
        private ObservableCollection<PresentationNumberViewModel> _pinNumbers = new ObservableCollection<PresentationNumberViewModel>();
        private readonly List<EventHandler<object>> _onCompleted = new List<EventHandler<object>>();

        public PresentationViewModel() { }
        
        public PresentationViewModel(ListController<DocumentController> lc)
        {
            _listController = lc;
            PinnedNodes = new ObservableCollection<DocumentController>(_listController.TypedData);
            for (var i = 1; i <= PinnedNodes.Count; i++)
            {
                PinNumbers.Add(new PresentationNumberViewModel(i));
            }
        }

        public void AddToPinnedNodesCollection(DocumentController dc)
        {
            if (_listController == null)
            {
                _listController = new ListController<DocumentController>();
                MainPage.Instance.MainDocument.SetField(KeyStore.PresentationItemsKey, _listController, true);
            }

            //if (PinnedNodes.Contains(dc)) return;

            if (PinnedNodes.Count == 0)
            {
                Storyboard helpOut = MainPage.Instance.xPresentationView.xHelpOut;

                void Completed(object o, object sender) => HelpOutOnCompleted(o, sender, dc);
                _onCompleted.Add(Completed);
                helpOut.Completed += Completed;

                helpOut.Begin();
            }
            else
            {
                PinnedNodes.Add(dc);
                PinNumbers.Add(new PresentationNumberViewModel(PinnedNodes.Count));
                _listController.Add(dc);
            }

            MainPage.Instance.xPresentationView.TryPlayStopClick();
        }

        private void HelpOutOnCompleted(object sender, object o, DocumentController dc = null)
        {
            PresentationView presView = MainPage.Instance.xPresentationView;
            presView.xHelpPrompt.Visibility = Visibility.Collapsed;

            PinnedNodes.Add(dc);
            PinNumbers.Add(new PresentationNumberViewModel(PinnedNodes.Count));
            _listController.Add(dc);

            foreach (var handler in _onCompleted)
            {
                presView.xHelpOut.Completed -= handler;
            }
            _onCompleted.Clear();
        }

        public async void RemovePinFromPinnedNodesCollection(DocumentController dc)
        {
            MainPage.Instance.xPresentationView.TryPlayStopClick();

            PinNumbers.RemoveAt(PinnedNodes.Count - 1);
            PinnedNodes.Remove(dc);
            _listController.Remove(dc);
            dc.SetField(KeyStore.PresentationTitleKey, null, true);

            if (PinnedNodes.Count > 0) return;

            PresentationView presView = MainPage.Instance.xPresentationView;
            presView.xHelpPrompt.Visibility = Visibility.Visible;

            if (MainPage.Instance.CurrPresViewState == MainPage.PresentationViewState.Collapsed) return;

            await Task.Delay(550);
            presView.xHelpIn.Begin();
        }
    }
}
