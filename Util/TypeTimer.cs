using System;
using Windows.UI.Xaml;

namespace Dash
{
    //this class uses a timer to batch type events for Undo
    static class TypeTimer
    {
        private static DispatcherTimer disTimer;

        public static void typeEvent()
        {
            if (disTimer == null)
            {
                //start batch 
                UndoManager.StartBatch();
                //and set up timer
                disTimer = new DispatcherTimer();
                disTimer.Tick += disTimer_Tick;
                disTimer.Interval = new TimeSpan(0, 0, 1);
                disTimer.Start();
            }
            else
            {
                //reset timer
                disTimer.Stop();
                disTimer.Start();
            }
        }

        static void disTimer_Tick(object sender, object e)
        {
            disTimer.Stop();
            //time out - finish batch
            UndoManager.EndBatch();

            disTimer = null;
        }
    }
}
