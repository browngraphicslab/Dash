using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Window = Windows.UI.Xaml.Window;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class Tag
    {
	   

        public string Text
        {
            get => _text;
            set { _text = value; }
        }

	    public Color Color
	    {
		    get => _color;
		    set { _color = value; }
	    }
        private string _text;
        private Color _color;
        private LinkMenu _linkMenu;
        public Grid XTagContainer
        {
            get => xTagContainer;
            set { xTagContainer = value; }
        }

		public Tag(LinkMenu linkMenu, String text, Color color)
        {
            this.InitializeComponent();
            xTagContainer.Background = new SolidColorBrush(color);
            xTagText.Text = text;
            _text = text;
            _linkMenu = linkMenu;
            _color = color;
        }


        private void XTagContainer_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (xTagContainer.BorderThickness == new Thickness(2))
            {
                Deselect();
            }

            else
            {
                Select();
            }
        }


        //public int Compare(Tag x, Tag y)
        //{
        //    return x.Text.CompareTo(y.Text);
        //}

        //public int CompareTo(object obj)
        //{
        //    if (obj is Tag tag)
        //    {
        //        return Compare(this, tag);
        //    }
        //    return 0;
        //}

        public void Deselect()
        {
            
            xTagContainer.BorderThickness = new Thickness(0);
            xTagContainer.Padding = new Thickness(4, 0, 4, 6);
        }

        public void fakeSelect()
        {
            xTagContainer.BorderThickness = new Thickness(2);
            xTagContainer.Padding = new Thickness(4, -2, 4, 6);
        }

        public void Select()
        {

            

            foreach (var tag in _linkMenu.RecentTags)
            {
                tag.Deselect();
            }

            xTagContainer.BorderThickness = new Thickness(2);
            xTagContainer.Padding = new Thickness(4, -2, 4, 6);

            bool unique = true;
            foreach (var recent in _linkMenu.RecentTags)
            {
                if (recent.Text == _text)
                {
                    unique = false;
                }
            }

            if (unique)
            {
                var doc = new DocumentController();
                doc.SetField<TextController>(KeyStore.DataKey, Text, true);
                doc.SetField<ColorController>(KeyStore.BackgroundColorKey, Color, true);

                if (_linkMenu.RecentTags.Count < 5)
                {
                    _linkMenu.RecentTags.Enqueue(this);
                    _linkMenu.RecentTagsSave.Add(doc);
                }
                else
                {
                    _linkMenu.RecentTags.Dequeue();
                    _linkMenu.RecentTagsSave.RemoveAt(0);
                    _linkMenu.RecentTags.Enqueue(this);
                    _linkMenu.RecentTagsSave.Add(doc);
                }
            }

            _linkMenu.LinkDoc.DataDocument.SetField<TextController>(KeyStore.LinkTagKey, Text, true);
            MainPage.Instance.XDocumentDecorations.AddLinkTypeButton(Text);
            MainPage.Instance.XDocumentDecorations.rebuildMenuIfNeeded();

        }

        private void DeleteButton_PointerEntered(object sender, PointerRoutedEventArgs pointerRoutedEventArgs)
        {
            xDeleteIcon.Opacity = 1;
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Hand, 1);
        }

        private void DeleteButton_PointerExited(object sender, PointerRoutedEventArgs pointerRoutedEventArgs)
        {
            xDeleteIcon.Opacity = 0.5;
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 1);
        }

        private void DeleteButton_PointerPressed(object sender, PointerRoutedEventArgs pointerRoutedEventArgs)
        {

            List<string> childList = new List<string>();
            List<string> recentList = new List<string>();

            foreach (var tag in _linkMenu.XTagContainer.Children)
            {
                childList.Add((tag as Tag).Text);
            }

            foreach (var tag in _linkMenu.RecentTags)
            {
                recentList.Add(tag.Text);
            }

            recentList.Reverse();


            DeleteTag();

            if (_linkMenu.RecentTags.Contains(this))
            {

                var temp = new List<Tag>();
                while (_linkMenu.RecentTags.Any())
                {
                    var currtag = _linkMenu.RecentTags.Dequeue();
                    if (currtag.Text != Text)
                    {
                        temp.Add(currtag);
                    }
                    _linkMenu.TagNameDict.Remove(currtag.Text);
                    


                }

                _linkMenu.RecentTags.Clear();
              
                foreach (var tag in temp)
                {
                    _linkMenu.RecentTags.Enqueue(tag);
                    _linkMenu.TagNameDict.Add(tag.Text, tag);
                  
                }

                temp.Reverse();

                foreach (var tag in temp)
                {
                    var doc = new DocumentController();
                    doc.SetField<TextController>(KeyStore.DataKey, tag.Text, true);
                    doc.SetField<ColorController>(KeyStore.BackgroundColorKey, tag.Color, true);

                }
            }
        }

        private void DeleteTag()
        {
            if (_linkMenu.TagNameDict.Count > 1 && _linkMenu.TagNameDict.ContainsKey(Text))
            {
                if (!this.Text.Equals("Annotation"))
                {
                    _linkMenu.TagNameDict.Remove(Text);
                    _linkMenu.XTagContainer.Children.Remove(this as UIElement);
                    DocumentController tempTag = new DocumentController();
                    DocumentController tempRecent = new DocumentController();
                    foreach (var doc in _linkMenu.TagsSave)
                    {
                        if (doc.GetField<TextController>(KeyStore.DataKey).ToString().Equals(Text))
                        {
                            tempTag = doc;
                        }
                    }

                    _linkMenu.TagsSave.Remove(tempTag);

                    foreach (var doc in _linkMenu.RecentTagsSave)
                    {
                        if (doc.GetField<TextController>(KeyStore.DataKey).ToString().Equals(Text))
                        {
                            tempRecent = doc;
                        }
                    }

                    _linkMenu.RecentTagsSave.Remove(tempRecent);

                    if (_linkMenu.LinkDoc.DataDocument.GetField<TextController>(KeyStore.LinkTagKey).Data.Equals(Text))
                    {
                        _linkMenu.LinkDoc.DataDocument.SetField<TextController>(KeyStore.LinkTagKey, "Annotation",
                            true);
                        MainPage.Instance.XDocumentDecorations.rebuildMenuIfNeeded();
                    }
                }
            }
        }
    }
}
