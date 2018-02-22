using Microsoft.ProjectOxford.Vision.Contract;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using static Dash.NoteDocuments;
using Point = Windows.Foundation.Point;


namespace Dash
{
    class GroupManager
    {

        static public void RemoveGroup(CollectionView parentCollection, DocumentController group)
        {
            var groupDataDocument = parentCollection.ParentDocument.ViewModel.DataDocument;
            var groupsList = groupDataDocument.GetDereferencedField<ListController<DocumentController>>(KeyStore.GroupingKey, null);
            if (groupsList != null)
            {
                groupsList.Remove(group);
                groupDataDocument.SetField(KeyStore.GroupingKey, new ListController<DocumentController>(groupsList.TypedData), true);
            }
        }
        static public List<DocumentViewModel> SplitupGroupings(DocumentView parentDocument, bool canSplitupDragGroup)
        {
            var parentCollection = parentDocument?.ParentCollection;
            if (parentCollection == null || parentDocument?.ViewModel?.DocumentController == null)
                return new List<DocumentViewModel>();
            var groupToSplit = parentCollection.GetDocumentGroup(parentDocument.ViewModel.DocumentController);
            if (groupToSplit != null && canSplitupDragGroup)
            {
                var docsToReassign = groupToSplit.GetDataDocument(null).GetDereferencedField<ListController<DocumentController>>(KeyStore.GroupingKey, null).TypedData.Select(d => parentCollection.GetDocumentViewModel(d)).ToList();

                var groupsList = parentCollection.ParentDocument.ViewModel.DataDocument.GetDereferencedField<ListController<DocumentController>>(KeyStore.GroupingKey, null);
                groupsList.Remove(groupToSplit);
                
                var groupings = new List<DocumentViewModel>();
                foreach (var dv in docsToReassign)
                {
                    if (dv != null)
                        groupings = SetupGroupings(dv, parentCollection, true);
                }

                foreach (var dv in docsToReassign.Where((d)=>d != null && !d.LayoutDocument.DocumentType.Equals(BackgroundBox.DocumentType)).ToList())
                {
                    if (parentCollection.GetDocumentGroup(dv.DocumentController) == null)
                        dv.BackgroundBrush = new SolidColorBrush(Colors.Transparent);
                }
                return groupings;
            }
            
            return SetupGroupings(parentDocument.ViewModel, parentDocument.ParentCollection, true);
        }

        /// Todo: Fix AddConnected to be able to join multiple groups instead of having to iteratively add one group at a time -- then this function can be replaced with SetupGroupings2
        static public List<DocumentViewModel> SetupGroupings(DocumentViewModel docViewModel, CollectionView parentCollection, bool forceWrite = false)
        {
            var groups = SetupGroupings2(docViewModel, parentCollection, forceWrite);
            while (true)
            {
                var groups2 = SetupGroupings2(docViewModel, parentCollection, forceWrite);
                if (groups2.Count == groups.Count)
                    return groups2;
                groups = groups2;
            }
            return groups;
        }
        static public List<DocumentViewModel> SetupGroupings2(DocumentViewModel docViewModel, CollectionView parentCollection, bool forceUpdate=false)
        {
            var groupedViews = new List<DocumentController>();
            if (parentCollection != null && docViewModel != null && parentCollection.ParentDocument != null && parentCollection?.CurrentView is CollectionFreeformView freeFormView)
                {
                DocumentController dragGroupDocument;
                groupedViews = GetGroupMembers(parentCollection, docViewModel, out dragGroupDocument);

                if (forceUpdate) // recompute groups if forceUpdate is set, otherwise use the groups they way they were
                {
                    var groupsList = GetGroupsList(parentCollection);
                    var groupDataDocument = parentCollection.ParentDocument.ViewModel.DataDocument;
                    var otherGroups = groupsList.Data.Where((gd) => !gd.Equals(dragGroupDocument)).Select((gd) => gd as DocumentController).ToList();
                    var groups = AddConnected(parentCollection, groupedViews, dragGroupDocument, otherGroups);
                    groupDataDocument.SetField(KeyStore.GroupingKey, new ListController<DocumentController>(groups ?? groupsList.TypedData), true);
                    
                    groupedViews = GetGroupMembers(parentCollection, docViewModel, out dragGroupDocument);
                }

            }
            return groupedViews.Select((gd) => parentCollection.GetDocumentViewModel(gd)).ToList();
        }

        static List<DocumentController> GetGroupMembers(CollectionView parentCollection, DocumentViewModel docViewModel, out DocumentController dragGroupDocument)
        {
            dragGroupDocument = parentCollection.GetDocumentGroup(docViewModel.DocumentController);
            var dragDocumentList = dragGroupDocument?.GetDataDocument(null).GetDereferencedField<ListController<DocumentController>>(KeyStore.GroupingKey, null)?.TypedData;
            if (dragDocumentList == null)
            {
                dragGroupDocument = docViewModel.DocumentController;
                dragDocumentList = new List<DocumentController>(new DocumentController[] { dragGroupDocument });
            }
            return dragDocumentList;
        }

        static ListController<DocumentController> GetGroupsList(CollectionView collectionView)
        {
            if (collectionView.ParentDocument == null)
                return new ListController<DocumentController>(new List<DocumentController>());
            var groupDataDoc = collectionView.ParentDocument.ViewModel.DataDocument;
            var groupsList = groupDataDoc.GetDereferencedField<ListController<DocumentController>>(KeyStore.GroupingKey, null);
            if (groupsList == null)
                return new ListController<DocumentController>(new List<DocumentController>());
            var addedItems = new List<DocumentController>();
            foreach (var d in collectionView.ViewModel.DocumentViewModels)
                if (collectionView.GetDocumentGroup(d.DocumentController) == null && !groupsList.Data.Contains(d.DocumentController))
                {
                    addedItems.Add(d.DocumentController);
                }

            var removedGroups = new List<DocumentController>();
            var docsInCollection = collectionView.ViewModel.DocumentViewModels.Select((dv) => dv.DocumentController);
            foreach (var g in groupsList.TypedData)
            {
                var groupDocs = g.GetDereferencedField<ListController<DocumentController>>(KeyStore.GroupingKey, null);
                if (!docsInCollection.Contains(g) && (groupDocs == null || groupDocs.TypedData.Where((gd) => docsInCollection.Contains(gd)).Count() != groupDocs.Count))
                {
                    removedGroups.Add(g);
                }
            }
            if (addedItems.Count > 0 || removedGroups.Count > 0)
            {
                var newGroupsList = new List<DocumentController>(groupsList.TypedData);
                newGroupsList.AddRange(addedItems);
                newGroupsList.RemoveAll((r) => removedGroups.Contains(r));
                groupsList = new ListController<DocumentController>(newGroupsList);
                groupDataDoc.SetField(KeyStore.GroupingKey, groupsList, true);
            }
            return groupsList;
        }
        
        static public List<DocumentController> GetGroupDocumentsList(CollectionView parentCollection, DocumentController doc, bool onlyGroups = false)
        {
            var groupList = parentCollection.ParentDocument.ViewModel.DataDocument.GetDereferencedField<ListController<DocumentController>>(KeyStore.GroupingKey, null);
            
            foreach (var g in groupList.TypedData)
                if (g.Equals(doc))
                {
                    if (parentCollection.GetDocumentViewModel(g) != null)
                    {
                        return new List<DocumentController>(new DocumentController[] { g });
                    }
                    else
                    {
                        var cfield = g.GetDataDocument(null).GetDereferencedField<ListController<DocumentController>>(KeyStore.GroupingKey, null);
                        if (cfield != null)
                        {
                            return cfield.Data.Select((cd) => cd as DocumentController).ToList();
                        }
                    }
                }
            return null;
        }

        static public List<DocumentController> AddConnected(CollectionView parentCollection, List<DocumentController> dragDocumentList, DocumentController dragGroupDocument, List<DocumentController> otherGroups)
        {
            foreach (var dragDocument in dragDocumentList)
            {
                var dragDocumentView = parentCollection.GetDocumentViewModel(dragDocument);
                if (dragDocumentView == null)
                    continue;
                var dragDocumentBounds = dragDocumentView.GroupingBounds;
                foreach (var otherGroup in otherGroups)
                {
                    var otherGroupMembers = GetGroupDocumentsList(parentCollection, otherGroup);
                    if (otherGroupMembers == null)
                        continue;
                    foreach (var otherGroupMember in otherGroupMembers)
                    {
                        var otherDocView = parentCollection.GetDocumentViewModel(otherGroupMember);
                        if (otherDocView == null)
                            continue;
                        var otherGroupMemberBounds = otherDocView.GroupingBounds;
                        otherGroupMemberBounds.Intersect(dragDocumentBounds);

                        if (otherGroupMemberBounds != Rect.Empty)
                        {
                            if (otherGroupMember.Equals(otherGroup)) 
                            {
                                var newList = otherGroups.ToList(); // create a copy of all the other groups
                                newList.Remove(otherGroup);  // remove the group (single document) that we are merging into
                                dragDocumentList.Add(otherGroup); // add the other group (single document) to the dragged document list
                                newList.Add(new GroupNote(dragDocumentList).DataDocument);  // create a new group from the new drag list
                                var r = new Random();
                                var solid = (parentCollection.GetDocumentViewModel(otherGroup)?.BackgroundBrush as SolidColorBrush)?.Color;
                                var dragSlid = (parentCollection.GetDocumentViewModel(dragDocumentList.First())?.BackgroundBrush as SolidColorBrush)?.Color;
                                var brush = solid != Colors.Transparent ? new SolidColorBrush((Windows.UI.Color)solid) :
                                    dragSlid != null && dragSlid != Colors.Transparent ? new SolidColorBrush((Windows.UI.Color)dragSlid) :
                                      new SolidColorBrush(Windows.UI.Color.FromArgb(0x33, (byte)r.Next(255), (byte)r.Next(255), (byte)r.Next(255)));
                                foreach (var dvm in dragDocumentList.Select((d) => parentCollection.GetDocumentViewModel(d)).Where((dvm) => dvm != null))
                                    dvm.BackgroundBrush = brush;
                                return newList;
                            }
                            else 
                            {   // add the dragged documents to the otherGroup they overlap with
                                var groupList = otherGroup.GetDataDocument(null).GetDereferencedField<ListController<DocumentController>>(KeyStore.GroupingKey, null);
                                groupList.AddRange(dragDocumentList);
                                foreach (var dvm in dragDocumentList.Select((d) => parentCollection.GetDocumentViewModel(d)).Where((dvm) => dvm != null))
                                    dvm.BackgroundBrush = parentCollection.GetDocumentViewModel(otherGroupMember).BackgroundBrush;
                                return otherGroups.ToList();  // return the list of other groups since the dragged group has been assimilated
                            }
                        }
                    }
                }

            }

            return null;
            var sameList = otherGroups.ToList();
            sameList.Add(dragGroupDocument);
            return sameList;
        }
    }
}
