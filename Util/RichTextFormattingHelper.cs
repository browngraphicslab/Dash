using System;
using System.Collections.Generic;
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
        private RichEditBox _richEditBox;
        private RichTextView richTextView;

        public RichTextFormattingHelper(RichTextView inRichTextView, RichEditBox richEditBox)
        {
            _richEditBox = richEditBox;
            richTextView = inRichTextView;
        }

        /// <summary>
        /// Makes current selection in the xRichEditBox bold
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        public void Bold(bool updateDocument)
        {
            // on/off instead of toggle to know exactly what state it is in (to determine whether a selection is bold or not)
            if (this._richEditBox.Document.Selection.CharacterFormat.Bold == FormatEffect.On)
            {
                this._richEditBox.Document.Selection.CharacterFormat.Bold = FormatEffect.Off;
            }
            else
            {
                this._richEditBox.Document.Selection.CharacterFormat.Bold = FormatEffect.On;
            }
            if (updateDocument) richTextView.UpdateDocument();
        }

        /// <summary>
        /// Italicizes current selection in xRichEditBox
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        public void Italicize(bool updateDocument)
        {
            if (this._richEditBox.Document.Selection.CharacterFormat.Italic == FormatEffect.On)
            {
                this._richEditBox.Document.Selection.CharacterFormat.Italic = FormatEffect.Off;
            }
            else
            {
                this._richEditBox.Document.Selection.CharacterFormat.Italic = FormatEffect.On;
            }
            //this.xRichEditBox.Document.Selection.CharacterFormat.Italic = FormatEffect.Toggle;
            if (updateDocument) richTextView.UpdateDocument();
        }

        /// <summary>
        /// Underlines the current selection in xRichEditBox
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        public void Underline(bool updateDocument)
        {
            if (this._richEditBox.Document.Selection.CharacterFormat.Underline == UnderlineType.None)
                this._richEditBox.Document.Selection.CharacterFormat.Underline = UnderlineType.Single;
            else
                this._richEditBox.Document.Selection.CharacterFormat.Underline = UnderlineType.None;
            if (updateDocument) richTextView.UpdateDocument();
        }

        /// <summary>
        /// Strikethrough the current selection in xRichEditBox
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        public void Strikethrough(bool updateDocument)
        {
            if (_richEditBox.Document.Selection.CharacterFormat.Strikethrough == FormatEffect.On)
                _richEditBox.Document.Selection.CharacterFormat.Strikethrough = FormatEffect.Off;
            else
                _richEditBox.Document.Selection.CharacterFormat.Strikethrough = FormatEffect.On;
            if (updateDocument) richTextView.UpdateDocument();
        }

        /// <summary>
        /// Formats the current selection in xRichEditBox into superscripts
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        public void Superscript(bool updateDocument)
        {
            if (_richEditBox.Document.Selection.CharacterFormat.Superscript == FormatEffect.On)
                _richEditBox.Document.Selection.CharacterFormat.Superscript = FormatEffect.Off;
            else
                _richEditBox.Document.Selection.CharacterFormat.Superscript = FormatEffect.On;
            if (updateDocument) richTextView.UpdateDocument();
        }

        /// <summary>
        /// Formats the current selection in xRichEditBox into subscripts
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        public void Subscript(bool updateDocument)
        {
            if (_richEditBox.Document.Selection.CharacterFormat.Subscript == FormatEffect.On)
                _richEditBox.Document.Selection.CharacterFormat.Subscript = FormatEffect.Off;
            else
                _richEditBox.Document.Selection.CharacterFormat.Subscript = FormatEffect.On;
            if (updateDocument) richTextView.UpdateDocument();
        }

        /// <summary>
        /// Formats the current selection in xRichEditBox into smallcaps
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        public void SmallCaps(bool updateDocument)
        {
            if (_richEditBox.Document.Selection.CharacterFormat.SmallCaps == FormatEffect.On)
                _richEditBox.Document.Selection.CharacterFormat.SmallCaps = FormatEffect.Off;
            else
                _richEditBox.Document.Selection.CharacterFormat.SmallCaps = FormatEffect.On;
            if (updateDocument) richTextView.UpdateDocument();
        }

        /// <summary>
        /// Formats the current selection in xRichEditBox into allcaps
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        public void AllCaps(bool updateDocument)
        {
            if (_richEditBox.Document.Selection.CharacterFormat.AllCaps == FormatEffect.On)
                _richEditBox.Document.Selection.CharacterFormat.AllCaps = FormatEffect.Off;
            else
                _richEditBox.Document.Selection.CharacterFormat.AllCaps = FormatEffect.On;
            if (updateDocument) richTextView.UpdateDocument();
        }

        /// <summary>
        /// Highlights the current selection in xRichEditBox, the color of the highlight is specified by background
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="background"></param>
        /// <param name="updateDocument"></param>
        public void Highlight(Color background, bool updateDocument)
        {
            _richEditBox.Document.Selection.CharacterFormat.BackgroundColor = background;
            if (updateDocument) richTextView.UpdateDocument();
        }

        /// <summary>
        /// Changes the color of the font of the current selection in xRichEditBox, the font color is specified by color
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="color"></param>
        /// <param name="updateDocument"></param>
        public void Foreground(Color color, bool updateDocument)
        {
            _richEditBox.Document.Selection.CharacterFormat.ForegroundColor = color;
            if (updateDocument) richTextView.UpdateDocument();
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
                _richEditBox.Document.Selection.ParagraphFormat.Alignment = (ParagraphAlignment)alignment;
                if (updateDocument) richTextView.UpdateDocument();
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
                _richEditBox.Document.Selection.ParagraphFormat.ListType = (MarkerType)type;
                if (updateDocument) richTextView.UpdateDocument();
            }
        }
    }
}
