﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;

namespace Dash.ViewModels
{
    public class OperatorDocumentViewModel : DocumentViewModel
    {
        public OperatorDocumentViewModel(OperatorDocumentModel document) : base(document)
        {
        }

        /// <summary>
        /// Overrides DocumentViewModel.GetUiElements to just return the operators view
        /// and listens to the operator view's IO event
        /// </summary>
        /// <returns></returns>
        public override List<UIElement> GetUiElements()
        {
            List<UIElement> elements = new List<UIElement>();
            LayoutModel model = GetLayoutModel();
            OperatorView view = DocumentModel.Field(OperatorDocumentModel.OperatorKey).MakeView(model.Fields[OperatorDocumentModel.OperatorKey]) as OperatorView;
            Debug.Assert(view != null);
            view.IoDragStarted += View_IODragStarted;
            view.IoDragEnded += View_IoDragEnded;
            elements.Add(view);
            return elements;
        }

        private void View_IoDragEnded(OperatorView.IOReference ioReference)
        {
            IODragEnded?.Invoke(ioReference);
        }

        private void View_IODragStarted(OperatorView.IOReference ioReference)
        {
            IODragStarted?.Invoke(ioReference);
        }

        public event OperatorView.IODragEventHandler IODragStarted;
        public event OperatorView.IODragEventHandler IODragEnded;
    }
}
