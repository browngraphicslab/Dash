using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Media;
using DashShared;

namespace Dash
{
    public static class TypeInfoBrush
    {
        public static SolidColorBrush None = new SolidColorBrush(Colors.Black);
        public static SolidColorBrush Number = new SolidColorBrush(Colors.DarkBlue);
        public static SolidColorBrush Text = new SolidColorBrush(Colors.DarkRed);
        public static SolidColorBrush Image = new SolidColorBrush(Colors.Indigo);
        public static SolidColorBrush Collection = new SolidColorBrush(Colors.DarkGoldenrod);
        public static SolidColorBrush Document = new SolidColorBrush(Colors.DimGray);
        public static SolidColorBrush Reference = new SolidColorBrush(Colors.DarkMagenta);
        public static SolidColorBrush Operator = new SolidColorBrush(Colors.DarkGreen);
        public static SolidColorBrush Point = new SolidColorBrush(Colors.DarkKhaki);
        public static SolidColorBrush List = new SolidColorBrush(Colors.DarkOliveGreen);
        public static SolidColorBrush Ink = new SolidColorBrush(Colors.LightSlateGray);
        public static SolidColorBrush RichText = new SolidColorBrush(Colors.LightSteelBlue);
        public static SolidColorBrush Any = new SolidColorBrush(Colors.WhiteSmoke);
    }

    public class TypeToFillConverter: SafeDataToXamlConverter<TypeInfo, SolidColorBrush>
    {
        public override SolidColorBrush ConvertDataToXaml(TypeInfo data, object parameter = null)
        {
            switch (data)
            {
                case TypeInfo.None:
                    return TypeInfoBrush.None;
                case TypeInfo.Number:
                    return TypeInfoBrush.Number;
                case TypeInfo.Text:
                    return TypeInfoBrush.Text;
                case TypeInfo.Image:
                    return TypeInfoBrush.Image;
                case TypeInfo.Collection:
                    return TypeInfoBrush.Collection;
                case TypeInfo.Document:
                    return TypeInfoBrush.Document;
                case TypeInfo.Reference:
                    return TypeInfoBrush.Reference;
                case TypeInfo.Operator:
                    return TypeInfoBrush.Operator;
                case TypeInfo.Point:
                    return TypeInfoBrush.Point;
                case TypeInfo.List:
                    return TypeInfoBrush.List;
                case TypeInfo.Ink:
                    return TypeInfoBrush.Ink;
                case TypeInfo.RichText:
                    return TypeInfoBrush.RichText;
                case TypeInfo.Any:
                    return TypeInfoBrush.Any;
                default:
                    throw new ArgumentOutOfRangeException(nameof(data), data, null);
            }
        }

        public override TypeInfo ConvertXamlToData(SolidColorBrush xaml, object parameter = null)
        {
            if (xaml == new SolidColorBrush(Colors.Black))
            {
                return TypeInfo.None;
            } else if (xaml == new SolidColorBrush(Colors.DarkBlue))
            {
                return TypeInfo.Number;
            } else if (xaml == new SolidColorBrush(Colors.DarkRed))
            {
                return TypeInfo.Text;
            } else if (xaml == new SolidColorBrush(Colors.Indigo))
            {
                return TypeInfo.Image;
            } else if (xaml == new SolidColorBrush(Colors.DarkGoldenrod))
            {
                return TypeInfo.Collection;
            } else if (xaml == new SolidColorBrush(Colors.DarkGray))
            {
                return TypeInfo.Document;
            } else if (xaml == new SolidColorBrush(Colors.DarkMagenta))
            {
                return TypeInfo.Reference;
            } else if (xaml == new SolidColorBrush(Colors.DarkGreen))
            {
                return TypeInfo.Operator;
            } else if (xaml == new SolidColorBrush(Colors.DarkKhaki))
            {
                return TypeInfo.Point;
            } else if (xaml == new SolidColorBrush(Colors.DarkOliveGreen))
            {
                return TypeInfo.List;
            } else if (xaml == new SolidColorBrush(Colors.LightSlateGray))
            {
                return TypeInfo.Ink;
            } else if (xaml == new SolidColorBrush(Colors.LightSteelBlue))
            {
                return TypeInfo.RichText;
            }
            else
            {
                return TypeInfo.Any;
            }
        }
    }
}
