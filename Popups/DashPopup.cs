using Windows.UI.Xaml;

namespace Dash.Popups
{
	/// <summary>
	/// This interface is only necessary when displayed over the main page
	/// </summary>
	public interface DashPopup
	{
		// for CoreWindow updates when the window size changes
		void SetHorizontalOffset(double offset);
		void SetVerticalOffset(double offset);
		FrameworkElement Self();
	}
}