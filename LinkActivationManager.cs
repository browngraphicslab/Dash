using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dash
{
	public static class LinkActivationManager
	{
		public static ObservableCollection<DocumentView> ActivatedDocs = new ObservableCollection<DocumentView>();
		
		public static void ToggleActivation(DocumentView view)
		{
			if (IsActivated(view))
			{
				DeactivateDoc(view);
			}
			else
			{
				ActivateDoc(view);
			}
			
		}

		public static void ActivateDoc(DocumentView view)
		{
			ActivateDocHelper(view, true);
		}

		private static void ActivateDocHelper(DocumentView view, bool shouldUndo)
		{
			if (IsActivated(view)) return;

			ActivatedDocs.Add(view);
            view.SetActivationMode(true);
			//add activation border 
			///view.SetLinkBorderColor();

			if (shouldUndo) UndoManager.EventOccured(new UndoCommand(() => ActivateDocHelper(view, false), () => DeactivateDocHelper(view, false)));
		}

		public static void DeactivateDoc(DocumentView view)
		{
			DeactivateDocHelper(view, true);
		}

		public static void DeactivateDocHelper(DocumentView view, bool shouldUndo)
		{
			if (!IsActivated(view)) return;

			ActivatedDocs.Remove(view);
			//remove activation border 
			///view.RemoveLinkBorderColor();
            view.SetActivationMode(false);
            if (shouldUndo) UndoManager.EventOccured(new UndoCommand(() => DeactivateDocHelper(view, false), () => ActivateDocHelper(view, false)));
		}

		public static bool IsActivated(DocumentView view)
		{
			return ActivatedDocs.Contains(view);
		}

		public static void DeactivateAll()
		{
			DocumentView[] copyArray = new DocumentView[ActivatedDocs.Count];
			ActivatedDocs.CopyTo(copyArray, 0);

			foreach (DocumentView copy in copyArray)
			{
				DeactivateDoc(copy);
	
			}
		}

		public static void DeactivateAllExcept(List<DocumentView> list)
		{
			DocumentView[] copyArray = new DocumentView[ActivatedDocs.Count];
			ActivatedDocs.CopyTo(copyArray, 0);

			foreach (DocumentView copy in copyArray)
			{
				if (!list.Contains(copy)) DeactivateDoc(copy);
			}
		}
	}
}
