using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Animation;
using Dash.Annotations;
using Syncfusion.Pdf;

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

        public DocumentController CurrPres
        {
            get => _currPres;
            set
            {
                _currPres = value;
                //update Pinned nodes
                _pinnedNodes.Clear();
                foreach(var node in _currPres.GetField<ListController<DocumentController>>(KeyStore.DataKey).TypedData)
                {
                    _pinnedNodes.Add(node);
                }
                
                _pinNumbers.Clear();
                //update PinnedNumbers
                for (var i = 1; i <= _pinnedNodes.Count; i++)
                {
                    _pinNumbers.Add(new PresentationNumberViewModel(i));
                }
            }
        }

        public ObservableCollection<DocumentController> Presentations
        {
            get => _presentations;
            set => SetProperty(ref _presentations, value);
        }

        public ObservableCollection<string> PresTitles
        {
            get => _presTitles;
        }

        //public ObservableCollection<DocumentController> PinnedNodes
        //{
        //    get => _currPres != null && _presToPinnedNodes.ContainsKey(_currPres) ? _presToPinnedNodes[_currPres] : new ObservableCollection<DocumentController>();
        //    set => _presToPinnedNodes[_currPres] = value;
        //}

        //public ObservableCollection<PresentationNumberViewModel> PinNumbers
        //{
        //    get => _currPres != null && _presToPinNumbers.ContainsKey(_currPres) ? _presToPinNumbers[_currPres] : null;
        //    set => _presToPinNumbers[_currPres] = value;
        //}


        private ListController<DocumentController> _listController = new ListController<DocumentController>();
        private ObservableDictionary<DocumentController, ObservableCollection<DocumentController>> _presToPinnedNodes = new ObservableDictionary<DocumentController, ObservableCollection<DocumentController>>();
        private ObservableDictionary<DocumentController, ObservableCollection<PresentationNumberViewModel>> _presToPinNumbers = new ObservableDictionary<DocumentController, ObservableCollection<PresentationNumberViewModel>>();
        private ObservableCollection<DocumentController> _presentations = new ObservableCollection<DocumentController>();
        private ObservableCollection<DocumentController> _pinnedNodes = new ObservableCollection<DocumentController>();
        private ObservableCollection<PresentationNumberViewModel> _pinNumbers = new ObservableCollection<PresentationNumberViewModel>();
        private ObservableCollection<string> _presTitles = new ObservableCollection<string>();
        private readonly List<EventHandler<object>> _onCompleted = new List<EventHandler<object>>();
        private DocumentController _currPres;

        
        /// <summary>
        /// Passed in a list of presentations
        /// </summary>
        /// <param name="lc"></param>
        public PresentationViewModel(ListController<DocumentController> lc = null)
        {
            //UPDATE TITLE LIST IF PRESENTATIONS IS CHANGED
            _presentations.CollectionChanged += (sender, args) =>
            {
                //remove if necessary
                if (args.OldItems != null)
                {
                    foreach (DocumentController item in args.OldItems)
                    {
                        _presTitles.Remove(item.Title);
                    }
                }
                //add new presentations to the list of titles (used for combobox selection)
                if (args.NewItems != null)
                {
                    foreach (DocumentController item in args.NewItems)
                    {
                        _presTitles.Add(item.Title);
                    }
                }
            };

            if (lc == null)
            {
                //list of presentations
                lc = new ListController<DocumentController>();
                MainPage.Instance.MainDocument.SetField(KeyStore.PresentationItemsKey, _listController, true);

                //make default presentation
                SetCurrentPresentation(MakeNewPres());

                _listController = lc;
            }
            else
            {
                _listController = lc;

                //check if there's a pres
                if (_listController.TypedData[0] == null)
                {
                    //make default pres if not
                    SetCurrentPresentation(this.MakeNewPres());
                }
                else
                {
                    //default presentation is first one
                    SetCurrentPresentation(_listController.TypedData[0]);
                }
            }


            //MAKE LIST OF PRESENTATIONS
            foreach (DocumentController pres in _listController)
            {
                //add to observable collection of presentations 
                _presentations.Add(pres);
            }

            
        }

        public void AddToPinnedNodesCollection(DocumentController dc, DocumentController parentPres = null)
        {
            if (_listController == null)
            {
                //list of presentations
                _listController = new ListController<DocumentController>();
                MainPage.Instance.MainDocument.SetField(KeyStore.PresentationItemsKey, _listController, true);

                //make default presentation
                CurrPres = new CollectionNote(new Point(), CollectionView.CollectionViewType.Freeform).Document;
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
                //TODO: ADD TO PRES DOC DATA
                var pres = parentPres ?? CurrPres;
                pres.GetField<ListController<DocumentController>>(KeyStore.DataKey).TypedData.Add(dc);
                if (pres == CurrPres)
                {
                    PinnedNodes.Add(dc);
                    PinNumbers.Add(new PresentationNumberViewModel(PinnedNodes.Count));
                }
                
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

        public async void RemovePinFromPinnedNodesCollection(DocumentController dc, DocumentController parentPres = null)
        {
            MainPage.Instance.xPresentationView.TryPlayStopClick();


            var pres = parentPres ?? CurrPres;
            pres.GetField<ListController<DocumentController>>(KeyStore.DataKey).TypedData.Remove(dc);
            if (dc == CurrPres)
            {
                PinNumbers.RemoveAt(PinnedNodes.Count - 1);
                PinnedNodes.Remove(dc);
            }
            dc.SetField(KeyStore.PresentationTitleKey, null, true);

            //TODO: MIGHT HAVE TO CHANGE BECAUSE FOLLOWING CODE ASSUMES PRESENTATION IS CURRPRES
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
                CurrPres = pres;
            }
            else
            {
                _listController.TypedData.Add(pres);
                this.SetCurrentPresentation(pres);
            }
        }

        /// <summary>
        /// Makes and returns a new presentation
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        public DocumentController MakeNewPres(string title = "Presentation Title")
        {
            var pres = new CollectionNote(new Point(), CollectionView.CollectionViewType.Freeform).Document;
            pres.SetField(KeyStore.DataKey, new ListController<DocumentController>(), true);
            pres.SetTitle(title);
            _listController.Add(pres);
            _presentations.Add(pres);

            //TODO: ADD TO TITLES LIST

            return pres;
        }
    }
}
