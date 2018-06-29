﻿using Windows.UI.Xaml;
using DashShared;
using Windows.UI.Xaml.Input;
using Windows.Foundation;

namespace Dash
{
    /// <summary>
    /// Creates the document controller for the actual template editor pane (with the left and right panes).
    /// Not to be confused with TemplateBox, which is the document controller for the physical template, which
    /// looks nothing more like a regular document.
    /// </summary>
    public class TemplateEditorBox : CourtesyDocument
    {
        public static DocumentType DocumentType = new DocumentType("931C41F4-EA4C-4911-A2EE-0D0B6C7BB089", "Template Editor Box");
        private static readonly string PrototypeId = "92230B6B-CE44-495E-A278-EE991A58B91D";

        public TemplateEditorBox(DocumentController workingDoc = null, double x = 0, double y = 0, double w = 200, double h = 20)
        {
            // template editor box data key = working doc
            var fields = DefaultLayoutFields(new Point(x,y), new Size(w,h), workingDoc);
            fields[KeyStore.DocumentContextKey] = new TemplateBox().Document;

            SetupDocument(DocumentType, PrototypeId, "TemplateEditorBox Prototype Layout", fields);
        }

        public TemplateEditorBox(DocumentController workingDoc = null, Point where = default(Point),
            Size size = default(Size)) : this(workingDoc, where.X, where.Y, size.Width, size.Height) { }

        public static FrameworkElement MakeView(DocumentController layoutDocController, Context context)
        {
            var tev = new TemplateEditorView
            {
                // Layout Document's Data = Working Document
                LayoutDocument = layoutDocController,
                // Data Doc's Data = List of Layout Documents
                DataDocument = layoutDocController.GetDataDocument(),
                ManipulationMode = ManipulationModes.All,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            SetupBindings(tev, layoutDocController, context);

            return tev;
        }
    }

}