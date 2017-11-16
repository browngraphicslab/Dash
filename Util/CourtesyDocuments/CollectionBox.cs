using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Dash;
using DashShared;
using System;
using DashShared.Models;

namespace Dash
{
    /// <summary>
    /// The data field for a collection is a document collection field model controller
    /// </summary>
    public class CollectionBox : CourtesyDocument
    {
        private static string PrototypeId = "E1F828EA-D44D-4C3C-BE22-9AAF369C3F19";

        public CollectionBox(FieldControllerBase refToCollection, double x = 0, double y = 0, double w = double.NaN, double h = double.NaN, CollectionView.CollectionViewType viewType = CollectionView.CollectionViewType.Freeform)
        {
            var fields = DefaultLayoutFields(new Point(x, y), new Size(w, h), refToCollection);
            fields[KeyStore.CollectionViewTypeKey] = new TextController(viewType.ToString());
            fields[KeyStore.InkDataKey] = new InkController();


            Document = GetLayoutPrototype().MakeDelegate();
            Document.SetFields(fields, true);
            
        }

        protected override DocumentController GetLayoutPrototype()
        {
            var prototype = ContentController<FieldModel>.GetController<DocumentController>(PrototypeId);
            if (prototype == null)
            {
                prototype = InstantiatePrototypeLayout();
            }
            return prototype;
        }

        protected override DocumentController InstantiatePrototypeLayout()
        {
            var docFieldModelController = new ListController<DocumentController>(new List<DocumentController>());
            var fields = DefaultLayoutFields(new Point(), new Size(double.NaN, double.NaN), docFieldModelController);
            fields[KeyStore.IconTypeFieldKey] = new NumberController((int)IconTypeEnum.Collection); // TODO factor out into SetIconField() method in base class

            var prototypeDocument = new DocumentController(fields, DashConstants.TypeStore.CollectionBoxType, PrototypeId);

            return prototypeDocument;
        }

        public override FrameworkElement makeView(DocumentController docController,
            Context context, bool isInterfaceBuilderLayout = false)
        {
            return MakeView(docController, context, null, null, isInterfaceBuilderLayout);
        }

        public static FrameworkElement MakeView(DocumentController docController,
            Context context, DocumentController dataDocument, Dictionary<KeyController, FrameworkElement> keysToFrameworkElementsIn = null, bool isInterfaceBuilderLayout = false)
        {
            var data = docController.GetField(KeyStore.DataKey);

            var opacity = (docController.GetDereferencedField(new KeyController("opacity", "opacity"), context) as NumberController)?.Data;

            double opacityValue = opacity.HasValue ? (double)opacity : 1;

            var collectionFieldModelController = data.DereferenceToRoot<ListController<DocumentController>>(context);
            Debug.Assert(collectionFieldModelController != null);

            var collectionViewModel = new CollectionViewModel(data, isInterfaceBuilderLayout, context) {InkFieldModelController = docController.GetField(KeyStore.InkDataKey) as InkController};

            var typeString = (docController.GetField(KeyStore.CollectionViewTypeKey) as TextController).Data;
            var viewType   = (CollectionView.CollectionViewType) Enum.Parse(typeof(CollectionView.CollectionViewType), typeString);
            var view       = new CollectionView(collectionViewModel,  viewType);

            //add to key to framework element dictionary
            var reference = data as ReferenceController;
            if (keysToFrameworkElementsIn != null)
            {
                keysToFrameworkElementsIn[reference.FieldKey] = view.ConnectionEllipseInput;
                keysToFrameworkElementsIn[KeyStore.CollectionOutputKey] = view.ConnectionEllipseOutput;
                //TODO Why are we doing this?
                docController.SetField(KeyStore.CollectionOutputKey,
                    new DocumentReferenceController(docController.GetId(), reference.FieldKey), true);
            }



            //if (context.DocContextList.FirstOrDefault().DocumentType != DashConstants.TypeStore.MainDocumentType &&
            //    context.DocContextList.FirstOrDefault().DocumentType != DashConstants.TypeStore.HomePageType)
            //{
                SetupBindings(view, docController, context);
            //}

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