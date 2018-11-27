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
                        [KeyStore.TitleKey] = new TextController("ArticleTemplate.xaml"),
                        [KeyStore.XamlKey] = new TextController(@"
<Page
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:dash=""using:Dash""
    xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006"">

    <StackPanel Padding=""10"">

        <TextBlock x:Name=""xTextField0"" FontWeight=""Bold"" FontSize=""25"" TextAlignment=""Right"" Text=""BREATH OF THE WILD"" HorizontalAlignment=""Stretch"" VerticalAlignment=""Top""/>
    <TextBlock x:Name=""xTextField1"" FontStyle=""Italic"" FontSize=""18"" TextAlignment=""Right"" Text=""The Legend of Zelda"" HorizontalAlignment=""Stretch"" VerticalAlignment=""Top""/>

    <dash:EditableImage x:Name=""xImageField2"" HorizontalAlignment=""Center"" VerticalAlignment=""Center"" Height=""Auto""
                    Margin=""0 10""/>

    <TextBlock x:Name=""xTextField3"" Text=""The Legend of Zelda: Breath of the Wild[a] is an action-adventure game developed and published by Nintendo. An entry in the longrunning The Legend of Zelda series, it was released for the Nintendo Switch and Wii U consoles on March 3, 2017."" TextWrapping=""Wrap""/>
    </StackPanel>
</Page>
"),
                    }, DocumentType.DefaultType),
                    new DocumentController(new Dictionary<KeyController, FieldControllerBase>{
                        [KeyStore.TitleKey] = new TextController("BiographyTemplate.xaml"),
                        [KeyStore.XamlKey] = new TextController(@"
<Page
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:dash=""using:Dash""
    xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006"">

    <Grid Padding=""10"">
        <Grid.RowDefinitions>
            <RowDefinition Height=""Auto""/>
            <RowDefinition Height=""Auto""/>
        </Grid.RowDefinitions>

        <Grid.ColumnDefinitions>
            <ColumnDefinition Width=""3*""/>
            <ColumnDefinition Width=""2*""/>
        </Grid.ColumnDefinitions>

        <Border Grid.Row=""0"" Grid.Column=""0"" Grid.ColumnSpan=""2"" BorderThickness=""1"" BorderBrush=""CadetBlue"" Padding=""10"">
            <TextBlock x:Name=""xTextField0"" FontWeight=""Bold"" FontSize=""25"" TextAlignment=""Center"" Text=""NESS (EARTHBOUND)"" HorizontalAlignment=""Stretch"" VerticalAlignment=""Top""></TextBlock>
        </Border>

        <dash:EditableImage x:Name=""xImageField1"" Grid.Row=""1"" Grid.Column=""0"" HorizontalAlignment=""Center"" VerticalAlignment=""Center"" Height=""Auto""
                    Margin=""0 10""/>
        
        <StackPanel Grid.Row=""1"" Grid.Column=""1"" Padding=""10"">
            <!--FontSize=""18"" FontWeight=""Bold""-->
            <TextBox x:Name=""xFirstHeader""  Text=""PlaceHolderText2"" TextWrapping=""Wrap""/>
            <TextBlock x:Name=""xTextField3"" Text=""Ness is the silent main protagonist of EarthBound and is analogous to Ninten and Lucas in their respective games. He greatly enjoys baseball; not only are most of his weapons various types of baseball bats, but he can also equip several baseball caps."" TextWrapping=""Wrap""/>
            <!--FontSize=""18"" FontWeight=""Bold""-->
            <TextBox x:Name=""xSecondHeader""  Text=""PlaceHolderText4"" TextWrapping=""Wrap""/>
            <TextBlock x:Name=""xTextField5"" Text=""At the beginning of EarthBound, Ness is awoken from a sound sleep by the impact of a meteorite north of his house in Onett. "" TextWrapping=""Wrap""/>
        </StackPanel>
    </Grid>
</Page>
"),
                    }, DocumentType.DefaultType),
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
        <Border BorderThickness=""2"" BorderBrush=""CadetBlue"" Margin =""0 0 0 0"" Width =""146"">
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
                        [KeyStore.TitleKey] = new TextController("FlashcardTemplate.xaml"),
                        [KeyStore.XamlKey] = new TextController(@"
<Grid
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:dash=""using:Dash""
    xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006"">

    <Grid Padding=""20"">
        <Grid.RowDefinitions>
            <RowDefinition Height=""Auto""></RowDefinition>
            <RowDefinition Height=""*""></RowDefinition>
        </Grid.RowDefinitions>
        <StackPanel>
            <TextBlock x:Name=""xTextField0"" FontWeight=""Bold"" FontSize=""25"" TextAlignment=""Left"" Text=""Paper Title"" HorizontalAlignment=""Stretch"" VerticalAlignment=""Top""/>
            <TextBlock x:Name=""xTextField1"" FontStyle=""Italic"" FontSize=""18"" TextAlignment=""Left"" Text=""Paper Author"" HorizontalAlignment=""Stretch"" VerticalAlignment=""Top""/>
            <TextBlock x:Name=""xTextField2"" FontStyle=""Italic"" FontSize=""18"" TextAlignment=""Left"" Text=""Paper Date"" HorizontalAlignment=""Stretch"" VerticalAlignment=""Top""/>
            <TextBlock x:Name=""xTextField3"" FontStyle=""Italic"" FontSize=""18"" TextAlignment=""Left"" Text=""Publication Venue"" HorizontalAlignment=""Stretch"" VerticalAlignment=""Top""/>
            <TextBlock x:Name=""xTextField4"" Margin=""0 20 0 0"" Text=""ABSTRACT: The Legend of Zelda: Breath of the Wild[a] is an action-adventure game developed and published by Nintendo. An entry in the longrunning The Legend of Zelda series, it was released for the Nintendo Switch and Wii U consoles on March 3, 2017."" TextWrapping=""Wrap""/>
            <TextBlock x:Name=""xTextField5"" Margin=""0 20 0 20"" Text=""KEYWORDS, ANOTHER KEYWORD, MORE"" HorizontalAlignment=""Center"" VerticalAlignment=""Top""/>
        </StackPanel>
        <Border Grid.Row=""1"" Background=""CadetBlue"" >
            <dash:PdfView x:Name=""xPdfField6"" Foreground=""White"" HorizontalAlignment=""Stretch"" Grid.Row=""1"" VerticalAlignment=""Top"" />
        </Border>
    </Grid>
</Grid>
"),
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
<<<<<<< HEAD
        <dash:RichEditView x:Name=""xRichTextField1"" Foreground=""White"" HorizontalAlignment=""Stretch"" Grid.Row=""1"" VerticalAlignment=""Top"" />
=======
        <dash:RichEditView x:Name=""xRichTextFieldData"" Foreground=""White"" HorizontalAlignment=""Stretch"" Grid.Row=""1"" VerticalAlignment=""Top"" />
>>>>>>> 9a761d7359006deb405b4d1921897b6d2698da28
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
                    new DocumentController(new Dictionary<KeyController, FieldControllerBase>{
                        [KeyStore.TitleKey] = new TextController("ProfileTemplate.xaml"),
                        [KeyStore.XamlKey] = new TextController(@"
<Page
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:dash=""using:Dash""
    xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006"">

    <Grid Padding=""10"">
        <Grid.RowDefinitions>
            <RowDefinition Height=""Auto""/>
            <RowDefinition Height=""Auto""/>
            <RowDefinition Height=""Auto""/>
        </Grid.RowDefinitions>

        <Border Grid.Row=""0"" BorderThickness=""1"" BorderBrush=""CadetBlue"" Padding=""10"">
            <TextBlock x:Name=""xTextField0"" FontWeight=""Bold"" FontSize=""25"" TextAlignment=""Center"" Text=""PIRANHA PLANT"" HorizontalAlignment=""Stretch"" VerticalAlignment=""Top""></TextBlock>
        </Border>

        <dash:EditableImage x:Name=""xImageField1"" Grid.Row=""1"" HorizontalAlignment=""Center"" VerticalAlignment=""Center"" Height=""Auto""
                      Margin=""0 10""/>

        <StackPanel Grid.Row=""2"" Padding=""10"">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width=""*""/>
                    <ColumnDefinition Width=""2*""/>
                </Grid.ColumnDefinitions>
                <TextBox Grid.Column=""0"" BorderThickness=""0"" x:Name=""xFirstHeader"" Text=""PlaceHolderText2"" TextWrapping=""Wrap""/>
                <TextBlock Grid.Column=""1"" x:Name=""xTextField3"" Text=""70""/>
            </Grid>
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width=""*""/>
                    <ColumnDefinition Width=""2*""/>
                </Grid.ColumnDefinitions>
                <TextBox Grid.Column=""0"" BorderThickness=""0"" x:Name=""xSecondHeader"" Text=""PlaceHolderText4"" TextWrapping=""Wrap""/>
                <TextBlock Grid.Column=""1"" x:Name=""xTextField5"" Text=""Super Mario Bros""/>
            </Grid>
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width=""*""/>
                    <ColumnDefinition Width=""2*""/>
                </Grid.ColumnDefinitions>
                <TextBox Grid.Column=""0"" BorderThickness=""0"" x:Name=""xThirdHeader"" Text=""PlaceHolderText6"" TextWrapping=""Wrap""/>
                <TextBlock Grid.Column=""1"" x:Name=""xTextField7"" Text=""Ultimate""/>
            </Grid>
        </StackPanel>
    </Grid>
</Page>
"),
                    }, DocumentType.DefaultType),
                    new DocumentController(new Dictionary<KeyController, FieldControllerBase>{
                        [KeyStore.TitleKey] = new TextController("TitleTemplate.xaml"),
                        [KeyStore.XamlKey] = new TextController(@"
<Page
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:dash=""using:Dash""
    xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006"">

    <Grid>
        <Border BorderThickness=""2"" BorderBrush=""CadetBlue"" Background=""White"">
            <TextBlock x:Name=""xTextField0"" Text=""DOC TITLE"" HorizontalAlignment=""Stretch"" Height=""20"" VerticalAlignment=""Top""/>
        </Border>
    </Grid>
</Page>
"),
                    }, DocumentType.DefaultType),
                };

                return _templates;
            }
        }

        public enum TemplateType
        {
             Article,
             Biography,
             Card,
             Citation,
             Flashcard,
             Note,
             Profile,
             Title,
                        None
        }
    }
}