using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Popups
{
    public sealed partial class PublishSelectDocumentsPopup : UserControl, DashPopup
    {
        public PublishSelectDocumentsPopup()
        {
            this.InitializeComponent();
        }

	    public void SetHorizontalOffset(double offset)
	    {
		    xPopup.HorizontalOffset = offset;
	    }

	    public void SetVerticalOffset(double offset)
	    {
			xPopup.VerticalOffset = offset;
	    }

	    public FrameworkElement Self()
	    {
		    return this;
	    }

	    public Task<List<DocumentController>> GetDocuments()
	    {
			// TODO
			// the user will see two lists, one to publish, and one available to publish. As they move one to the other, there should be a checkbox saying "add entire network" which will import its whole island
			// WARNING: be sure to distinguish between aliases! Use DocumentIDs to distinguish the difference.
		    throw new NotImplementedException(); 
	    }
    }
}
