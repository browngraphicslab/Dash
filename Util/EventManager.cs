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
                eventController.SetField<TextController>(KeyStore.XamlKey, displayXaml, true);
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
                EventControllers = events.ToList();
            }
        }
    }
}
