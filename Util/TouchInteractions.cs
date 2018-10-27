using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    }
}
