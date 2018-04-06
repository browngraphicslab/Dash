using System;
using System.Globalization;

namespace Dash
{
    public class DateTimeToStringConverter : SafeDataToXamlConverter<DateTime, string>
    {
        public int[] Dti;

        public override string ConvertDataToXaml(DateTime data, object parameter = null)
        {
            return data.ToString("G");
        }

        public override DateTime ConvertXamlToData(string xaml, object parameter = null)
        {
            var dateTime = new DateTime();
            DateTime.TryParse(xaml, out dateTime);
            return dateTime;
        }
        

        //public override DateTime ConvertXamlToData(string xaml, object parameter = null)
        //{
        //    string[] dateTimeSplit = xaml.Split(' ');
        //    string[] dateComponents = dateTimeSplit[0].Split('/');
        //    string[] timeComponents = dateTimeSplit[1].Split(':');
        //    if (ConvertSuccessful(dateComponents, timeComponents))
        //    {
        //        try
        //        {
        //            return new DateTime(Dti[0], Dti[1], Dti[2], Dti[3], Dti[4], Dti[5]);
        //        }
        //        catch (ArgumentOutOfRangeException e)
        //        {
        //            return new DateTime();
        //        }
        //    }
        //    return new DateTime();
        //}

        //public bool ConvertSuccessful(string[] dateComponents, string[] timeComponents)
        //{
        //    if (!int.TryParse(dateComponents[2], out int year))
        //    {
        //        return false;
        //    }
        //    if (!int.TryParse(dateComponents[0], out int month))
        //    {
        //        return false;
        //    }
        //    if (!int.TryParse(dateComponents[1], out int day))
        //    {
        //        return false;
        //    }
        //    if (!int.TryParse(timeComponents[0], out int hour))
        //    {
        //        return false;
        //    }
        //    if (!int.TryParse(timeComponents[1], out int minute))
        //    {
        //        return false;
        //    }
        //    if (!int.TryParse(timeComponents[2], out int second))
        //    {
        //        return false;
        //    }
        //    Dti = new [] {year, month, day, hour, minute, second};
        //    return true;
        //}
    }
}