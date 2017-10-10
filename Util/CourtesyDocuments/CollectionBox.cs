﻿using System.Collections.Generic;
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
        public static DocumentType DocumentType = new DocumentType("7C59D0E9-11E8-4F12-B355-20035B3AC359", "Collection Box");
        private static string PrototypeId = "E1F828EA-D44D-4C3C-BE22-9AAF369C3F19";

        public static KeyController CollectionViewTypeKey = new KeyController("EFC44F1C-3EB0-4111-8840-E694AB9DCB80", "Collection View Type");

        public CollectionBox(FieldModelController refToCollection, double x = 0, double y = 0, double w = double.NaN, double h = double.NaN, CollectionView.CollectionViewType viewType = CollectionView.CollectionViewType.Freeform)
        {
            var fields = DefaultLayoutFields(new Point(x, y), new Size(w, h), refToCollection);
            fields[CollectionViewTypeKey] = new TextFieldModelController(viewType.ToString());
            fields[InkBox.InkDataKey] = new InkFieldModelController();

            Document = GetLayoutPrototype().MakeDelegate();
            Document.SetFields(fields, true);
            
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
            return MakeView(docController, context, null, null, isInterfaceBuilderLayout);
        }

        public static FrameworkElement MakeView(DocumentController docController,
            Context context, DocumentController dataDocument, Dictionary<KeyController, FrameworkElement> keysToFrameworkElementsIn = null, bool isInterfaceBuilderLayout = false)
        {
            var data = docController.GetField(KeyStore.DataKey);

            var opacity = (docController.GetDereferencedField(new KeyController("opacity", "opacity"), context) as NumberFieldModelController)?.Data;

            double opacityValue = opacity.HasValue ? (double)opacity : 1;

            var collectionFieldModelController = data.DereferenceToRoot<DocumentCollectionFieldModelController>(context);
            Debug.Assert(collectionFieldModelController != null);

            var collectionViewModel = new CollectionViewModel(data, isInterfaceBuilderLayout, context) {InkFieldModelController = docController.GetField(InkBox.InkDataKey) as InkFieldModelController};

            var typeString = (docController.GetField(CollectionViewTypeKey) as TextFieldModelController).Data;
            CollectionView.CollectionViewType viewType =  (CollectionView.CollectionViewType) Enum.Parse(typeof(CollectionView.CollectionViewType), typeString);
            var view = new CollectionView(collectionViewModel,  viewType);

            //add to key to framework element dictionary
            var reference = data as ReferenceFieldModelController;
            if (keysToFrameworkElementsIn != null)
            {
                keysToFrameworkElementsIn[reference.FieldKey] = view.ConnectionEllipseInput;
                keysToFrameworkElementsIn[KeyStore.CollectionOutputKey] = view.ConnectionEllipseOutput;
            }


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