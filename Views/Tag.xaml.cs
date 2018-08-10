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
            if (xTagContainer.BorderBrush.Equals(selectedBrush))
            {
				Deselect();
			}
            else
            {
	            Select();

			}
        }

        private void AddLink(DocumentController link, ListController<TextController> currtags)
        {
            var uniqueTag = true;

            if (currtags != null)
            {
                foreach (var tag in currtags)
                {
                    if (tag.Data == this.Text)
                    {
                        uniqueTag = false;
                    }
                }
                if (uniqueTag)
                {
                    currtags.Add(new TextController(this.Text));
                }
            }
            else
            {
                currtags = new ListController<TextController>();
                currtags.Add(new TextController(this.Text));
            }


            link.GetDataDocument()
                .SetField(KeyStore.LinkTagKey, currtags, true);
        }

        private void RemoveLink(DocumentController link, ListController<TextController> currtags)
        {
            var index = 0;
            if (currtags != null)
            {
                foreach (var tag in currtags)
                {
                    if (tag.Data == this.Text)
                    {
                        index = currtags.IndexOf(tag);
                    }
                }
                currtags.RemoveAt(index);
            }

            link.GetDataDocument()
                .SetField(KeyStore.LinkTagKey, currtags, true);
            
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
		    Selected = false;
		    xTagContainer.BorderBrush = new SolidColorBrush(Colors.Transparent);
			if (_docdecs.SelectedDocs.Count == 1)
			{
				ListController<DocumentController> linksFrom = _docdecs.SelectedDocs.First().ViewModel.DataDocument.GetLinks(KeyStore.LinkFromKey);

				if (linksFrom != null)
				{
					foreach (var link in linksFrom)
					{
						var currtags = link.GetDataDocument().GetField<ListController<TextController>>(KeyStore.LinkTagKey);
						if (LinkActivationManager.ActivatedDocs.Any(dv => dv.ViewModel.DocumentController.Equals(link.GetLinkedDocument(LinkDirection.ToSource))))
						{


							RemoveLink(link, currtags);
							break;
						}

						if ((link.GetField<ListController<TextController>>(KeyStore.LinkTagKey)?.Count ?? 0) == 0)
						{
							RemoveLink(link, currtags);
							break;
						}
					}
				}



				ListController<DocumentController> linksTo = _docdecs.SelectedDocs.First().ViewModel.DataDocument.GetLinks(KeyStore.LinkToKey);

				if (linksTo != null)
				{
					foreach (var link in linksTo)
					{
						var currtags = link.GetDataDocument().GetField<ListController<TextController>>(KeyStore.LinkTagKey);
						if (LinkActivationManager.ActivatedDocs.Any(dv => dv.ViewModel.DocumentController.Equals(link.GetLinkedDocument(LinkDirection.ToDestination))))
						{

							RemoveLink(link, currtags);
							break;
						}

						if ((link.GetField<ListController<TextController>>(KeyStore.LinkTagKey)?.Count ?? 0) == 0)
						{
							RemoveLink(link, currtags);
							break;
						}
					}
				}


			}
		}

	    public void Select()
	    {
		    Selected = true;

		    xTagContainer.BorderBrush = selectedBrush;
			//xTagContainer.BorderBrush = new SolidColorBrush(Colors.DodgerBlue);

			//tell doc decs to change currently activated buttons 

			
			bool unique = true;
			foreach (var recent in _docdecs.RecentTags)
			{
				if (recent.Text == _text)
				{
					unique = false;
				}
			}

			var doc = new DocumentController();
			doc.SetField<TextController>(KeyStore.DataKey, _text, true);
			doc.SetField<ColorController>(KeyStore.BackgroundColorKey, _color, true);

			if (unique)
			{
				if (_docdecs.RecentTags.Count < 5)
				{
					_docdecs.RecentTags.Enqueue(this);
					_docdecs.RecentTagsSave.Add(doc);
				}
				else
				{
					_docdecs.RecentTags.Dequeue();
					_docdecs.RecentTagsSave.RemoveAt(0);
					_docdecs.RecentTags.Enqueue(this);
					_docdecs.RecentTagsSave.Add(doc);
				}
			}

			if (_docdecs.SelectedDocs.Count == 1)
			{
				ListController<DocumentController> linksFrom = _docdecs.SelectedDocs.First().ViewModel.DataDocument.GetLinks(KeyStore.LinkFromKey);

				if (linksFrom != null)
				{
					foreach (var link in linksFrom)
					{
						var currtags = link.GetDataDocument().GetField<ListController<TextController>>(KeyStore.LinkTagKey);
						if (LinkActivationManager.ActivatedDocs.Any(dv => dv.ViewModel.DocumentController.Equals(link.GetLinkedDocument(LinkDirection.ToSource))))
						{
							AddLink(link, currtags);
							break;
						}

						if ((link.GetField<ListController<TextController>>(KeyStore.LinkTagKey)?.Count ?? 0) == 0)
						{

							AddLink(link, currtags);
							break;
						}
					}
				}



				ListController<DocumentController> linksTo = _docdecs.SelectedDocs.First().ViewModel.DataDocument.GetLinks(KeyStore.LinkToKey);

				if (linksTo != null)
				{
					foreach (var link in linksTo)
					{
						var currtags = link.GetDataDocument().GetField<ListController<TextController>>(KeyStore.LinkTagKey);
						if (LinkActivationManager.ActivatedDocs.Any(dv => dv.ViewModel.DocumentController.Equals(link.GetLinkedDocument(LinkDirection.ToDestination))))
						{

							AddLink(link, currtags);
							break;
						}

						if ((link.GetField<ListController<TextController>>(KeyStore.LinkTagKey)?.Count ?? 0) == 0)
						{

							AddLink(link, currtags);
							break;
						}
					}
				}
			

			}
			
		}

	    public void RidSelectionBorder()
	    {
		    xTagContainer.BorderBrush = new SolidColorBrush(Colors.Transparent);

	    }

		public void AddSelectionBorder()
		{
			xTagContainer.BorderBrush = selectedBrush;

		}
	}
}
