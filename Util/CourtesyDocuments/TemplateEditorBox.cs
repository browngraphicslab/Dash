using System;
using Windows.UI.Xaml;
using DashShared;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.Foundation;
using Dash.Converters;
using Windows.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Diagnostics;

namespace Dash
{
    public class TemplateEditorBox : CourtesyDocument
    {
        public static DocumentType DocumentType = new DocumentType("931C41F4-EA4C-4911-A2EE-0D0B6C7BB089", "Template Editor Box");
        private static readonly string PrototypeId = "92230B6B-CE44-495E-A278-EE991A58B91D";

        public TemplateEditorBox(FieldControllerBase refToWorkingDoc = null, double x = 0, double y = 0, double w = 200, double h = 20)
        {
            // template editor box data key = working doc
            var fields = DefaultLayoutFields(new Point(x,y), new Size(w,h), refToWorkingDoc);
            SetupDocument(DocumentType, PrototypeId, "TemplateEditorBox Prototype Layout", fields);
        }
        public class AutomatedTextWrappingBinding : SafeDataToXamlConverter<List<object>, Windows.UI.Xaml.TextWrapping>
        {
            public override Windows.UI.Xaml.TextWrapping ConvertDataToXaml(List<object> data, object parameter = null)
            {
                if (data[0] != null && data[0] is double)
                {
                    switch ((int)(double)data[0])
                    {
                        case (int)Windows.UI.Xaml.TextWrapping.Wrap:
                            return Windows.UI.Xaml.TextWrapping.Wrap;
                        case (int)Windows.UI.Xaml.TextWrapping.NoWrap:
                            return Windows.UI.Xaml.TextWrapping.NoWrap;
                    }

                }
                double width = (double)data[1];
                return double.IsNaN(width) ? Windows.UI.Xaml.TextWrapping.NoWrap : Windows.UI.Xaml.TextWrapping.Wrap;
            }

            public override List<object> ConvertXamlToData(Windows.UI.Xaml.TextWrapping xaml, object parameter = null)
            {
                throw new NotImplementedException();
            }
        }

        public static FrameworkElement MakeView(DocumentController layoutDocController, Context context)
        {
	        if (layoutDocController == null)
	        {
		        Debug.WriteLine("DOC CONTROLLER IS NULL");
	        }

            TemplateEditorView tev = null;
            var dataField = layoutDocController.GetDereferencedField(KeyStore.DataKey, context);
            var dataDocument = dataField as DocumentController;
            if (dataDocument != null)
            {
                tev = new TemplateEditorView
                {
                    LayoutDocument = layoutDocController,
                    DataDocument = layoutDocController.GetDataDocument(),
                    //LinkedDocument = docController.GetField<DocumentController>(KeyStore.DataKey),
                    ManipulationMode = ManipulationModes.All
                };
	            tev.Load();
                tev.PointerEntered += (sender, args) => tev.ManipulationMode = ManipulationModes.None;
                tev.GotFocus += (sender, args) => tev.ManipulationMode = ManipulationModes.None;
                tev.LostFocus += (sender, args) => tev.ManipulationMode = ManipulationModes.All;
                //TODO: lose focus when you drag the rich text view so that text doesn't select at the same time
                tev.HorizontalAlignment = HorizontalAlignment.Stretch;
                tev.VerticalAlignment = VerticalAlignment.Stretch;
                SetupBindings(tev, layoutDocController, context);
            }

            return tev;
        }

        private static ReferenceController GetTextReference(DocumentController docController)
        {
            return docController.GetField(KeyStore.DataKey) as ReferenceController;
        }
    }

}