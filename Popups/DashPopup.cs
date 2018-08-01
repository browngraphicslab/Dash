namespace Dash.Popups
{
	public interface DashPopup
	{
		// for CoreWindow updates when the window size changes
		void SetHorizontalOffset(double offset);
		void SetVerticalOffset(double offset);
	}
}