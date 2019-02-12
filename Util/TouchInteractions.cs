using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Foundation;
using Windows.UI.Xaml.Input;

namespace Dash
{
    static class TouchInteractions 
    {

        //records number of fingers on screen for touch interactions
        private static int numFingers;
        public static int NumFingers
        {
            get => numFingers;
            set
            {
                if (value >= 0)
                {
                    numFingers = value;
                    if (numFingers == 0)
                    {
                        isPanning = false;
                        CurrInteraction = TouchInteraction.None;
                    }
                }
            } 
        }
        
        public static TouchInteraction CurrInteraction = TouchInteraction.None;

        public enum TouchInteraction
        {
            Zoom,
            Pan,
            Marquee,
            DocumentManipulation,
            None
        }

        public static List<PointerRoutedEventArgs> handledTouch = new List<PointerRoutedEventArgs>();


        public static bool DraggingDoc;
        public static bool isPanning;
        public static DocumentView docHeld;

        public static DocumentView HeldDocument {
            get => docHeld;
            set
            {
                docHeld = value;
                if (value == null) HideMenu();
                Debug.WriteLine("docHeld value: " + value);
            }
        }

        public static void ShowMenu(Point point, DocumentView view = null, CollectionFreeformView col = null)
        {
            MainPage.Instance.xTouchMenu.ShowMenu(point, col);
        }

        public static void HideMenu()
        {
            MainPage.Instance.xTouchMenu.HideMenuAsync();
        }

        public static void TryShowMenu(Point position, CollectionFreeformView marqueeCol = null)
        {
            if ((NumFingers == 2 && HeldDocument != null) || (marqueeCol?._isMarqueeActive ?? false))
            {
                ShowMenu(position, HeldDocument, marqueeCol);
            }
            else
            {
                //HideMenu();
            }
        }

        public static bool HoldingPDF()
        {
            return HeldDocument?.ViewModel == null ? false :
                HeldDocument.ViewModel.DocumentController.DocumentType.Equals(PdfBox.DocumentType);
        }

        public static bool HoldingRichEdit()
        {
            return HeldDocument?.ViewModel == null ? false :
                HeldDocument.ViewModel.DocumentController.DocumentType.Equals(RichTextBox.DocumentType);
        }

        public static void DropCompleted(DocumentView droppedDoc)
        {
            NumFingers = 0;
            //inform pdf of drop (if holding pdf
            //if (HoldingPDF()) droppedDoc.GetFirstDescendantOfType<PdfAnnotationView>()?.PdfOnDrop();
            if (HoldingRichEdit()) NumFingers--;
            if (HeldDocument == droppedDoc) HeldDocument = null;
            CurrInteraction = TouchInteraction.None;
        }

        internal static bool CanMarquee()
        {
            return CurrInteraction != TouchInteraction.DocumentManipulation && CurrInteraction != TouchInteraction.Pan;
        }
    }
}
