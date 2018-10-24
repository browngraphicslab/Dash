using System;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class TemplateApplier : UserControl
    {
        public ObservableCollection<DocumentViewModel> Templates;
        public ObservableCollection<TemplateRecord> TemplateRecords;

        public ObservableCollection<TemplateRecord> Favorites;
        public ObservableCollection<DocumentController> AddedTemplateControllers;

        private DocumentController _document;

        public TemplateApplier(DocumentController doc)
        {
            this.InitializeComponent();

            //selected document
            _document = doc;
            Templates = new ObservableCollection<DocumentViewModel>();
            TemplateRecords = new ObservableCollection<TemplateRecord>();

            foreach (var template in MainPage.Instance.MainDocument.GetFieldOrCreateDefault<ListController<DocumentController>>(
                KeyStore.TemplateListKey))
            {
                var tr = new TemplateRecord(template, this);
                TemplateRecords.Add(tr);
                tr.Tapped += Template_Picked;
            }
        }

        private void Template_Picked(object sender, TappedRoutedEventArgs args)
        {
            foreach (var temp in TemplateRecords)
            {
                temp.hideButtons();
            }

            var tr = sender as TemplateRecord;
            tr.showButtons();


        }

        public void Apply_Template(TemplateRecord tr)
        {
            // retrieve the layout document of the template box from the template record
            var template = tr.Template;
            if (template == null) return;
            var newTemplate = MainPage.Instance.MainDocument
                .GetField<ListController<DocumentController>>(KeyStore.TemplateListKey)
                .First(temp => temp.Equals(template));
            SetTemplate(newTemplate.GetDataInstance());

            newTemplate.FieldModelUpdated +=
                delegate(FieldControllerBase sender, FieldUpdatedEventArgs args, Context context)
                {
                    if (MainPage.Instance.MainDocument
                            .GetField<ListController<DocumentController>>(KeyStore.TemplateListKey)
                            .First(temp => temp.Equals(template))
                            .GetField<ListController<DocumentController>>(KeyStore.DataKey).Count == 0)
                    {
                        // TODO: figure out a way to remove active layouts -sy
                        SetTemplate(newTemplate.GetDataInstance());
                    }
                    else
                    {
                        SetTemplate(newTemplate.GetDataInstance());
                    }
                };
                //template.LayoutDocument.GetDataInstance();
        }

        private void SetTemplate(DocumentController templateToSet)
        {
            // set the new layout document's context to the selected document's data doc
            templateToSet.SetField(KeyStore.DocumentContextKey, _document.GetDataDocument(), true);

            //foreach (var layoutDoc in templateToSet.GetField<ListController<DocumentController>>(KeyStore.DataKey)
            //    .TypedData)
            //{
            //    (layoutDoc.GetField<PointerReferenceController>(KeyStore.DataKey).DocumentReference as
            //        DocumentReferenceController).ChangeFieldDoc(_document.GetDataDocument());
            //}

            // set the position to match the old position
            templateToSet.SetField(KeyStore.PositionFieldKey,
                _document.GetField<PointController>(KeyStore.PositionFieldKey), true);
            // set the selected document's active layout to the new layout document
            _document.SetField(KeyStore.ActiveLayoutKey, templateToSet, true);
        }

        private void Search_Entered(object sender, TextChangedEventArgs textChangedEventArgs)
        {
            // when the text box text is changed, find all the matching template records
            var matchingItems = TemplateRecords.Where(tr =>
                tr.Title.Contains((sender as TextBox).Text, StringComparison.OrdinalIgnoreCase)).ToArray();
            // update the items source to matching items if there is anything in it
            // otherwise, use a new collection with one null template record
            xListView.ItemsSource = matchingItems.Any()
                ? matchingItems
                : new Collection<TemplateRecord>() {new TemplateRecord(null, this)}.ToArray();
        }
    }
}
