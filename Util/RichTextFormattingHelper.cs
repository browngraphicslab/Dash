using System.Diagnostics;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml.Controls;

namespace Dash
{
    /// <summary>
    /// This class contains helper functions used in the RichEditBox and FormattingMenu classes
    /// </summary>
    public static class RichTextFormattingHelper
    {

        /// <summary>
        /// Makes current selection in the xRichEditBox bold
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        public static void Bold(this RichEditBox v)
        {
            Debug.WriteLine($"{v.Document.Selection.CharacterFormat.Bold}");

            // on/off instead of toggle to know exactly what state it is in (to determine whether a selection is bold or not)
            v.Document.Selection.CharacterFormat.Bold = v.Document.Selection.CharacterFormat.Bold == FormatEffect.On ? FormatEffect.Off : FormatEffect.On;
        }

        /// <summary>
        /// Italicizes current selection in xRichEditBox
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        public static void Italicize(this RichEditBox v)
        {
            if (v.Document.Selection.CharacterFormat.Italic == FormatEffect.On)
            {
                v.Document.Selection.CharacterFormat.Italic = FormatEffect.Off;
            }
            else
            {
                v.Document.Selection.CharacterFormat.Italic = FormatEffect.On;
            }
            //this.Document.Selection.CharacterFormat.Italic = FormatEffect.Toggle;
        }

        /// <summary>
        /// Underlines the current selection in xRichEditBox
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        public static void Underline(this RichEditBox v)
        {
            v.Document.Selection.CharacterFormat.Underline = v.Document.Selection.CharacterFormat.Underline == UnderlineType.None ? UnderlineType.Single : UnderlineType.None;
        }

        /// <summary>
        /// Strikethrough the current selection in xRichEditBox
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        public static void Strikethrough(this RichEditBox v)
        {
            if (v.Document.Selection.CharacterFormat.Strikethrough == FormatEffect.On)
                v.Document.Selection.CharacterFormat.Strikethrough = FormatEffect.Off;
            else
                v.Document.Selection.CharacterFormat.Strikethrough = FormatEffect.On;
        }

        /// <summary>
        /// Formats the current selection in xRichEditBox into superscripts
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        public static void Superscript(this RichEditBox v)
        {
            if (v.Document.Selection.CharacterFormat.Superscript == FormatEffect.On)
                v.Document.Selection.CharacterFormat.Superscript = FormatEffect.Off;
            else
                v.Document.Selection.CharacterFormat.Superscript = FormatEffect.On;
        }

        /// <summary>
        /// Formats the current selection in xRichEditBox into subscripts
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        public static void Subscript(this RichEditBox v)
        {
            if (v.Document.Selection.CharacterFormat.Subscript == FormatEffect.On)
                v.Document.Selection.CharacterFormat.Subscript = FormatEffect.Off;
            else
                v.Document.Selection.CharacterFormat.Subscript = FormatEffect.On;
        }

        /// <summary>
        /// Formats the current selection in xRichEditBox into smallcaps
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        public static void SmallCaps(this RichEditBox v)
        {
            if (v.Document.Selection.CharacterFormat.SmallCaps == FormatEffect.On)
                v.Document.Selection.CharacterFormat.SmallCaps = FormatEffect.Off;
            else
                v.Document.Selection.CharacterFormat.SmallCaps = FormatEffect.On;
        }

        /// <summary>
        /// Formats the current selection in xRichEditBox into allcaps
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        public static void AllCaps(this RichEditBox v)
        {
            if (v.Document.Selection.CharacterFormat.AllCaps == FormatEffect.On)
                v.Document.Selection.CharacterFormat.AllCaps = FormatEffect.Off;
            else
                v.Document.Selection.CharacterFormat.AllCaps = FormatEffect.On;
        }

        /// <summary>
        /// Highlights the current selection in xRichEditBox, the color of the highlight is specified by background
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="background"></param>
        /// <param name="updateDocument"></param>
        public static void Highlight(this RichEditBox v, Color background)
        {
            //if (v.Document.Selection.CharacterFormat.BackgroundColor == background)
            //    v.Document.Selection.CharacterFormat.BackgroundColor = Colors.Transparent;
            //else
                v.Document.Selection.CharacterFormat.BackgroundColor = background;
        }

        /// <summary>
        /// Changes the color of the font of the current selection in xxRichEditBox, the font color is specified by color
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="color"></param>
        /// <param name="updateDocument"></param>
        public static void Foreground(this RichEditBox v, Color color)
        {
	        v.Document.Selection.CharacterFormat.ForegroundColor = color;
        }

        /// <summary>
        /// Sets the paragraph alignment of the current selection to be what's specified by alignment
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="alignment"></param>
        /// <param name="updateDocument"></param>
        public static void Alignment(this RichEditBox v, object alignment)
        {
            if (alignment != null && alignment.GetType() == typeof(ParagraphAlignment))
            {
                v.Document.Selection.ParagraphFormat.Alignment = (ParagraphAlignment)alignment;
            }
        }

        /// <summary>
        /// Sets the list marker of the current selection to be what's specified by type
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="type"></param>
        /// <param name="updateDocument"></param>
        public static void Marker(this RichEditBox v, object type)
        {
            if (type != null && type.GetType() == typeof(MarkerType))
            {
                v.Document.Selection.ParagraphFormat.ListType = (MarkerType)type;
            }
        }
    }
}
