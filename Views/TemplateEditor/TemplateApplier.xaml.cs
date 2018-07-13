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
using Dash.Controllers;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class TemplateApplier : UserControl
    {
        public ObservableCollection<DocumentViewModel> Templates;
        public ObservableCollection<TemplateRecord> TemplateRecords;
        private DocumentController _document;

        public TemplateApplier(DocumentController doc,
            ObservableCollection<DocumentViewModel> documentViewModels)
        {
            this.InitializeComponent();

            _document = doc;
            Templates = new ObservableCollection<DocumentViewModel>();
            TemplateRecords = new ObservableCollection<TemplateRecord>();

            foreach (var dvm in documentViewModels)
            {
                if (dvm.LayoutDocument.DocumentType.Equals(TemplateBox.DocumentType) && !Templates.Contains(dvm))
                {
                    var tr = new TemplateRecord(dvm, this);
                    TemplateRecords.Add(tr);
                    tr.Tapped += Template_Picked;
                }
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
            var template = tr.TemplateViewModel;
            if (template == null) return;
            var newLayoutDoc = template.LayoutDocument.GetDataInstance();

            // set the new layout document's context to the selected document's data doc
            newLayoutDoc.SetField(KeyStore.DocumentContextKey, _document.GetDataDocument(), true);
            // set the position to match the old position
            newLayoutDoc.SetField(KeyStore.PositionFieldKey,
                _document.GetField<PointController>(KeyStore.PositionFieldKey), true);
            // set the selected document's active layout to the new layout document
            _document.SetField(KeyStore.ActiveLayoutKey, newLayoutDoc, true);
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
