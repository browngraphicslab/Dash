﻿using DashShared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class RichTextView : UserControl
    {
        private RichTextFieldModelController _richTextFieldModelController;
        ObservableCollection<FontFamily> fonts = new ObservableCollection<FontFamily>();
        //private int _selectionStart;
        //private int _selectionEnd;
        ReferenceFieldModelController _reftorichtext;
        Context _refcontext;

        private ITextSelection _selectedText
        {
            set { _richTextFieldModelController.SelectedText = value; }
        }

        public RichTextView(RichTextFieldModelController richTextFieldModelController, ReferenceFieldModelController reftorichtext, Context refcontext)
        {
            _reftorichtext = reftorichtext;
            _refcontext = refcontext;
            this.InitializeComponent();
            _richTextFieldModelController = richTextFieldModelController;
            Loaded += OnLoaded;
            xRichEitBox.SelectionChanged += XRichEitBox_SelectionChanged;
            xRichEitBox.LostFocus += XRichEitBox_LostFocus;
            xRichEitBox.GotFocus += XRichEitBoxOnGotFocus;
            xRichEitBox.TextChanged += XRichEitBoxOnTextChanged;
            _richTextFieldModelController.FieldModelUpdated += RichTextFieldModelControllerOnFieldModelUpdated;
            if (_reftorichtext != null)
                _reftorichtext.GetDocumentController(refcontext).DocumentFieldUpdated += RichTextView_DocumentFieldUpdated;
        }

        private void RichTextView_DocumentFieldUpdated(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
        {
            if (_reftorichtext != null)
                if (args.Action == DocumentController.FieldUpdatedAction.Replace && args.OldValue == _richTextFieldModelController)
                {
                    _richTextFieldModelController = args.NewValue as RichTextFieldModelController;
                    string curText;
                    xRichEitBox.Document.GetText(TextGetOptions.None, out curText);
                    var argText = args.NewValue.DereferenceToRoot<RichTextFieldModelController>(args.Context).RichTextData.ReadableString.TrimEnd('\r');
                    if (curText.TrimEnd('\r') != argText)
                    {
                        var newtext = (args.NewValue as RichTextFieldModelController).RichTextData;
                        xRichEitBox.Document.SetText(TextSetOptions.FormatRtf, newtext.RtfFormatString);
                        //string finalText;
                        //xRichEitBox.Document.GetText(TextGetOptions.FormatRtf, out finalText);
                        //if (finalText != newtext.RtfFormatString)
                        //    System.Diagnostics.Debug.WriteLine("Mismatch");
                    }
                }
        }

        private void XRichEitBoxOnGotFocus(object sender, RoutedEventArgs routedEventArgs)
        {
        }

        private void XRichEitBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            _selectedText = xRichEitBox.Document.Selection;
        }


        private void XRichEitBox_LostFocus(object sender, RoutedEventArgs e)
        {
        }

        private async Task<string> LoadText()
        {
            var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/rtf.txt"));
            var rtfString = await FileIO.ReadTextAsync(file);
            return rtfString;
        }

        private async void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            if (_richTextFieldModelController.RichTextData != null)
            {
                xRichEitBox.Document.SetText(TextSetOptions.FormatRtf, _richTextFieldModelController.RichTextData.RtfFormatString);
            }
            else
            {
                var rtfString = await LoadText();
                xRichEitBox.Document.SetText(TextSetOptions.FormatRtf, rtfString);
            }

        }


        private void RichTextFieldModelControllerOnFieldModelUpdated(FieldModelController sender, FieldUpdatedEventArgs args, Context c)
        {
            var text = string.Empty;
            xRichEitBox.Document.GetText(TextGetOptions.FormatRtf, out text);
            if (_richTextFieldModelController.RichTextData != null && !text.Equals(_richTextFieldModelController.RichTextData))
            {
                xRichEitBox.Document.SetText(TextSetOptions.FormatRtf, _richTextFieldModelController.RichTextData.RtfFormatString);
            }
        }
        // freezes the app
        private void XRichEitBoxOnTextChanged(object sender, RoutedEventArgs routedEventArgs)
        {
            string allText;
            xRichEitBox.Document.GetText(TextGetOptions.UseObjectText, out allText);
            if (_reftorichtext != null)
            { // we seem to get an additional \r added for no reason when you SetText on an RTF document.  this avoids an infinite loop
                var curRTFField = _reftorichtext.GetDocumentController(_refcontext).GetDereferencedField(_reftorichtext.FieldKey, _refcontext) as RichTextFieldModelController;
                if (allText.TrimEnd('\r') == curRTFField.RichTextData.ReadableString.TrimEnd('\r'))
                    return;
            }

            var startPt = new Point();
            var s1 = this.xRichEitBox.Document.Selection.StartPosition;
            var s2 = this.xRichEitBox.Document.Selection.EndPosition;
            this.xRichEitBox.Document.Selection.GetPoint(HorizontalCharacterAlignment.Center, VerticalCharacterAlignment.Baseline, PointOptions.Start, out startPt);

            // try to get last typed character based on the current selection position 
            this.xRichEitBox.Document.Selection.SetRange(Math.Max(0, s1 - 1), s1);
            string lastTypedCharacter;
            this.xRichEitBox.Document.Selection.GetText(TextGetOptions.None, out lastTypedCharacter);

            // if the last lastTypedCharacter is white space, then we check to see if it terminates a hyperlink
            if (lastTypedCharacter == " " || lastTypedCharacter == "\r" || lastTypedCharacter == "^")
            {
                // search through all the text for the nearest '@' indicating the start of a possible hyperlink
                int atPos = findPreviousHyperlinkStartMarker(allText, s1);

                // we found the nearest '@'
                if (atPos != -1)
                {
                    // get the text between the '@' and the current input position 
                    var refText = getHyperlinkText(atPos, s2);

                    if (!refText.StartsWith("HYPERLINK")) // @HYPERLINK means we've already created the hyperlink
                    {
                        // see if we can find a document whose primary keys match the text
                        var theDoc = findHyperlinkTarget(lastTypedCharacter == "^", refText);

                        createRTFHyperlink(theDoc, startPt, ref s1, ref s2, lastTypedCharacter == "^");
                    }
                }
            }
            
            if (_reftorichtext != null)
            {
                string allRtfText;
                xRichEitBox.Document.GetText(TextGetOptions.FormatRtf, out allRtfText);
                this._reftorichtext.GetDocumentController(_refcontext).SetField(_reftorichtext.FieldKey, new RichTextFieldModelController(new RichTextFieldModel.RTD(allText, allRtfText)), true);
            }
            this.xRichEitBox.Document.Selection.SetRange(s1, s2);
        }

        static DocumentController findHyperlinkTarget(bool createIfNeeded, string refText)
        {
            var theDoc = DocumentController.FindDocMatchingPrimaryKeys(new List<string>(new string[] { refText }));
            if (theDoc == null && createIfNeeded)
            {
                if (refText.StartsWith("http"))
                {
                    theDoc = DBTest.CreateWebPage(refText);
                }
                else
                {
                    theDoc = new NoteDocuments.RichTextNote(NoteDocuments.PostitNote.DocumentType).Document;
                    theDoc.SetField(NoteDocuments.RichTextNote.TitleKey, new TextFieldModelController(refText), true);
                }
            }

            return theDoc;
        }

        void createRTFHyperlink(DocumentController theDoc, Point startPt, ref int s1, ref int s2, bool createIfNeeded)
        {
            if (theDoc != null && this.xRichEitBox.Document.Selection.StartPosition != this.xRichEitBox.Document.Selection.EndPosition && 
                this.xRichEitBox.Document.Selection.Link != "\"" + theDoc.GetId() + "\"")
            {
                // set the hyperlink for the matched text
                this.xRichEitBox.Document.Selection.Link = "\"" + theDoc.GetId() + "\"";
                // advance the end selection past the RTF embedded HYPERLINK keyword
                s2 += this.xRichEitBox.Document.Selection.Link.Length + "HYPERLINK".Length + 1;
                s1 = s2;
                this.xRichEitBox.Document.Selection.CharacterFormat.BackgroundColor = Colors.LightCyan;
                this.xRichEitBox.Document.Selection.SetPoint(startPt, PointOptions.Start, true);
            }
        }
        

        string getHyperlinkText(int atPos, int s2)
        {
            this.xRichEitBox.Document.Selection.SetRange(atPos + 1, s2 - 1);
            string refText;
            this.xRichEitBox.Document.Selection.GetText(TextGetOptions.None, out refText);

            return refText;
        }

        int findPreviousHyperlinkStartMarker(string allText, int s1)
        {
            this.xRichEitBox.Document.Selection.SetRange(0, allText.Length);
            var atPos = -1;
            while (this.xRichEitBox.Document.Selection.FindText("@", 0, FindOptions.None) > 0)
            {
                if (this.xRichEitBox.Document.Selection.StartPosition < s1)
                {
                    atPos = this.xRichEitBox.Document.Selection.StartPosition;
                    this.xRichEitBox.Document.Selection.SetRange(atPos + 1, allText.Length);
                }
                else break;
            }

            return atPos;
        }

        int LastS1 = 0, LastS2 = 0;
        void xRichEitBox_SelectionChanged_1(object sender, RoutedEventArgs e)
        {
            var s1 = this.xRichEitBox.Document.Selection.StartPosition;
            var s2 = this.xRichEitBox.Document.Selection.EndPosition;
            if (LastS1 != s1 || LastS2 != s2)  // test if the selection has actually changed... seem to get in here when nothing has happened perhaps because of losing focus?
            {
                // If there's a Document hyperlink in the selection, then follow it.  This is a hack because
                // I don't seem to be able to get direct access to the hyperlink events in the rich edit box.
                if (this.xRichEitBox.Document.Selection.Link.Length > 1)
                {
                    var target = this.xRichEitBox.Document.Selection.Link.Split('\"')[1];
                    var theDoc = ContentController.GetController<DocumentController>(target);
                    if (theDoc != null && theDoc != DBTest.DBNull)
                    {
                        var pt = this.TransformToVisual(MainPage.Instance).TransformPoint(new Point());
                        pt.X -= 150;
                        pt.Y -= 50;
                        MainPage.Instance.DisplayDocument(theDoc, pt);

                    }
                    else if (target.StartsWith("http"))
                    {
                        theDoc = DocumentController.FindDocMatchingPrimaryKeys(new string[] { target });
                        if (theDoc != null && theDoc != DBTest.DBNull)
                        {
                            var pt = this.TransformToVisual(MainPage.Instance).TransformPoint(new Point());
                            pt.X -= 150;
                            pt.Y -= 50;
                            MainPage.Instance.DisplayDocument(theDoc, pt);
                        }
                        else
                        {
                            var WebDoc = DBTest.CreateWebPage(target);
                            var pt = this.TransformToVisual(MainPage.Instance).TransformPoint(new Point());
                            pt.X -= 150;
                            pt.Y -= 50;
                            MainPage.Instance.DisplayDocument(WebDoc, pt);
                        }
                    }
                }
            }
            LastS1 = s1;
            LastS2 = s2;
        }
    }
}
