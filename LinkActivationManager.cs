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
			if (IsActivated(view)) return;

			ActivatedDocs.Add(view);
			//add activation border 
			view.SetLinkBorderColor();
		}

		public static void DeactivateDoc(DocumentView view)
		{
			if (!IsActivated(view)) return;

			ActivatedDocs.Remove(view);
			//remove activation border 
			view.RemoveLinkBorderColor();
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
