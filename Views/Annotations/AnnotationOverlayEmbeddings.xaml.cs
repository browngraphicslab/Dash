﻿using Dash.Annotations;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using NewControls.Geometry;
using static Dash.DataTransferTypeInfo;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class AnnotationOverlayEmbeddings : UserControl, ILinkHandler
    {
        public readonly DocumentController MainDocument;
        public AnnotationOverlay                       AnnotationOverlay;
        public ListController<DocumentController>      EmbeddedDocsList; // shortcut to the embedded documents stored in the EmbeddedDocs Key
        public ObservableCollection<DocumentViewModel> EmbeddedViewModels { get; set; } = new ObservableCollection<DocumentViewModel>();
        public AnnotationOverlayEmbeddings([NotNull] AnnotationOverlay annotationOverlay)
        {
            AnnotationOverlay = annotationOverlay;
            InitializeComponent();
            
            EmbeddedDocsList = AnnotationOverlay.MainDocument.GetDataDocument().GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.EmbeddedDocumentsKey);
            AnnotationOverlay.MainDocument.GetDataDocument().AddWeakFieldUpdatedListener(this, KeyStore.EmbeddedDocumentsKey, (view, controller, arge) => view.embeddedDocsListOnFieldModelUpdated(controller, arge));
            embeddedDocsListOnFieldModelUpdated(null,
                new DocumentController.DocumentFieldUpdatedEventArgs(null, null, DocumentController.FieldUpdatedAction.Update, null,
               new ListController<DocumentController>.ListFieldUpdatedEventArgs(ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Add, EmbeddedDocsList.ToList(), new List<DocumentController>(), 0), false));
        }
        public LinkHandledResult HandleLink(DocumentController linkDoc, LinkDirection direction)
        {
            if (linkDoc.GetDataDocument().GetLinkBehavior() == LinkBehavior.Overlay) {
                DocumentView target = null;
                if (EmbeddedDocsList.Contains(linkDoc.GetDataDocument().GetField<DocumentController>(KeyStore.LinkSourceKey)))
                {
                    var dest = linkDoc.GetDataDocument().GetField<DocumentController>(KeyStore.LinkDestinationKey);
                    target = this.GetDescendantsOfType<DocumentView>().FirstOrDefault(dv => dv.ViewModel.DataDocument.Equals(dest.GetDataDocument()));
                }
                if (EmbeddedDocsList.Contains(linkDoc.GetDataDocument().GetField<DocumentController>(KeyStore.LinkDestinationKey)))
                {
                    var src = linkDoc.GetDataDocument().GetField<DocumentController>(KeyStore.LinkSourceKey);
                    target = this.GetDescendantsOfType<DocumentView>().FirstOrDefault(dv => dv.ViewModel.DataDocument.Equals(src.GetDataDocument()));
                }
                if (target != null)
                {
                    target.ViewModel.LayoutDocument.ToggleHidden();
                    return LinkHandledResult.HandledClose;
                }
                this.GetDocumentView().ViewModel.SetSearchHighlightState(null); // toggles highlight
                return LinkHandledResult.HandledClose;
            }

            return LinkHandledResult.Unhandled;
        }
        private void embeddedDocsListOnFieldModelUpdated(FieldControllerBase fieldControllerBase, FieldUpdatedEventArgs args)
        {
            if (args is DocumentController.DocumentFieldUpdatedEventArgs dargs && 
                dargs.FieldArgs is ListController<DocumentController>.ListFieldUpdatedEventArgs listArgs)
            {
                switch (listArgs.ListAction)
                {
                    case ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Add:
                        listArgs.NewItems.ForEach(reg => EmbeddedViewModels.Add(new DocumentViewModel(reg) { ResizersVisible = true }));
                        break;
                    case ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Remove:
                        listArgs.OldItems.ForEach(removedDoc =>
                            EmbeddedViewModels.Where(em => em.DocumentController.Equals(removedDoc)).ToList().ForEach(em => EmbeddedViewModels.Remove(em)));
                        break;
                }
            }
        }
    }
}
