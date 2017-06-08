using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Dash
{
    public abstract class TemplateModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public double Left
        {
            get { return _left; }
            set
            {
                _left = value;

                NotifyPropertyChanged("Left");
            }
        }

        public double Top
        {
            get { return _top; }
            set
            {
                _top = value;

                NotifyPropertyChanged("Top");
            }
        }

        public Visibility Visibility { get; set; }

        public double Width { get; set; }
        public double Height { get; set; }

        private double _left;
        private double _top;

        public TemplateModel(double left = 0, double top = 0, double width = 0, double height = 0, Visibility visibility = Visibility.Visible)
        {
            Left = left;
            Top = top;
            Width = width;
            Height = height;
            Visibility = visibility;
        }

        public abstract FieldViewModel CreateViewModel(FieldModel field);
    }
}
