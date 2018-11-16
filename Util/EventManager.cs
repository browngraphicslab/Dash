using System.Collections.Generic;

namespace Dash
{
    public static class EventManager
    {
        private static List<DocumentController> EventControllers = new List<DocumentController>();

        public static void EventOccured(DocumentController eventController)
        {
            EventControllers.Add(eventController);
            MainPage.Instance.MainDocument.AddToListField(KeyStore.EventManagerKey, eventController);
        }

        public static List<DocumentController> GetEvents()
        {
            return EventControllers;
        }

        public static void LoadEvents(ListController<DocumentController> events)
        {
            if (events != null)
            {
                EventControllers = events.TypedData;
            }
        }
    }
}
