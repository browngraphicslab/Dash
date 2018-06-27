using DashShared;
using System.Collections.Generic;
using Windows.Foundation;

namespace Dash
{
    public class TemplateEditorBase : NoteDocument
    {
        public static DocumentType DocumentType = new DocumentType("0AD8E8E2-D414-4AC3-9D33-98BA185510A2", "Template Editor Base");
        static string _prototypeID = "804CB4BF-AB4D-4600-AD92-3AD31AFFA10B";
        protected override DocumentController createPrototype(string prototypeID)
        {
            return null;
        }

        static int rcount = 1;
        DocumentController CreateLayout(DocumentController dataDoc, Point @where, Size size)
        {
            return null;
        }

        public TemplateEditorBase(DocumentView linkedToDoc, DocumentController doc, Point where = new Point(), Size size = new Size()) :
            base(_prototypeID)
        {
        }
    }
}
