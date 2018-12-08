using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Toolkit.Uwp.UI.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class LinkMenu : UserControl
    {

        public Queue<Tag> RecentTags
        {
            get => _recentTags;
            set { _recentTags = value; }
        }

        private Queue<Tag> _recentTags;

        public WrapPanel XTagContainer => xTagContainer;

        //these lists save the RecentTags and Tags in between refreshes/restarts so that they are preserved for the user
        public ListController<DocumentController> RecentTagsSave;
        public ListController<DocumentController> TagsSave;

        //_tagNameDict is used for the actual tags graphically added into the tag/link pane. it contains a list of names of the tags paired with the tags themselves.
        public Dictionary<string, Tag> TagNameDict;
        public DocumentViewModel LinkDoc => DataContext as DocumentViewModel;


        public LinkMenu()
        {
            this.InitializeComponent();
            //Tags = new List<Tag>();
            _recentTags = new Queue<Tag>();
            TagNameDict = new Dictionary<string, Tag>();
            DataContextChanged += LinkMenu_DataContextChanged;
            Unloaded += LinkMenu_Unloaded;


        }

        public static Color? GetTagColor(string name)
        {
            var settingsDoc = MainPage.Instance.MainDocument.GetDataDocument().GetField<DocumentController>(KeyStore.SettingsDocKey);

            foreach (var documentController in settingsDoc.GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.TagsKey))
            {
                var tagName = documentController.GetField<TextController>(KeyStore.DataKey).ToString();
                if (tagName == name)
                {
                    return  documentController.GetField<ColorController>(KeyStore.BackgroundColorKey).Data;
                }
            }
            return null;
        }

        private void LinkMenu_DataContextChanged(object sender, DataContextChangedEventArgs e)
        {
            //if (LinkDoc == null || !this.IsInVisualTree())
            //{
            //    return;
            //}

            if (LinkDoc == null)
            {
                return;
            }
            xTagContainer.Children.Clear();
            _recentTags.Clear();
            TagNameDict.Clear();
            var settingsDoc = MainPage.Instance.MainDocument.GetDataDocument().GetField<DocumentController>(KeyStore.SettingsDocKey);
            RecentTagsSave = settingsDoc.GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.RecentTagsKey);
            TagsSave = settingsDoc.GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.TagsKey);

            if (TagsSave.Count == 0 || RecentTagsSave.Count == 0)
            {
                var doc = new DocumentController();
                doc.SetField<TextController>(KeyStore.DataKey, "Annotation", true);
                doc.SetField<ColorController>(KeyStore.BackgroundColorKey, Colors.DarkSlateGray, true);
                if(!TagsSave.Any()) TagsSave.Add(doc);
                if(!RecentTagsSave.Any()) RecentTagsSave.Add(doc);
            }

            foreach (var documentController in RecentTagsSave.Reverse())
            {
                RecentTags.Enqueue(new Tag(this, documentController.GetField<TextController>(KeyStore.DataKey).ToString(), documentController.GetField<ColorController>(KeyStore.BackgroundColorKey).Data));
            }

            foreach (var documentController in TagsSave)
            {

                var tag = new Tag(this, documentController.GetField<TextController>(KeyStore.DataKey).ToString(), documentController.GetField<ColorController>(KeyStore.BackgroundColorKey).Data);
                if (!TagNameDict.ContainsKey(tag.Text))
                {
                    TagNameDict.Add(tag.Text, tag);
                }
            }

            //graphically displays the reloaded recent tags
            foreach (var tag in RecentTags)
            {
                xTagContainer.Children.Add(tag);
            }

            //var linkDoc = (DataContext as DocumentView).ViewModel.DataDocument.GetLinks(KeyStore.LinkToKey).FirstOrDefault() ?? (DataContext as DocumentView).ViewModel.DataDocument.GetLinks(KeyStore.LinkFromKey).FirstOrDefault();
            var binding = new FieldBinding<FieldControllerBase, TextController>
            {
                Document = LinkDoc.DataDocument,
                Key = KeyStore.DataKey,
                Mode = BindingMode.TwoWay
            };
            xDescriptionBox.AddFieldBinding(RichEditView.TextProperty, binding);

            foreach (var tag in xTagContainer.Children)
            {
                if (((Tag) tag).Text.Equals(LinkDoc.DataDocument.GetField<TextController>(KeyStore.LinkTagKey).Data))
                {
                    ((Tag) tag).fakeSelect();
                }
            }

            if (!this.IsInVisualTree())
            {
                return;
            }
            var behavior = LinkDoc.DataDocument.GetLinkBehavior();
            switch (behavior)
            {
            case LinkBehavior.Follow:
                xTypeFollow.IsChecked = true;
                break;
            case LinkBehavior.Annotate:
                xTypeAnnotation.IsChecked = true;
                break;
            case LinkBehavior.Dock:
                xTypeDock.IsChecked = true;
                break;
            case LinkBehavior.Float:
                xTypeFloat.IsChecked = true;
                break;
            }
        }

        private void LinkMenu_Unloaded(object sender, RoutedEventArgs e)
        {

            //var linkDoc = (DataContext as DocumentView).ViewModel.DataDocument.GetLinks(KeyStore.LinkToKey).FirstOrDefault() ?? (DataContext as DocumentView).ViewModel.DataDocument.GetLinks(KeyStore.LinkFromKey).FirstOrDefault();
            //LinkDoc.GetDataDocument().SetField<TextController>(KeyStore.DataKey, xDescriptionBox.Text, true);

        }

        //checks to see if a tag with the same name has already been created. if not, then a new tag is created
        public Tag AddTagIfUnique(string name)
        {
            if (TagNameDict.TryGetValue(name, out var tag))
            {
                return tag;
            }

            return AddTag(name);
        }

        //adds a new tag both graphically and to the dictionary
        public Tag AddTag(string linkName, List<DocumentController> links = null)
        {

            var r = new Random();
            var hexColor = Color.FromArgb(150, (byte)r.Next(256), (byte)r.Next(256), (byte)r.Next(256));

            Tag tag = null;

            //removes an old tag if one already exists and redoes it
            if (TagNameDict.ContainsKey(linkName))
            {
                tag = TagNameDict[linkName];
            }
            else
            {
                //otherwise a new tag is created and is added to the tag dictionary and the list of tags
                tag = new Tag(this, linkName, hexColor);

                //Tags.Add(tag);
                TagNameDict.Add(linkName, tag);

                //creates a new document controller out of the tag details to save into the database via tagssave
                var doc = new DocumentController();
                doc.SetField<TextController>(KeyStore.DataKey, linkName, true);
                doc.SetField<ColorController>(KeyStore.BackgroundColorKey, hexColor, true);
                TagsSave.Add(doc);

                //if there are currently less than 5 recent tags (aka less than 5 tags currently exist), add the new tag to the recent tags
                if (_recentTags.Count < 5)
                {
                    _recentTags.Enqueue(tag);
                    RecentTagsSave.Add(doc);
                }
                //otherwise, get rid of the oldest recent tag and add the new tag to recent tags, as well as update the recenttagssave
                else
                {
                    _recentTags.Dequeue();
                    RecentTagsSave.RemoveAt(0);
                    _recentTags.Enqueue(tag);
                    RecentTagsSave.Add(doc);
                }

                //replace the default recent tags to include the newest tag
                xTagContainer.Children.Clear();
                foreach (var recent in _recentTags.Reverse())
                {
                    xTagContainer.Children.Add(recent);
                }
            }
            return tag;
        }

        private void XAutoSuggestBox_OnKeyUp(object sender, KeyRoutedEventArgs e)
        {
            //if enter is pressed, the text in the search box will be made into a new tag 
            if (e.Key == VirtualKey.Enter)
            {
                var box = sender as AutoSuggestBox;
                string entry = box.Text.Trim();
                if (string.IsNullOrEmpty(entry)) return;

                var newtag = AddTagIfUnique(entry);
                //newtag.Select();

                box.Text = "";
            }
        }

        private void XAutoSuggestBox_OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            var results = new List<Tag>();
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                xTagContainer.Children.Clear();
                string search = sender.Text;

                //if nothing is changed, keep the results as the default recent tags
                if (search == "")
                {
                    foreach (var recent in _recentTags.Reverse())
                    {
                        if (!xTagContainer.Children.Contains(recent))
                        {
                            xTagContainer.Children.Add(recent);
                        }

                    }
                }
                else
                {
                    //first gather the tags that start with the search input, as they are more relevant than others
                    foreach (var name in TagNameDict.Keys)
                    {
                        if (name.StartsWith(search))
                        {
                            TagNameDict.TryGetValue(name, out var tag);
                            results.Add(tag);
                        }
                    }
                    var temp = new List<Tag>();
                    //then gather the tags that contain the search input anywhere, and add them to the results if they have not already been added
                    foreach (var name in TagNameDict.Keys)
                    {
                        if (name.Contains(search))
                        {
                            bool unique = true;
                            foreach (var result in results)
                            {
                                if (result.Text == name)
                                {
                                    unique = false;
                                }
                            }
                            if (unique)
                            {
                                TagNameDict.TryGetValue(name, out var tag);
                                results.Add(tag);
                            }
                        }
                    }
                    //sort and add them to the results
                    temp.Sort();
                    results.AddRange(temp);
                    //add all relevant results to be graphically displayed in the tag container
                    foreach (var result in results)
                    {
                        xTagContainer.Children.Add(result);
                    }
                }
            }
        }

        private void xLinkBehavior_OnChecked(object sender, RoutedEventArgs e)
        {
            if (sender == xTypeFollow)
            {
                LinkDoc.DataDocument.SetLinkBehavior(LinkBehavior.Follow);
            }

            if (sender == xTypeAnnotation)
            {
                LinkDoc.DataDocument.SetLinkBehavior(LinkBehavior.Annotate);
            }

            if (sender == xTypeDock)
            {
                LinkDoc.DataDocument.SetLinkBehavior(LinkBehavior.Dock);
            }

            if (sender == xTypeFloat)
            {
                LinkDoc.DataDocument.SetLinkBehavior(LinkBehavior.Float);
            }
        }

        //private void XInContext_OnToggled(object sender, RoutedEventArgs e)
        //{
        //    //var toggled = (sender as ToggleSwitch)?.IsOn;
        //    //var linkDoc = (DataContext as DocumentView).ViewModel.DataDocument.GetLinks(KeyStore.LinkToKey).FirstOrDefault() ??
        //    //              (DataContext as DocumentView).ViewModel.DataDocument.GetLinks(KeyStore.LinkFromKey).FirstOrDefault();
        //    //linkDoc.GetDataDocument().SetField<BoolController>(KeyStore.LinkContextKey, toggled, true);
        //}
    }
}
