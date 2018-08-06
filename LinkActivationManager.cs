using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Dash
{
	public class LinkActivationManager
	{
		public static ObservableCollection<DocumentView> ActivatedDocs = new ObservableCollection<DocumentView>();
		
		public LinkActivationManager()
		{
			
		}

		public void ToggleActivation(DocumentView view)
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

		public void ActivateDoc(DocumentView view)
		{
			if (IsActivated(view)) return;

			ActivatedDocs.Add(view);
			//add activation border 
			view.SetLinkBorderColor();
		}

		public void DeactivateDoc(DocumentView view)
		{
			if (!IsActivated(view)) return;

			ActivatedDocs.Remove(view);
			//remove activation border 
			view.RemoveLinkBorderColor();
		}

		public bool IsActivated(DocumentView view)
		{
			return ActivatedDocs.Contains(view);
		}
	}
}
