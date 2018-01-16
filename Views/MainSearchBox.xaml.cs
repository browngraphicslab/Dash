using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using DashShared;
using DashShared.Models;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class MainSearchBox : UserControl
    {

        public MainSearchBox()
        {
            this.InitializeComponent();
        }

        private async void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            // Only get results when it was a user typing, 
            // otherwise assume the value got filled in by TextMemberPath 
            // or the handler for SuggestionChosen.
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                //Set the ItemsSource to be your filtered dataset
                //sender.ItemsSource = dataset;


                //Search(sender, sender.Text.ToLower());
                var vms = LocalSearch(sender.Text.ToLower());
                var results = new ObservableCollection<SearchResultViewModel>(vms);
                sender.ItemsSource = results;
            }
        }


        private void AutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            // Set sender.Text. You can use args.SelectedItem to build your text string.
            if (args.SelectedItem is SearchResultViewModel resultVM)
            {
                sender.Text = resultVM.Title;
            }
        }


        private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion != null)
            {
                // User selected an item from the suggestion list, take an action on it here.
            }
            else
            {
                // Use args.QueryText to determine what to do.
            }
        }

        /// <summary>
        /// searches but only through the content controller
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="searchString"></param>
        /// <returns></returns>
        private IEnumerable<SearchResultViewModel> LocalSearch(string searchString)
        {
            var results = new List<SearchResultViewModel>();
            foreach (var documentController in ContentController<FieldModel>.GetControllers<DocumentController>())
            {
                foreach (var kvp in documentController.DocumentModel.Fields)
                {
                    var keySearch = ContentController<FieldModel>.GetController<FieldControllerBase>(kvp.Key).SearchForString(searchString);
                    var fieldSearch = ContentController<FieldModel>.GetController<FieldControllerBase>(kvp.Value).SearchForString(searchString);

                    string topText = null;
                    if (fieldSearch.StringFound)
                    {
                        topText = ContentController<FieldModel>.GetController<KeyController>(kvp.Key).Name;
                    }
                    else if (keySearch.StringFound)
                    {
                        topText = "Name Of Key: "+keySearch.RelatedString;
                    }

                    if (keySearch.StringFound || fieldSearch.StringFound)
                    {
                        var bottomText = (fieldSearch?.RelatedString ?? keySearch?.RelatedString)?.Replace('\n',' ').Replace('\t', ' ').Replace('\r', ' ');
                        var title = string.IsNullOrEmpty(documentController.Title) ? topText : documentController.Title;
                        results.Add(new SearchResultViewModel(title, bottomText ?? documentController.Id));
                    }
                }
            }
            return results;
            //ContentController<FieldModel>.GetControllers<DocumentController>().Where(doc => SearchKeyFieldIdPair(doc.DocumentModel.Fields, searchString))
        }

        /// <summary>
        /// the method in which we actually process the search and perform the db query
        /// </summary>
        /// <param name="searchString"></param>
        /// <returns></returns>
        private void Search(AutoSuggestBox sender, string searchString)
        {
            for (var i = 0; i < 10; i++)
            {
                //results.Add(new SearchResultViewModel("Title" + i, "id " + i));
            }

            RESTClient.Instance.Fields.GetDocumentsByQuery(new SearchQuery(GetQueryFunc(searchString)),
                async (RestRequestReturnArgs args) =>
                {
                    var results = new ObservableCollection<SearchResultViewModel>(args.ReturnedObjects.OfType<DocumentModel>().Select(DocumentToSearchResult));
                    sender.ItemsSource = results;
                }, null);
        }

        private SearchResultViewModel DocumentToSearchResult(DocumentModel doc)
        {
            if (doc == null)
            {
                return null;
            }
            return new SearchResultViewModel((ContentController<FieldModel>.GetController<DocumentController>(doc.Id)?.GetField(KeyStore.TitleKey) as TextController)?.Data ?? "", doc.Id);
        }

        private bool TextFieldContains(TextController field, string searchString)
        {
            if (field == null)
            {
                return false;
            }
            return field.Data.ToLower().Contains(searchString);
        }


        private bool KeyContains(KeyController key, string searchString)
        {
            if (key == null)
            {
                return false;
            }
            return key.Name.ToLower().Contains(searchString);
        }

        private bool SearchKeyFieldIdPair(KeyValuePair<string, string> keyFieldPair, string searchString)
        {
            return (ContentController<FieldModel>.GetController<FieldControllerBase>(keyFieldPair.Value).SearchForString(searchString)?.StringFound == true ||
                ContentController<FieldModel>.GetController<FieldControllerBase>(keyFieldPair.Key).SearchForString(searchString)?.StringFound == true);
        }

        private Func<FieldModel, bool> GetQueryFunc(string searchString)
        {
            return(fieldModel) =>
            {
                if (!(fieldModel is DocumentModel))
                {
                    return false;
                }
                var doc = (DocumentModel) fieldModel;
                //var docController = ContentController<FieldModel>.GetController<DocumentController>(doc.Id);
                //return docController.
                return doc.Fields.Any(i => SearchKeyFieldIdPair(i, searchString) != null);
            };
        }
    }
}
