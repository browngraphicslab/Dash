using Windows.UI.Xaml;

namespace Dash.Converters
{
    class IntToTextAlignmentConverter : SafeDataToXamlConverter<double, TextAlignment>
    {
        public override TextAlignment ConvertDataToXaml(double data, object parameter = null)
        {
            switch ((int) data)
            {
                case 0:
                    return TextAlignment.Center; // Center is 0!!
                case 1:
                    return TextAlignment.Left;
                case 2:
                    return TextAlignment.Right;
                default:
                    return TextAlignment.Left;
            }
        }

        public override double ConvertXamlToData(TextAlignment alignment, object parameter = null)
        {
            switch (alignment)
            {
                case TextAlignment.Center:// Center is 0!!
                    return 0;
                case TextAlignment.Left:
                    return 1;
                case TextAlignment.Right:
                    return 2;
                default:
                    return 0;
            }
        }
    }
}
