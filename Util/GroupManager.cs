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
        static public List<DocumentViewModel> SplitupGroupings(DocumentView parentDocument, bool canSplitupDragGroup)
        {
            if (parentDocument?.ParentCollection == null || parentDocument?.ViewModel?.DocumentController == null)
                return new List<DocumentViewModel>();
            var groupToSplit = parentDocument.ParentCollection.GetDocumentGroup(parentDocument.ViewModel.DocumentController);
            if (groupToSplit != null && canSplitupDragGroup)
            {
                var docsToReassign = groupToSplit.GetDataDocument(null).GetDereferencedField<ListController<DocumentController>>(KeyStore.GroupingKey, null);

                var groupsList = parentDocument.ParentCollection.ParentDocument.ViewModel.DocumentController.GetDataDocument(null).GetDereferencedField<ListController<DocumentController>>(KeyStore.GroupingKey, null);
                groupsList.Remove(groupToSplit);

                var parentCollection = parentDocument.ParentCollection;
                List<DocumentViewModel> groupings = new List<DocumentViewModel>();
                foreach (var dv in docsToReassign.TypedData.Select(d => parentCollection.GetDocumentViewModel(d)).ToList())
                {
                    if (dv != null)
                        groupings = SetupGroupings(dv, parentDocument.ParentCollection);
                }

                foreach (var dv in docsToReassign.TypedData.Select((d) => parentCollection.GetDocumentViewModel(d)).ToList())
                {
                    if (dv != null && parentDocument.ParentCollection.GetDocumentGroup(dv.DocumentController) == null &&
                        !dv.DocumentController.DocumentType.Equals(BackgroundBox.DocumentType))
                        dv.BackgroundBrush = new SolidColorBrush(Colors.Transparent);
                }
                return groupings;
            }
            
            return SetupGroupings(parentDocument.ViewModel, parentDocument.ParentCollection, true);
        }


        static public List<DocumentViewModel> SetupGroupings(DocumentViewModel docViewModel, CollectionView parentCollection, bool forceWrite=false)
        {
            if (parentCollection == null || docViewModel == null || parentCollection.ParentDocument == null)
                return new List<DocumentViewModel>();
            var groupsList = GetGroupsList(parentCollection);

            DocumentController dragGroupDocument;
            var dragDocumentList = GetGroupMembers(parentCollection, docViewModel, out dragGroupDocument);

            if (parentCollection?.CurrentView is CollectionFreeformView freeFormView)
            {
                var groups = AddConnected(parentCollection, dragDocumentList, dragGroupDocument, groupsList.Data.Where((gd) => !gd.Equals(dragGroupDocument)).Select((gd) => gd as DocumentController));
                if (groups != null || forceWrite) {
                    parentCollection.ParentDocument.ViewModel.DocumentController.GetDataDocument(null).SetField(KeyStore.GroupingKey, new ListController<DocumentController>(groups ?? groupsList.TypedData), true);
                }

                DocumentController newDragGroupDocument;
                return GetGroupMembers(parentCollection, docViewModel, out newDragGroupDocument).Select((gd) => parentCollection.GetDocumentViewModel(gd)).ToList();
            }
            return new List<DocumentViewModel>();
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
            var groupsList = collectionView.ParentDocument.ViewModel.DocumentController.GetDataDocument(null).GetDereferencedField<ListController<DocumentController>>(KeyStore.GroupingKey, null);
            if ((groupsList == null || groupsList.Count == 0) && collectionView.ViewModel.DocumentViewModels.Count > 0)
            {
                groupsList = new ListController<DocumentController>(collectionView.ViewModel.DocumentViewModels.Select((vm) => vm.DocumentController));
                collectionView.ParentDocument.ViewModel.DocumentController.GetDataDocument(null).SetField(KeyStore.GroupingKey, groupsList, true);
            }
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
                collectionView.ParentDocument.ViewModel.DocumentController.GetDataDocument(null).SetField(KeyStore.GroupingKey, groupsList, true);
            }
            return groupsList;
        }
        
        static public List<DocumentController> GetGroupDocumentsList(CollectionView parentCollection, DocumentController doc, bool onlyGroups = false)
        {
            var groupList = parentCollection.ParentDocument.ViewModel.DocumentController.GetDataDocument(null).GetDereferencedField<ListController<DocumentController>>(KeyStore.GroupingKey, null);
            
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

        static public List<DocumentController> AddConnected(CollectionView parentCollection, List<DocumentController> dragDocumentList, DocumentController dragGroupDocument, IEnumerable<DocumentController> otherGroups)
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
