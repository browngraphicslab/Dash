using Microsoft.Toolkit.Uwp.UI.Controls;
using Microsoft.Toolkit.Uwp.UI.Controls.TextToolbarButtons;
using Microsoft.Toolkit.Uwp.UI.Controls.TextToolbarButtons.Common;
using Microsoft.Toolkit.Uwp.UI.Controls.TextToolbarFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash.Views.Document_Menu.Toolbar
{
    //Toolbar TODO
	public class CustomButtonFormatter : Formatter
	{
		public CustomButtonFormatter(TextToolbar model)
			: base(model)
		{
			CommonButtons = new CommonButtons(model);
		}

	public override ButtonMap DefaultButtons
	{
		get
		{
			var bold = CommonButtons.Bold;
			bold.Activation = item => Selected.Text = "BOLD!!!";

			return new ButtonMap
			{
				bold
			};
		}
	}

		private CommonButtons CommonButtons { get; }
	}
}
