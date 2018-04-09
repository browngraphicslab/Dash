namespace Dash
{
    public class DoubleToStringConverter : SafeDataToXamlConverter<double, string>
    {
        private readonly double _minValue;
        private readonly double _maxValue;

        public DoubleToStringConverter(double minValue = double.NaN, double maxValue = double.NaN)
        {
            _minValue = minValue;
            _maxValue = maxValue;
        }

        public override string ConvertDataToXaml(double data, object parameter = null)
        {
            return Clamp(data).ToString();
        }

        public override double ConvertXamlToData(string xaml, object parameter = null)
        {
            if (!double.TryParse(xaml, out double outputValue))
            {
                outputValue = 0;
            }
            return Clamp(outputValue);
        }


        private double Clamp(double n)
        {
            if (!double.IsNaN(_minValue) && n < _minValue)
                return _minValue;
            if (!double.IsNaN(_maxValue) && n > _maxValue)
                return _maxValue;
            return n;
        }
    }
}