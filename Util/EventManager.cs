using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;

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
                eventController.SetHeight(300);
                eventController.SetHorizontalAlignment(HorizontalAlignment.Stretch);
                eventController.SetField<TextController>(KeyStore.XamlKey, @"<Grid
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:dash=""using:Dash""
    xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006"">
    <Grid.RowDefinitions>
        <RowDefinition Height=""Auto""></RowDefinition>
        <RowDefinition Height=""*""></RowDefinition>
        <RowDefinition Height=""*""></RowDefinition>
    </Grid.RowDefinitions>
    <Border BorderThickness=""2"" BorderBrush=""CadetBlue"" Background=""White"">
        <TextBlock x:Name=""xTextFieldTitle"" HorizontalAlignment=""Stretch"" Height=""Auto"" VerticalAlignment=""Top""/>
    </Border>
    <StackPanel Orientation=""Horizontal""  Grid.Row=""2"" Height=""Auto"" Background=""White"" >
        <dash:DocumentView x:Name=""xDocumentField_EventDisplayKey"" Foreground=""White"" HorizontalAlignment=""Stretch"" Grid.Row=""2"" VerticalAlignment=""Top"" />
    </StackPanel>
</Grid>", true);
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
