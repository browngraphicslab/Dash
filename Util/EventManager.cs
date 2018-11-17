using System.Collections.Generic;

namespace Dash
{
    public static class EventManager
    {
        private static List<DocumentController> EventControllers = new List<DocumentController>();

        public static void EventOccured(DocumentController eventController, DocumentController eventDisplay = null)
        {
            if (eventDisplay != null)
            {
                eventController.SetField(KeyStore.EventDisplayKey, eventDisplay, true);
                eventController.SetField<TextController>(KeyStore.XamlKey, , true);
            }

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
