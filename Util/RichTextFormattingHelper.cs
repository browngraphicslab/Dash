using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml.Controls;

namespace Dash
{
    /// <summary>
    /// This class contains helper functions used in the RichTextView and FormattingMenu classes
    /// </summary>
    public class RichTextFormattingHelper
    {
        public RichEditBox RichEditBox;
        public RichTextView RichTextView;

        public RichTextFormattingHelper(RichTextView inRichTextView, RichEditBox richEditBox)
        {
            RichEditBox = richEditBox;
            RichTextView = inRichTextView;
        }

        /// <summary>
        /// Makes current selection in the xRichEditBox bold
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        public void Bold(bool updateDocument)
        {
            Debug.WriteLine($"{this.RichEditBox.Document.Selection.CharacterFormat.Bold}");

            // on/off instead of toggle to know exactly what state it is in (to determine whether a selection is bold or not)
            this.RichEditBox.Document.Selection.CharacterFormat.Bold = this.RichEditBox.Document.Selection.CharacterFormat.Bold == FormatEffect.On ? FormatEffect.Off : FormatEffect.On;
            if (updateDocument) RichTextView.UpdateDocument();
        }

        /// <summary>
        /// Italicizes current selection in xRichEditBox
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        public void Italicize(bool updateDocument)
        {
            if (this.RichEditBox.Document.Selection.CharacterFormat.Italic == FormatEffect.On)
            {
                this.RichEditBox.Document.Selection.CharacterFormat.Italic = FormatEffect.Off;
            }
            else
            {
                this.RichEditBox.Document.Selection.CharacterFormat.Italic = FormatEffect.On;
            }
            //this.xRichEditBox.Document.Selection.CharacterFormat.Italic = FormatEffect.Toggle;
            if (updateDocument) RichTextView.UpdateDocument();
        }

        /// <summary>
        /// Underlines the current selection in xRichEditBox
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        public void Underline(bool updateDocument)
        {
            this.RichEditBox.Document.Selection.CharacterFormat.Underline = this.RichEditBox.Document.Selection.CharacterFormat.Underline == UnderlineType.None ? UnderlineType.Single : UnderlineType.None;
            if (updateDocument) RichTextView.UpdateDocument();
        }

        /// <summary>
        /// Strikethrough the current selection in xRichEditBox
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        public void Strikethrough(bool updateDocument)
        {
            if (RichEditBox.Document.Selection.CharacterFormat.Strikethrough == FormatEffect.On)
                RichEditBox.Document.Selection.CharacterFormat.Strikethrough = FormatEffect.Off;
            else
                RichEditBox.Document.Selection.CharacterFormat.Strikethrough = FormatEffect.On;
            if (updateDocument) RichTextView.UpdateDocument();
        }

        /// <summary>
        /// Formats the current selection in xRichEditBox into superscripts
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        public void Superscript(bool updateDocument)
        {
            if (RichEditBox.Document.Selection.CharacterFormat.Superscript == FormatEffect.On)
                RichEditBox.Document.Selection.CharacterFormat.Superscript = FormatEffect.Off;
            else
                RichEditBox.Document.Selection.CharacterFormat.Superscript = FormatEffect.On;
            if (updateDocument) RichTextView.UpdateDocument();
        }

        /// <summary>
        /// Formats the current selection in xRichEditBox into subscripts
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        public void Subscript(bool updateDocument)
        {
            if (RichEditBox.Document.Selection.CharacterFormat.Subscript == FormatEffect.On)
                RichEditBox.Document.Selection.CharacterFormat.Subscript = FormatEffect.Off;
            else
                RichEditBox.Document.Selection.CharacterFormat.Subscript = FormatEffect.On;
            if (updateDocument) RichTextView.UpdateDocument();
        }

        /// <summary>
        /// Formats the current selection in xRichEditBox into smallcaps
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        public void SmallCaps(bool updateDocument)
        {
            if (RichEditBox.Document.Selection.CharacterFormat.SmallCaps == FormatEffect.On)
                RichEditBox.Document.Selection.CharacterFormat.SmallCaps = FormatEffect.Off;
            else
                RichEditBox.Document.Selection.CharacterFormat.SmallCaps = FormatEffect.On;
            if (updateDocument) RichTextView.UpdateDocument();
        }

        /// <summary>
        /// Formats the current selection in xRichEditBox into allcaps
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        public void AllCaps(bool updateDocument)
        {
            if (RichEditBox.Document.Selection.CharacterFormat.AllCaps == FormatEffect.On)
                RichEditBox.Document.Selection.CharacterFormat.AllCaps = FormatEffect.Off;
            else
                RichEditBox.Document.Selection.CharacterFormat.AllCaps = FormatEffect.On;
            if (updateDocument) RichTextView.UpdateDocument();
        }

        /// <summary>
        /// Highlights the current selection in xRichEditBox, the color of the highlight is specified by background
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="background"></param>
        /// <param name="updateDocument"></param>
        public void Highlight(Color background, bool updateDocument)
        {
            RichEditBox.Document.Selection.CharacterFormat.BackgroundColor = background;
            if (updateDocument) RichTextView.UpdateDocument();
        }

        /// <summary>
        /// Changes the color of the font of the current selection in xRichEditBox, the font color is specified by color
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="color"></param>
        /// <param name="updateDocument"></param>
        public void Foreground(Color color, bool updateDocument)
        {
            RichEditBox.Document.Selection.CharacterFormat.ForegroundColor = color;
            if (updateDocument) RichTextView.UpdateDocument();
        }

        /// <summary>
        /// Sets the paragraph alignment of the current selection to be what's specified by alignment
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="alignment"></param>
        /// <param name="updateDocument"></param>
        public void Alignment(object alignment, bool updateDocument)
        {
            if (alignment != null && alignment.GetType() == typeof(ParagraphAlignment))
            {
                RichEditBox.Document.Selection.ParagraphFormat.Alignment = (ParagraphAlignment)alignment;
                if (updateDocument) RichTextView.UpdateDocument();
            }
        }

        /// <summary>
        /// Sets the list marker of the current selection to be what's specified by type
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="type"></param>
        /// <param name="updateDocument"></param>
        public void Marker(object type, bool updateDocument)
        {
            if (type != null && type.GetType() == typeof(MarkerType))
            {
                RichEditBox.Document.Selection.ParagraphFormat.ListType = (MarkerType)type;
                if (updateDocument) RichTextView.UpdateDocument();
            }
        }
    }
}
