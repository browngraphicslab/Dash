using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public static class TemplateList {
        private static ListController<DocumentController> _templates;
        public static ListController<DocumentController> Templates
        {
            get
            {
                if(_templates != null) {
                    return _templates;
                }
                
                _templates = new ListController<DocumentController> {
                    new DocumentController(new Dictionary<KeyController, FieldControllerBase>{
                        [KeyStore.TitleKey] = new TextController("CardTemplate.xaml"),
                        [KeyStore.XamlKey] = new TextController(@"
<Grid
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:dash=""using:Dash""
    xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006"">

    <StackPanel Orientation=""Horizontal"" HorizontalAlignment=""Center"">
        <StackPanel>
            <Border BorderThickness=""2"" BorderBrush=""CadetBlue"" Background=""White"">
                <dash:EditableTextBlock x:Name=""xTextField0"" Text=""Title"" HorizontalAlignment=""Left"" VerticalAlignment=""Top"" />
            </Border>
            <Border BorderThickness=""10"" BorderBrush=""CadetBlue"" Background=""White"" Margin=""0 0 0 0"" Width=""150"" Height=""160"">
                <dash:EditableImage x:Name=""xImageField1"" HorizontalAlignment=""Left"" VerticalAlignment=""Top"" />
            </Border>
        </StackPanel>
        <Border BorderThickness=""2"" BorderBrush=""CadetBlue"" Margin =""0 40 0 0"" Width =""146"">
            <StackPanel Margin=""0 0 0 0"">
                <dash:EditableTextBlock x:Name=""xTextField2"" Text=""Default1"" HorizontalAlignment=""Left"" VerticalAlignment=""Center"" Padding=""0 0 5 0"" Margin=""0 10 0 0"" />
                <dash:EditableTextBlock x:Name=""xTextField3"" Text=""Default2"" HorizontalAlignment=""Left"" VerticalAlignment=""Center"" Padding=""0 0 5 0"" Margin=""0 0 0 0"" />
                <dash:EditableTextBlock x:Name=""xTextField4"" Text=""Default3"" HorizontalAlignment=""Left"" VerticalAlignment=""Center"" Padding=""0 0 5 0"" Margin=""0 0 0 0"" />
                <dash:EditableTextBlock x:Name=""xTextField5"" Text=""Default4"" HorizontalAlignment=""Left"" VerticalAlignment=""Center"" Padding=""0 0 5 0"" Margin=""0 0 0 0"" />
                <dash:EditableTextBlock x:Name=""xTextField6"" Text=""Default5"" HorizontalAlignment=""Left"" VerticalAlignment=""Center"" Padding=""0 0 5 0"" Margin=""0 0 0 0"" />



            </StackPanel>
        </Border>
    </StackPanel>
</Grid>"),
                    }, DocumentType.DefaultType),
                    new DocumentController(new Dictionary<KeyController, FieldControllerBase>{
                        [KeyStore.TitleKey] = new TextController("CitationTemplate.xaml"),
                        [KeyStore.XamlKey] = new TextController(@"
<Grid
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:dash=""using:Dash""
    xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006"">
    <Grid.RowDefinitions>
        <RowDefinition Height=""Auto""></RowDefinition>
        <RowDefinition Height=""*""></RowDefinition>
        <RowDefinition Height=""Auto""></RowDefinition>
    </Grid.RowDefinitions>
    <Border BorderThickness=""2"" BorderBrush=""CadetBlue"" Background=""White"">
        <TextBlock x:Name=""xTextField0"" Text=""DOC TITLE"" HorizontalAlignment=""Stretch"" Height=""25"" VerticalAlignment=""Top""/>
    </Border>
    <Border Grid.Row=""1"" Background=""CadetBlue"" >
        <dash:PdfView x:Name=""xPdfField1"" Foreground=""White"" HorizontalAlignment=""Stretch"" Grid.Row=""1"" VerticalAlignment=""Top"" />
    </Border>
    <StackPanel Orientation=""Horizontal""  Grid.Row=""2"" Height=""30"" Background=""White"" >
        <!--<TextBlock Text=""Author:"" HorizontalAlignment=""Stretch"" FontStyle=""Italic"" FontSize=""9"" VerticalAlignment=""Center"" Margin=""0 5 0 0"" Padding=""0 0 5 0"" />-->
        <dash:EditableTextBlock x:Name=""xTextField2"" Text=""author"" HorizontalAlignment=""Stretch"" VerticalAlignment=""Center"" Padding=""0 0 5 0"" />
        <!--<TextBlock Text=""Created: "" HorizontalAlignment=""Stretch"" FontStyle=""Italic"" FontSize=""9"" VerticalAlignment=""Center"" Margin=""0 5 0 0"" Padding=""0 0 5 0"" />-->
        <TextBlock x:Name=""xTextField3"" Text=""created"" HorizontalAlignment=""Stretch"" VerticalAlignment=""Center"" />
    </StackPanel>
</Grid>"),
                    }, DocumentType.DefaultType),
                    new DocumentController(new Dictionary<KeyController, FieldControllerBase>{
                        [KeyStore.TitleKey] = new TextController("NoteTemplate.xaml"),
                        [KeyStore.XamlKey] = new TextController(@"
<Grid
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:dash=""using:Dash""
    xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006"">
    <Grid.RowDefinitions>
        <RowDefinition Height=""Auto""></RowDefinition>
        <RowDefinition Height=""*""></RowDefinition>
        <RowDefinition Height=""Auto""></RowDefinition>
    </Grid.RowDefinitions>
    <Border BorderThickness=""2"" BorderBrush=""CadetBlue"" Background=""White"">
        <TextBlock x:Name=""xTextField0"" Text=""DOC TITLE"" HorizontalAlignment=""Stretch"" Height=""25"" VerticalAlignment=""Top""/>
    </Border>
    <Border Grid.Row=""1"" Background=""CadetBlue"" >
        <dash:RichEditView x:Name=""xRichTextField1"" Foreground=""White"" HorizontalAlignment=""Stretch"" Grid.Row=""1"" VerticalAlignment=""Top"" />
    </Border>
    <StackPanel Orientation=""Horizontal""  Grid.Row=""2"" Height=""30"" Background=""White"" >
        <!--<TextBlock Text=""Author:"" HorizontalAlignment=""Stretch"" FontStyle=""Italic"" FontSize=""9"" VerticalAlignment=""Center"" Margin=""0 5 0 0"" Padding=""0 0 5 0"" />-->
        <dash:EditableTextBlock x:Name=""xTextField2"" Text=""author"" HorizontalAlignment=""Stretch"" VerticalAlignment=""Center"" Padding=""0 0 5 0"" />
        <!--<TextBlock Text=""Created: "" HorizontalAlignment=""Stretch"" FontStyle=""Italic"" FontSize=""9"" VerticalAlignment=""Center"" Margin=""0 5 0 0"" Padding=""0 0 5 0"" />-->
        <TextBlock x:Name=""xTextField3"" Text=""created"" HorizontalAlignment=""Stretch"" VerticalAlignment=""Center"" />
    </StackPanel>
</Grid>
"),
                    }, DocumentType.DefaultType),
                };

                return _templates;
            }
        }

        public enum TemplateType
        {
             Card,
             Citation,
             Note,
                    }
    }
}