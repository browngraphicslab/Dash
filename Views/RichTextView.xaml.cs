﻿using Dash.Controllers.Operators;
using DashShared;
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
        ObservableCollection<FontFamily> fonts = new ObservableCollection<FontFamily>();

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            "Text", typeof(RichTextFieldModel.RTD), typeof(RichTextView), new PropertyMetadata(default(RichTextFieldModel.RTD)));
        

        public RichTextFieldModel.RTD Text
        {
            get { return (RichTextFieldModel.RTD)GetValue(TextProperty); }
            set{ SetValue(TextProperty, value); }
        }

        public RichTextFieldModelController  TargetRTFController = null;
        public ReferenceFieldModelController TargetFieldReference = null;
        public Context                       TargetDocContext = null;

        public RichTextView()
        {
            this.InitializeComponent();
            Loaded   += OnLoaded;
            Unloaded += UnLoaded;

            TextChangedCallbackToken = RegisterPropertyChangedCallback(TextProperty, TextChangedCallback);
        }
        long TextChangedCallbackToken;

        private void TextChangedCallback(DependencyObject sender, DependencyProperty dp)
        {
            xRichEitBox.Document.SetText(TextSetOptions.FormatRtf, Text.RtfFormatString);
            xRichEitBox.Document.Selection.SetRange(LastS1, LastS2);
        }
        private void UnLoaded(object sender, RoutedEventArgs e)
        {
            xRichEitBox.TextChanged  -= XRichEitBoxOnTextChanged;
        }

        RichTextFieldModel.RTD GetText()
        {
            if (TargetRTFController != null)
                return TargetRTFController.Data;
            return TargetFieldReference?.Dereference(TargetDocContext)?.GetValue(TargetDocContext) as RichTextFieldModel.RTD;
        }


        private async Task<string> LoadText()
        {
            var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/rtf.txt"));
            return await FileIO.ReadTextAsync(file);
        }

        private async void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            UnLoaded(sender, routedEventArgs); // make sure we're not adding handlers twice
            
            if (GetText() != null)
                xRichEitBox.Document.SetText(TextSetOptions.FormatRtf, GetText().RtfFormatString);
            
            xRichEitBox.TextChanged += XRichEitBoxOnTextChanged;
        }

        // freezes the app
        private void XRichEitBoxOnTextChanged(object sender, RoutedEventArgs routedEventArgs)
        {
            string allText;
            xRichEitBox.Document.GetText(TextGetOptions.UseObjectText, out allText);

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

            if (allText.TrimEnd('\r') != GetText()?.ReadableString?.TrimEnd('\r'))
            {
                string allRtfText;
                xRichEitBox.Document.GetText(TextGetOptions.FormatRtf, out allRtfText);
                UnregisterPropertyChangedCallback(TextProperty, TextChangedCallbackToken);
                Text = new RichTextFieldModel.RTD(allText, allRtfText.Replace("\\pard\\tx720\\par", ""));  // RTF editor adds a trailing extra paragraph when queried -- need to strip that off
                TextChangedCallbackToken = RegisterPropertyChangedCallback(TextProperty, TextChangedCallback);
            }
            this.xRichEitBox.Document.Selection.SetRange(s1, s2);
        }

        static DocumentController findHyperlinkTarget(bool createIfNeeded, string refText)
        {
            var primaryKeys = refText.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var theDoc = DocumentController.FindDocMatchingPrimaryKeys(new List<string>(primaryKeys));
            if (theDoc == null && createIfNeeded)
            {
                if (refText.StartsWith("http"))
                {
                    theDoc = DBTest.CreateWebPage(refText);
                }
                else if (primaryKeys.Count() == 2 && primaryKeys[0] == "Filter")
                {
                    theDoc = DBFilterOperatorFieldModelController.CreateFilter(new ReferenceFieldModelController(DBTest.DBDoc.GetId(), KeyStore.DataKey), primaryKeys.Last());
                }
                else
                {
                    theDoc = new NoteDocuments.RichTextNote(NoteDocuments.PostitNote.DocumentType).Document;
                    theDoc.GetDataDocument(null).SetField(KeyStore.TitleKey, new TextFieldModelController(refText), true);
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

        private void ItalicButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.xRichEitBox.Document.Selection.CharacterFormat.Italic = FormatEffect.Toggle;
            UpdateDocument();
        }

        private void BoldButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.xRichEitBox.Document.Selection.CharacterFormat.Bold = FormatEffect.Toggle;
            UpdateDocument();
        }


        private void UnderlineButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (this.xRichEitBox.Document.Selection.CharacterFormat.Underline == UnderlineType.None)
                this.xRichEitBox.Document.Selection.CharacterFormat.Underline = UnderlineType.Single;
            else
                this.xRichEitBox.Document.Selection.CharacterFormat.Underline = UnderlineType.None;
            UpdateDocument();
        }

        void UpdateDocument()
        {
            string allText;
            xRichEitBox.Document.GetText(TextGetOptions.UseObjectText, out allText);
            string allRtfText;
            xRichEitBox.Document.GetText(TextGetOptions.FormatRtf, out allRtfText);
            UnregisterPropertyChangedCallback(TextProperty, TextChangedCallbackToken);
            Text = new RichTextFieldModel.RTD(allText, allRtfText.Replace("\\pard\\tx720\\par", ""));  // RTF editor adds a trailing extra paragraph when queried -- need to strip that off
            TextChangedCallbackToken = RegisterPropertyChangedCallback(TextProperty, TextChangedCallback);
        }
        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void xRichEitBox_GotFocus(object sender, RoutedEventArgs e)
        {
            xFormatCol.Width = new GridLength(50);
        }

        private void Grid_GotFocus(object sender, RoutedEventArgs e)
        {

            xFormatCol.Width = new GridLength(50);
        }

        private void Grid_LostFocus(object sender, RoutedEventArgs e)
        {
            xFormatCol.Width = new GridLength(0);
        }

        private void xRichEitBox_DragOver(object sender, DragEventArgs e)
        {
        }

        private void xRichEitBox_Drop(object sender, DragEventArgs e)
        {
            DocumentController theDoc = null;
            if (e.DataView.Properties.ContainsKey("DocumentControllerList"))
            {
                var docCtrls = e.DataView.Properties["DocumentControllerList"] as List<DocumentController>;
                theDoc = docCtrls.First();
            }

            string allText;
            xRichEitBox.Document.GetText(TextGetOptions.UseObjectText, out allText);

            var startPt = new Point();
            var s1 = this.xRichEitBox.Document.Selection.StartPosition;
            var s2 = this.xRichEitBox.Document.Selection.EndPosition;
            this.xRichEitBox.Document.Selection.GetPoint(HorizontalCharacterAlignment.Center, VerticalCharacterAlignment.Baseline, PointOptions.Start, out startPt);

            createRTFHyperlink(theDoc, startPt, ref s1, ref s2, false);

            if (allText.TrimEnd('\r') != GetText()?.ReadableString?.TrimEnd('\r'))
            {
                string allRtfText;
                xRichEitBox.Document.GetText(TextGetOptions.FormatRtf, out allRtfText);
                UnregisterPropertyChangedCallback(TextProperty, TextChangedCallbackToken);
                Text = new RichTextFieldModel.RTD(allText, allRtfText.Replace("\\pard\\tx720\\par", ""));  // RTF editor adds a trailing extra paragraph when queried -- need to strip that off
                TextChangedCallbackToken = RegisterPropertyChangedCallback(TextProperty, TextChangedCallback);
            }
            this.xRichEitBox.Document.Selection.SetRange(s1, s2);
            e.Handled = true;
            if (DocumentView.DragDocumentView != null)
            {
                DocumentView.DragDocumentView.OuterGrid.BorderThickness = new Thickness(0);
                DocumentView.DragDocumentView.IsHitTestVisible = true;
                DocumentView.DragDocumentView = null;
            }
        }

        void xRichEitBox_SelectionChanged(object sender, RoutedEventArgs e)
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
                        MainPage.Instance.DisplayDocument(theDoc.GetViewCopy(pt));
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
