using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;

namespace Dash
{
    public static class EventManager
    {
        private static List<DocumentController> EventControllers = new List<DocumentController>();

        public static void EventOccured(DocumentController eventController, string displayXaml = null)
        {
            if (displayXaml != null)
            {
                eventController.SetHorizontalAlignment(HorizontalAlignment.Stretch);
                eventController.SetXaml(displayXaml);
            }

            EventControllers.Add(eventController);
            MainPage.Instance.MainDocument.AddToListField(KeyStore.EventManagerKey, eventController);
        }

        public static List<DocumentController> GetEvents()
        {
            var events = new List<DocumentController>(EventControllers);
            events.OrderBy(ec => ec.GetDataDocument().GetField<DateTimeController>(KeyStore.DateCreatedKey).Data);
            return events;
        }

        public static void LoadEvents(ListController<DocumentController> events)
        {
            if (events != null)
            {
                EventControllers = events.ToList();
            }
        }

        public static bool HasEvent(string text)
        {
            return EventControllers.Any(ec =>
                ec.Title.Equals(text) || ec.GetDataDocument().GetField<TextController>(KeyStore.DocumentTextKey).Data.Equals(text));
        }
    }
}
