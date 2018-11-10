using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Animation;
using Dash.Annotations;

namespace Dash
{
    public class PresentationViewModel : ViewModelBase
    {
        //list of doc-controllers that are pinned to the current presentation
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

        //Current presentation that the user has selected
        public DocumentController CurrPres
        {
            get => _currPres;
            set
            {
                _currPres = value;
                //update Pinned nodes
                _pinnedNodes.Clear();
                foreach(var node in _currPres.GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null))
                {
                    _pinnedNodes.Add(node);
                }
                //update PinnedNumbers
                _pinNumbers.Clear();
                for (var i = 1; i <= _pinnedNodes.Count; i++)
                {
                    _pinNumbers.Add(new PresentationNumberViewModel(i));
                }
                //update ComboBox selection accordingly
                MainPage.Instance.xPresentationView.xPresentations.SelectedItem = value;
            }
        }

        public ObservableCollection<DocumentController> Presentations
        {
            get => _presentations;
            set => SetProperty(ref _presentations, value);
        }

        private ListController<DocumentController> _listController;
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
            if (lc == null)
            {
                //list of presentations
                _listController = new ListController<DocumentController>();
                MainPage.Instance.MainDocument.GetDataDocument().SetField(KeyStore.PresentationItemsKey, _listController, true);
           
                //make default presentation
                SetCurrentPresentation(MakeNewPres());
            }
            else
            {
                _listController = lc;

                //check if there's a pres & make default pres if not
                if (_listController[0] == null)
                {
                    SetCurrentPresentation(this.MakeNewPres());
                }
                else  //default presentation is first one
                {
                    SetCurrentPresentation(_listController[0]);
                }
            }
            
            //make list of presentations
            foreach (DocumentController pres in _listController)
            {
                if (!Presentations.Contains(pres)) Presentations.Add(pres);
            }
        }

        //Add passed-in document controller to the current presentation
        public void AddToPinnedNodesCollection(DocumentController dc, DocumentController parentPres = null)
        {
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
                var pres = parentPres ?? CurrPres;
                pres.GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null).Add(dc);
                if (pres == CurrPres)
                {
                    PinnedNodes.Add(dc);
                    PinNumbers.Add(new PresentationNumberViewModel(PinnedNodes.Count));
                }
            }

            MainPage.Instance.xPresentationView.TryPlayStopClick();
        }

        //deals with the first document controller added to the presentation
        private void HelpOutOnCompleted(object sender, object o, DocumentController dc = null)
        {
            PresentationView presView = MainPage.Instance.xPresentationView;
            presView.xHelpPrompt.Visibility = Visibility.Collapsed;

            CurrPres.GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null).Add(dc);
            PinnedNodes.Add(dc);
            PinNumbers.Add(new PresentationNumberViewModel(PinnedNodes.Count));

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
            pres.GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null).Remove(dc);
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
            if (_listController.Contains(pres))
            {
                CurrPres = pres;
            }
            else
            {
                _listController.Add(pres);
                this.SetCurrentPresentation(pres);
            }
        }

        /// <summary>
        /// Makes and returns a new presentation
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        public DocumentController MakeNewPres(string title = "New Presentation")
        {
            var pres = new CollectionNote(new Point(), CollectionViewType.Freeform).Document;
            pres.GetDataDocument().SetTitle(title);
            _listController.Add(pres);
            _presentations.Add(pres);

            return pres;
        }

        public void RenamePres(DocumentController pres, string newName)
        {
            pres.SetTitle(newName);
        }
    }
}
