using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
	interface IAnnotationEnabled
	{
		/// <summary> Region is selected </summary>
		void RegionSelected(object region, Windows.Foundation.Point pt, DocumentController chosenDoc = null);
	}
}
