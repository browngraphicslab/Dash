using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI.Xaml.Controls;

namespace Dash
{
    public class SimpleCollectionViewModel : BaseCollectionViewModel
    {

        public SimpleCollectionViewModel(bool isInInterfaceBuilder) : base(isInInterfaceBuilder)
        {
            CellSize = 250;
            CanDragItems = true;
        }

        public override void AddDocuments(List<DocumentController> documents, Context context)
        {
            foreach (var docController in documents)
                AddDocument(docController, context);
        }

        public override void AddDocument(DocumentController document, Context context)
        {
            var docVm = new DocumentViewModel(document, IsInterfaceBuilder);
            DocumentViewModels.Add(docVm);
        }

        public override void RemoveDocuments(List<DocumentController> documents)
        {
            foreach (var doc in documents)
                RemoveDocument(doc);
        }

        public override void RemoveDocument(DocumentController document)
        {
            var vmToRemove = DocumentViewModels.FirstOrDefault(vm => vm.DocumentController.GetId() == document.GetId());
            if (vmToRemove != null)
                DocumentViewModels.Remove(vmToRemove);
        }
    }
}