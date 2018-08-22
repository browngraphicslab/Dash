using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Office.Interop.Word;

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
	    public bool Selected = false;
	    public SolidColorBrush selectedBrush = new SolidColorBrush(Color.FromArgb(240, 64, 123, 177));

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


        //      private void XTagContainer_OnTapped(object sender, TappedRoutedEventArgs e)
        //      {
        //          if (xTagContainer.BorderBrush.Equals(selectedBrush))
        //          {
        //		Deselect();
        //	}
        //          else
        //          {
        //           Select();

        //	}
        //      }

        //      //temporary method for telling all links associated with this tag that an additional tag has been added
        //      public void UpdateOtherTags()
        //      {
        //          //get active links from doc dec based on last-pressed btn & add this tag to them
        //          _docdecs.UpdateAllTags(this);

        //      }

        //      public void AddLink(DocumentController link)
        //      {
        //          var currtags = link.GetDataDocument().GetLinkTags();



        //          currtags = new ListController<TextController>();
        //          currtags.Add(new TextController(this.Text));

        //          _docdecs.Tags.Add(this);
        //          link.GetDataDocument().SetField(KeyStore.LinkTagKey, currtags, true);
        //      }

        //      private void RemoveLink(DocumentController link, ListController<TextController> currtags)
        //      {

        //          currtags.Clear();


        //          if (_docdecs.Tags.Contains(this))
        //          {
        //              _docdecs.Tags.Remove(this);
        //          }

        //          link.GetDataDocument().SetField(KeyStore.LinkTagKey, currtags, true);
        //      }

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

        //   public void Deselect()
        //   {
        //    Selected = false;
        //    XTagContainer.BorderBrush = new SolidColorBrush(Colors.Transparent);
        //          var firstDoc = _docdecs.SelectedDocs.FirstOrDefault();
        //	if (_docdecs.SelectedDocs.Count == 1)
        //	{
        //              foreach (var direction in new LinkDirection[] { LinkDirection.ToSource, LinkDirection.ToDestination })
        //		    foreach (var link in firstDoc.ViewModel.DataDocument.GetLinks(direction == LinkDirection.ToSource ? KeyStore.LinkFromKey : KeyStore.LinkToKey))
        //		    {
        //                      var currtags = link.GetDataDocument().GetLinkTags();
        //			    if (LinkActivationManager.ActivatedDocs.Any(dv => dv.ViewModel.DocumentController.Equals(link.GetLinkedDocument(direction))))
        //			    {
        //				    RemoveLink(link, currtags);
        //				    break;
        //			    }

        //                      if ((link.GetLinkTags()?.Count ?? 0) == 0)
        //                      {
        //                          RemoveLink(link, currtags);
        //                          break;
        //                      }
        //                  }
        //	}
        //}

        //   public void Select()
        //   {
        //    Selected = true;

        //       foreach (var tag in _docdecs.RecentTags)
        //       {
        //           tag.Deselect();
        //       }

        //          xTagContainer.BorderBrush = selectedBrush;
        //          //xTagContainer.BorderBrush = new SolidColorBrush(Colors.DodgerBlue);

        //          //tell doc decs to change currently activated buttons 


        //          bool unique = true;
        //          foreach (var recent in _docdecs.RecentTags)
        //          {
        //              if (recent.Text == _text)
        //              {
        //                  unique = false;
        //              }
        //          }

        //          var doc = new DocumentController();
        //	doc.SetField<TextController>(KeyStore.DataKey, _text, true);
        //	doc.SetField<ColorController>(KeyStore.BackgroundColorKey, _color, true);

        //          if (unique)
        //          {
        //              if (_docdecs.RecentTags.Count < 5)
        //		{
        //			_docdecs.RecentTags.Enqueue(this);
        //			_docdecs.RecentTagsSave.Add(doc);
        //		}
        //		else
        //		{
        //			_docdecs.RecentTags.Dequeue();
        //			_docdecs.RecentTagsSave.RemoveAt(0);
        //			_docdecs.RecentTags.Enqueue(this);
        //			_docdecs.RecentTagsSave.Add(doc);
        //		}
        //	}

        //          var firstDoc = _docdecs.SelectedDocs.FirstOrDefault();
        //          if (_docdecs.SelectedDocs.Count == 1)
        //          {
        //              foreach (var direction in new LinkDirection[] { LinkDirection.ToSource, LinkDirection.ToDestination })
        //                  foreach (var link in firstDoc.ViewModel.DataDocument.GetLinks(direction == LinkDirection.ToSource ? KeyStore.LinkFromKey : KeyStore.LinkToKey))
        //                  {
        //                      if (LinkActivationManager.ActivatedDocs.Any(dv => dv.ViewModel.DocumentController.Equals(link.GetLinkedDocument(direction))))
        //                      {
        //                          AddLink(link);
        //                          break;
        //                      }

        //                      if ((link.GetLinkTags()?.Count ?? 0) == 0)
        //                      {
        //                          AddLink(link);
        //                          break;
        //                      }
        //                  }
        //          }

        //}

        //   public void RidSelectionBorder()
        //   {
        //    xTagContainer.BorderBrush = new SolidColorBrush(Colors.Transparent);

        //   }

        //public void AddSelectionBorder()
        //{
        //	xTagContainer.BorderBrush = selectedBrush;

        //}
    }
}
