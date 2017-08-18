using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Dash;
using DashShared;
using System;

namespace Dash
{
    /// <summary>
    /// The data field for a collection is a document collection field model controller
    /// </summary>
    public class CollectionBox : CourtesyDocument
    {
        public static KeyController CollectionViewKey = new KeyController("FA7002C4-0EB2-44F7-8D25-ECA5C618D10C", "Collection View Key");
        public static DocumentType DocumentType = new DocumentType("7C59D0E9-11E8-4F12-B355-20035B3AC359", "Collection Box");
        private static string PrototypeId = "E1F828EA-D44D-4C3C-BE22-9AAF369C3F19";

        public CollectionBox(FieldModelController refToCollection, double x = 0, double y = 0, double w = 400, double h = 400)
        {
            var fields = DefaultLayoutFields(new Point(x, y), new Size(w, h), refToCollection);
            Document = GetLayoutPrototype().MakeDelegate();
            Document.SetFields(fields, true);
            Document.SetField(InkBox.InkDataKey, new InkFieldModelController(), true);
        }

        protected override DocumentController GetLayoutPrototype()
        {
            var prototype = ContentController.GetController<DocumentController>(PrototypeId);
            if (prototype == null)
            {
                prototype = InstantiatePrototypeLayout();
            }
            return prototype;
        }

        protected override DocumentController InstantiatePrototypeLayout()
        {
            var docFieldModelController = new DocumentCollectionFieldModelController(new List<DocumentController>());
            var fields = DefaultLayoutFields(new Point(), new Size(double.NaN, double.NaN), docFieldModelController);
            fields[KeyStore.IconTypeFieldKey] = new NumberFieldModelController((int)IconTypeEnum.Collection); // TODO factor out into SetIconField() method in base class
            var prototypeDocument = new DocumentController(fields, DocumentType, PrototypeId);
            return prototypeDocument;
        }

        public override FrameworkElement makeView(DocumentController docController,
            Context context, bool isInterfaceBuilderLayout = false)
        {
            return MakeView(docController, context, null, isInterfaceBuilderLayout);
        }

        public static FrameworkElement MakeView(DocumentController docController,
            Context context, DocumentController dataDocument, bool isInterfaceBuilderLayout = false)
        {
            var data = docController.GetField(KeyStore.DataKey);

            var opacity = (docController.GetDereferencedField(new KeyController("opacity", "opacity"), context) as NumberFieldModelController)?.Data;

            double opacityValue = opacity.HasValue ? (double)opacity : 1;

            var collectionFieldModelController = data.DereferenceToRoot<DocumentCollectionFieldModelController>(context);
            Debug.Assert(collectionFieldModelController != null);

            var collectionViewModel = new CollectionViewModel(data, isInterfaceBuilderLayout, context) {InkFieldModelController = docController.GetField(InkBox.InkDataKey) as InkFieldModelController};

            var view = new CollectionView(collectionViewModel, (docController.GetDereferencedField(CollectionViewKey,context) as TextFieldModelController)?.Data == "Text" ? CollectionView.CollectionViewType.Text : CollectionView.CollectionViewType.Freeform);

            if (context.DocContextList.FirstOrDefault().DocumentType != MainPage.MainDocumentType)
            {
                SetupBindings(view, docController, context);
            }

            view.Opacity = opacityValue;
            if (isInterfaceBuilderLayout)
            {
                SelectableContainer container = new SelectableContainer(view, docController, dataDocument);
                //SetupBindings(container, docController, context);
                return container;
            }
            return view;
        }
    }
}