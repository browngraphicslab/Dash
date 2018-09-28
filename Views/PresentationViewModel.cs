using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Animation;
using Dash.Annotations;

namespace Dash
{
    public class PresentationViewModel : ViewModelBase
    {
        //public ObservableCollection<DocumentController> PinnedNodes
        //{
        //    get => _pinnedNodes;
        //    set => SetProperty(ref _pinnedNodes, value);
        //}

        //public ObservableCollection<PresentationNumberViewModel> PinNumbers
        //{
        //    get => _pinNumbers;
        //    set => SetProperty(ref _pinNumbers, value);
        //}

        public ObservableCollection<DocumentController> PinnedNodes
        {
            get => _currPres != null && _presToPinnedNodes.ContainsKey(_currPres) ? _presToPinnedNodes[_currPres] : null;
            set => _presToPinnedNodes[_currPres] = value;
        }

        public ObservableCollection<PresentationNumberViewModel> PinNumbers
        {
            get => _currPres != null && _presToPinNumbers.ContainsKey(_currPres) ? _presToPinNumbers[_currPres] : null;
            set => _presToPinNumbers[_currPres] = value;
        }

        private ListController<DocumentController> _listController = null;
        private ObservableDictionary<DocumentController, ObservableCollection<DocumentController>> _presToPinnedNodes = new ObservableDictionary<DocumentController, ObservableCollection<DocumentController>>();
        private ObservableDictionary<DocumentController, ObservableCollection<PresentationNumberViewModel>> _presToPinNumbers = new ObservableDictionary<DocumentController, ObservableCollection<PresentationNumberViewModel>>();
        private ObservableCollection<DocumentController> _pinnedNodes = new ObservableCollection<DocumentController>();
        private ObservableCollection<PresentationNumberViewModel> _pinNumbers = new ObservableCollection<PresentationNumberViewModel>();
        private readonly List<EventHandler<object>> _onCompleted = new List<EventHandler<object>>();
        private DocumentController _currPres;

        public PresentationViewModel() { }
        
        public PresentationViewModel(ListController<DocumentController> lc)
        {
            //list of presentations
            _listController = lc;

            //default presentation is first one
            if (_listController != null)
            {
                _currPres = _listController.TypedData[0];
            }

            foreach (DocumentController pres in lc)
            {
                //add pinned nodes of this presentation to the dictionary
                _presToPinnedNodes.Add(pres,
                    new ObservableCollection<DocumentController>(pres
                        .GetField<ListController<DocumentController>>(KeyStore.DataKey).TypedData));
                _presToPinNumbers.Add(pres, new ObservableCollection<PresentationNumberViewModel>());
                for (var i = 1; i <= PinnedNodes.Count; i++)
                {
                    PinNumbers.Add(new PresentationNumberViewModel(i));
                }
            }
        }

        public void AddToPinnedNodesCollection(DocumentController dc)
        {
            if (_listController == null)
            {
                //list of presentations
                _listController = new ListController<DocumentController>();
                MainPage.Instance.MainDocument.SetField(KeyStore.PresentationItemsKey, _listController, true);

                //make default presentation
                _currPres = new CollectionNote(new Point(), CollectionView.CollectionViewType.Freeform).Document;
                _listController.Add(_currPres);
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
                //_listController.Add(dc);
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
            _currPres.GetField<ListController<DocumentController>>(KeyStore.DataKey).TypedData.Remove(dc);
            dc.SetField(KeyStore.PresentationTitleKey, null, true);

            if (PinnedNodes.Count > 0) return;

            PresentationView presView = MainPage.Instance.xPresentationView;
            presView.xHelpPrompt.Visibility = Visibility.Visible;

            if (MainPage.Instance.CurrPresViewState == MainPage.PresentationViewState.Collapsed) return;

            await Task.Delay(550);
            presView.xHelpIn.Begin();
        }

        //change the current working presentation
        public void SetCurrentPresentation(DocumentController pres)
        {
            if (_listController.TypedData.Contains(pres))
            {
                _currPres = pres;
            } 
        }
    }
}
