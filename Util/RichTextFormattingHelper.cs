using System.Diagnostics;
using Windows.UI;
using Windows.UI.Text;

namespace Dash
{
    /// <summary>
    /// This class contains helper functions used in the RichTextView and FormattingMenu classes
    /// </summary>
    public static class RichTextFormattingHelper
    {

        /// <summary>
        /// Makes current selection in the xRichEditBox bold
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        public static void Bold(this RichTextView v, bool updateDocument)
        {
            Debug.WriteLine($"{v.xRichEditBox.Document.Selection.CharacterFormat.Bold}");

            // on/off instead of toggle to know exactly what state it is in (to determine whether a selection is bold or not)
            v.xRichEditBox.Document.Selection.CharacterFormat.Bold = v.xRichEditBox.Document.Selection.CharacterFormat.Bold == FormatEffect.On ? FormatEffect.Off : FormatEffect.On;
            if (updateDocument)
                v.UpdateDocumentFromXaml();
        }

        /// <summary>
        /// Italicizes current selection in xRichEditBox
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        public static void Italicize(this RichTextView v, bool updateDocument)
        {
            if (v.xRichEditBox.Document.Selection.CharacterFormat.Italic == FormatEffect.On)
            {
                v.xRichEditBox.Document.Selection.CharacterFormat.Italic = FormatEffect.Off;
            }
            else
            {
                v.xRichEditBox.Document.Selection.CharacterFormat.Italic = FormatEffect.On;
            }
            //this.xRichEditBox.Document.Selection.CharacterFormat.Italic = FormatEffect.Toggle;
            if (updateDocument) v.UpdateDocumentFromXaml();
        }

        /// <summary>
        /// Underlines the current selection in xRichEditBox
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        public static void Underline(this RichTextView v, bool updateDocument)
        {
            v.xRichEditBox.Document.Selection.CharacterFormat.Underline = v.xRichEditBox.Document.Selection.CharacterFormat.Underline == UnderlineType.None ? UnderlineType.Single : UnderlineType.None;
            if (updateDocument) v.UpdateDocumentFromXaml();
        }

        /// <summary>
        /// Strikethrough the current selection in xRichEditBox
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        public static void Strikethrough(this RichTextView v, bool updateDocument)
        {
            if (v.xRichEditBox.Document.Selection.CharacterFormat.Strikethrough == FormatEffect.On)
                v.xRichEditBox.Document.Selection.CharacterFormat.Strikethrough = FormatEffect.Off;
            else
                v.xRichEditBox.Document.Selection.CharacterFormat.Strikethrough = FormatEffect.On;
            if (updateDocument) v.UpdateDocumentFromXaml();
        }

        /// <summary>
        /// Formats the current selection in xRichEditBox into superscripts
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        public static void Superscript(this RichTextView v, bool updateDocument)
        {
            if (v.xRichEditBox.Document.Selection.CharacterFormat.Superscript == FormatEffect.On)
                v.xRichEditBox.Document.Selection.CharacterFormat.Superscript = FormatEffect.Off;
            else
                v.xRichEditBox.Document.Selection.CharacterFormat.Superscript = FormatEffect.On;
            if (updateDocument) v.UpdateDocumentFromXaml();
        }

        /// <summary>
        /// Formats the current selection in xRichEditBox into subscripts
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        public static void Subscript(this RichTextView v, bool updateDocument)
        {
            if (v.xRichEditBox.Document.Selection.CharacterFormat.Subscript == FormatEffect.On)
                v.xRichEditBox.Document.Selection.CharacterFormat.Subscript = FormatEffect.Off;
            else
                v.xRichEditBox.Document.Selection.CharacterFormat.Subscript = FormatEffect.On;
            if (updateDocument) v.UpdateDocumentFromXaml();
        }

        /// <summary>
        /// Formats the current selection in xRichEditBox into smallcaps
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        public static void SmallCaps(this RichTextView v, bool updateDocument)
        {
            if (v.xRichEditBox.Document.Selection.CharacterFormat.SmallCaps == FormatEffect.On)
                v.xRichEditBox.Document.Selection.CharacterFormat.SmallCaps = FormatEffect.Off;
            else
                v.xRichEditBox.Document.Selection.CharacterFormat.SmallCaps = FormatEffect.On;
            if (updateDocument) v.UpdateDocumentFromXaml();
        }

        /// <summary>
        /// Formats the current selection in xRichEditBox into allcaps
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        public static void AllCaps(this RichTextView v, bool updateDocument)
        {
            if (v.xRichEditBox.Document.Selection.CharacterFormat.AllCaps == FormatEffect.On)
                v.xRichEditBox.Document.Selection.CharacterFormat.AllCaps = FormatEffect.Off;
            else
                v.xRichEditBox.Document.Selection.CharacterFormat.AllCaps = FormatEffect.On;
            if (updateDocument) v.UpdateDocumentFromXaml();
        }

        /// <summary>
        /// Highlights the current selection in xRichEditBox, the color of the highlight is specified by background
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="background"></param>
        /// <param name="updateDocument"></param>
        public static void Highlight(this RichTextView v, Color background, bool updateDocument)
        {
            if (v.xRichEditBox.Document.Selection.CharacterFormat.BackgroundColor == background)
                v.xRichEditBox.Document.Selection.CharacterFormat.BackgroundColor = Colors.Transparent;
            else v.xRichEditBox.Document.Selection.CharacterFormat.BackgroundColor = background;
            if (updateDocument) v.UpdateDocumentFromXaml();
        }

        /// <summary>
        /// Changes the color of the font of the current selection in xxRichEditBox, the font color is specified by color
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="color"></param>
        /// <param name="updateDocument"></param>
        public static void Foreground(this RichTextView v, Color color, bool updateDocument)
        {
	        v.xRichEditBox.Document.Selection.CharacterFormat.ForegroundColor = color;
            if (updateDocument) v.UpdateDocumentFromXaml();
        }

        /// <summary>
        /// Sets the paragraph alignment of the current selection to be what's specified by alignment
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="alignment"></param>
        /// <param name="updateDocument"></param>
        public static void Alignment(this RichTextView v, object alignment, bool updateDocument)
        {
            if (alignment != null && alignment.GetType() == typeof(ParagraphAlignment))
            {
                v.xRichEditBox.Document.Selection.ParagraphFormat.Alignment = (ParagraphAlignment)alignment;
                if (updateDocument) v.UpdateDocumentFromXaml();
            }
        }

        /// <summary>
        /// Sets the list marker of the current selection to be what's specified by type
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="type"></param>
        /// <param name="updateDocument"></param>
        public static void Marker(this RichTextView v, object type, bool updateDocument)
        {
            if (type != null && type.GetType() == typeof(MarkerType))
            {
                v.xRichEditBox.Document.Selection.ParagraphFormat.ListType = (MarkerType)type;
                if (updateDocument) v.UpdateDocumentFromXaml();
            }
        }
    }
}
