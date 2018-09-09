using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Office.Interop.Word;
using Window = Windows.UI.Xaml.Window;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class Tag : IComparable, IComparer<Tag>
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
        private DocumentDecorations _docdecs;
        public Grid XTagContainer
        {
            get => xTagContainer;
            set { xTagContainer = value; }
        }

		public Tag(DocumentDecorations docdecs, String text, Color color)
        {
            this.InitializeComponent();
            xTagContainer.Background = new SolidColorBrush(color);
            xTagText.Text = text;
            _text = text;
            _docdecs = docdecs;
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

        //      //temporary method for telling all links associated with this tag that an additional tag has been added
        //      public void UpdateOtherTags()
        //      {
        //          //get active links from doc dec based on last-pressed btn & add this tag to them
        //          _docdecs.UpdateAllTags(this);

        //      }

        public void AddTag(DocumentController link)
        {
            link.GetDataDocument().SetField<TextController>(KeyStore.LinkTagKey, Text, true);
        }

        private void RemoveTag(DocumentController link)
        {
            link.GetDataDocument().RemoveField(KeyStore.LinkTagKey);
            //link.GetDataDocument().SetField<TextController>(KeyStore.LinkTagKey, "Annotation", true);
        }

        public int Compare(Tag x, Tag y)
        {
            return x.Text.CompareTo(y.Text);
        }

        public int CompareTo(object obj)
        {
            if (obj is Tag tag)
            {
                return Compare(this, tag);
            }

            return 0;
        }

        public void Deselect()
        {
            
            xTagContainer.BorderThickness = new Thickness(0);
            xTagContainer.Padding = new Thickness(4, 0, 4, 6);

            //var firstDoc = _docdecs.SelectedDocs.FirstOrDefault();
            //if (_docdecs.SelectedDocs.Count == 1)
            //{
            //    foreach (var direction in new LinkDirection[] { LinkDirection.ToSource, LinkDirection.ToDestination })
            //        foreach (var link in firstDoc.ViewModel.DataDocument.GetLinks(direction == LinkDirection.ToSource ? KeyStore.LinkFromKey : KeyStore.LinkToKey))
            //        {
            //            var currtags = link.GetDataDocument().GetLinkTags();
            //            if (LinkActivationManager.ActivatedDocs.Any(dv => dv.ViewModel.DocumentController.Equals(link.GetLinkedDocument(direction))))
            //            {
            //                RemoveTag(link, currtags);
            //                break;
            //            }

            //            if ((link.GetLinkTags()?.Count ?? 0) == 0)
            //            {
            //                RemoveTag(link, currtags);
            //                break;
            //            }
            //        }
            //}
        }

        public void Select()
        {

            foreach (var tag in _docdecs._tagNameDict)
            {
                tag.Value.Deselect();
            }

            foreach (var tag in _docdecs.RecentTags)
            {
                tag.Deselect();
            }


            xTagContainer.BorderThickness = new Thickness(2);
            xTagContainer.Padding = new Thickness(4, -2, 4, 6);


            //tell doc decs to change currently activated buttons 


            bool unique = true;
            foreach (var recent in _docdecs.RecentTags)
            {
                if (recent.Text == _text)
                {
                    unique = false;
                }
            }

            //var doc = new DocumentController();
            //doc.SetField<TextController>(KeyStore.DataKey, _text, true);
            //doc.SetField<ColorController>(KeyStore.BackgroundColorKey, _color, true);

            if (unique)
            {
                if (_docdecs.RecentTags.Count < 5)
                {
                    _docdecs.RecentTags.Enqueue(this);
                    _docdecs.RecentTagsSave.Add(_docdecs.TagsSave.Where(t => t.GetField<TextController>(KeyStore.DataKey).Data == Text).First());
                }
                else
                {
                    _docdecs.RecentTags.Dequeue();
                    _docdecs.RecentTagsSave.RemoveAt(0);
                    _docdecs.RecentTags.Enqueue(this);
                    _docdecs.RecentTagsSave.Add(_docdecs.TagsSave.Where(t => t.GetField<TextController>(KeyStore.DataKey).Data == Text).First());
                }
            }

            var firstDoc = _docdecs.SelectedDocs.FirstOrDefault();
            if (_docdecs.SelectedDocs.Count == 1)
            {
                //foreach (var direction in new LinkDirection[] { LinkDirection.ToSource, LinkDirection.ToDestination })
                    foreach (var link in _docdecs.CurrentLinks)
                    {
                        //if (LinkActivationManager.ActivatedDocs.Any(dv => dv.ViewModel.DocumentController.Equals(link.GetLinkedDocument(direction))))
                        //{
                        //    AddTag(link);
                        //    break;
                        //}

                        if (link.GetDataDocument().GetLinkTag() != null)
                        {
                            AddTag(link);
                            break;
                        }
                    }
            }


            _docdecs.SetPositionAndSize();
        }

        public void RidSelectionBorder()
        {
            xTagContainer.BorderThickness = new Thickness(0);

        }

        public void AddSelectionBorder()
        {
            xTagContainer.BorderThickness = new Thickness(2);

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

            foreach (var tag in _docdecs.XTagContainer.Children)
            {
                childList.Add((tag as Tag).Text);
            }

            foreach (var tag in _docdecs.RecentTags)
            {
                recentList.Add(tag.Text);
            }

            recentList.Reverse();


            DeleteTag();
            if (_docdecs.RecentTags.Contains(this))
            {

                _docdecs.RecentTagsSave.Remove(_docdecs.RecentTagsSave.FirstOrDefault(t => t.GetField<TextController>(KeyStore.DataKey).Data == Text));
                var temp = new List<Tag>();
                while (_docdecs.RecentTags.Any())
                {
                    var currtag = _docdecs.RecentTags.Dequeue();
                    if (currtag.Text != Text)
                    {
                        temp.Add(currtag);
                    }
                    _docdecs.Tags.Remove(currtag);
                    


                }

                _docdecs.RecentTags.Clear();
                foreach (var tag in temp)
                {
                    _docdecs.RecentTags.Enqueue(tag);
                    _docdecs.Tags.Add(tag);
                    _docdecs.RecentTags.Enqueue(tag);
                    _docdecs.RecentTagsSave.Remove(_docdecs.RecentTagsSave.FirstOrDefault(t => t.GetField<TextController>(KeyStore.DataKey).Data == tag.Text));
                }

                if (_docdecs.InLineTags.Any())
                {
                    var pop = _docdecs.InLineTags.Pop();

                    _docdecs.RecentTags.Enqueue(pop);
                    var doc = new DocumentController();
                    doc.SetField<TextController>(KeyStore.DataKey, pop.Text, true);
                    doc.SetField<ColorController>(KeyStore.BackgroundColorKey, pop.Color, true);

                    _docdecs.RecentTagsSave.Add(doc);

                    var tags = new List<Tag>();
                    foreach (var tag in _docdecs.XTagContainer.Children)
                    {
                        tags.Add((tag as Tag));
                    }
                    _docdecs.XTagContainer.Children.Clear();
                    _docdecs.XTagContainer.Children.Add(pop);
                    foreach (var tag in tags)
                    {
                        _docdecs.XTagContainer.Children.Add(tag);
                    }
                }

                temp.Reverse();

                foreach (var tag in temp)
                {
                    var doc = new DocumentController();
                    doc.SetField<TextController>(KeyStore.DataKey, tag.Text, true);
                    doc.SetField<ColorController>(KeyStore.BackgroundColorKey, tag.Color, true);
                    _docdecs.RecentTagsSave.Add(doc);
                }
                
            }

        }

        private void DeleteTag()
        {
            if (_docdecs.Tags.Count > 1 && _docdecs.Tags.Contains(this))
            {
                if (!this.Text.Equals("Annotation"))
                {
                    if (_docdecs.TagMap.TryGetValue(Text, out var list))
                    {
                        var newlinks = list;
                        _docdecs.TagMap.Remove(Text);
                        var oldlinks = _docdecs.TagMap["Annotation"];
                        oldlinks.AddRange(newlinks);
                        _docdecs.TagMap["Annotation"] = oldlinks;
                    }

                    _docdecs.Tags.Remove(this);
                    _docdecs.TagsSave.Remove(_docdecs.TagsSave.FirstOrDefault(t => t.GetField<TextController>(KeyStore.DataKey).Data == Text));
                    _docdecs._tagNameDict.Remove(Text);



                    _docdecs.XTagContainer.Children.Remove(this as UIElement);
                }
            }
        }
    }
}
