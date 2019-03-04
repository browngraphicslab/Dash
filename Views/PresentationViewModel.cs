using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Animation;
using Dash.Annotations;

namespace Dash
{
    public class PresentationViewModel : ViewModelBase
    {

        public ObservableCollection<PresentationItemViewModel> PinnedNodes
        {
            get => _pinnedNodes;
            set => SetProperty(ref _pinnedNodes, value);
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
                var field = _currPres.GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null);
                for (var index = 0; index < field.Count; index++)
                {
                    var node = field[index];
                    _pinnedNodes.Add(new PresentationItemViewModel(node, index + 1));
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
        private ObservableDictionary<DocumentController, ObservableCollection<PresentationItemViewModel>> _presToPinNumbers = new ObservableDictionary<DocumentController, ObservableCollection<PresentationItemViewModel>>();
        private ObservableCollection<DocumentController> _presentations = new ObservableCollection<DocumentController>();
        private ObservableCollection<PresentationItemViewModel> _pinnedNodes = new ObservableCollection<PresentationItemViewModel>();
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
                    var fields = new List<KeyValuePair<KeyController, FieldControllerBase>>()
                    {
                        new KeyValuePair<KeyController, FieldControllerBase>(KeyStore.PresentationVisibleKey,
                            new BoolController(false)),
                        new KeyValuePair<KeyController, FieldControllerBase>(KeyStore.PresentationGroupUpKey,
                            new BoolController(false)),
                        new KeyValuePair<KeyController, FieldControllerBase>(KeyStore.PresentationFadeKey,
                            new BoolController(false)),
                        new KeyValuePair<KeyController, FieldControllerBase>(KeyStore.PresentationNavigateKey,
                            new BoolController(false)),
                        new KeyValuePair<KeyController, FieldControllerBase>(KeyStore.PresentationHideKey,
                            new BoolController(false))
                    };
                    dc.SetFields(fields, true);
                    PinnedNodes.Add(new PresentationItemViewModel(dc, PinnedNodes.Count));
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
            PinnedNodes.Add(new PresentationItemViewModel(dc, PinnedNodes.Count));

            foreach (var handler in _onCompleted)
            {
                presView.xHelpOut.Completed -= handler;
            }
            _onCompleted.Clear();
        }

        public void RemoveNode(PresentationItemViewModel item)
        {
            MainPage.Instance.xPresentationView.TryPlayStopClick();
            CurrPres.GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null).Remove(item.Document);
            PinnedNodes.Remove(item);
            item.Document.SetField(KeyStore.PresentationTitleKey, null, true);

            //TODO: MIGHT HAVE TO CHANGE BECAUSE FOLLOWING CODE ASSUMES PRESENTATION IS CURRPRES
            if (PinnedNodes.Count > 0) return;

            PresentationView presView = MainPage.Instance.xPresentationView;
            presView.xHelpPrompt.Visibility = Visibility.Visible;

            if (MainPage.Instance.xPresentationView.CurrPresViewState == PresentationView.PresentationViewState.Collapsed) return;

            presView.xHelpIn.Begin();
        }

        public bool RemovePinFromPinnedNodesCollection(DocumentController dc, DocumentController parentPres = null)
        {
            MainPage.Instance.xPresentationView.TryPlayStopClick();

            var pres = parentPres ?? CurrPres;
            var docs = pres.GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null);
            bool removed = true;
            // ReSharper disable once AssignmentInConditionalExpression
            while (removed &= docs.Remove(dc)) ;//TODO This should be RemoveAll
            if (dc == CurrPres)
            {
                PinnedNodes.Remove(PinnedNodes.First(vm => vm.Document == dc));
            }
            dc.SetField(KeyStore.PresentationTitleKey, null, true);

            //TODO: MIGHT HAVE TO CHANGE BECAUSE FOLLOWING CODE ASSUMES PRESENTATION IS CURRPRES
            if (PinnedNodes.Count > 0) return removed;

            PresentationView presView = MainPage.Instance.xPresentationView;
            presView.xHelpPrompt.Visibility = Visibility.Visible;

            if (MainPage.Instance.xPresentationView.CurrPresViewState == PresentationView.PresentationViewState.Collapsed) return removed;

            presView.xHelpIn.Begin();

            return removed;
        }

        //change the current working presentation
        public void SetCurrentPresentation(DocumentController pres)
        {
            if (!_listController.Contains(pres))
            {
                _listController.Add(pres);
            }
            CurrPres = pres;
        }

        public void DeletePresentation(DocumentController pres)
        {
            if (_listController.Remove(pres))
            {
                SetCurrentPresentation(_listController.Any() ? _listController.First() : MakeNewPres());
                Presentations.Remove(pres);
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

        public void UpdateList()
        {
            //TODO This is pretty inefficient. It's probably ok for now because presentations should be short, 
            // but it is not elegant
            var docs = PinnedNodes.Select(pivm => pivm.Document);
            var presList = CurrPres.GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey);
            presList.Clear();
            presList.AddRange(docs);
        }
    }
}
