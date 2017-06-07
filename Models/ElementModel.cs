using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Dash.ViewModels;
using Windows.UI.Text;

namespace Dash.Models
{
    public class ElementModel: INotifyPropertyChanged
    {
        public Visibility Visibility { get; set; }

        public FontWeight FontWeight { get; set;  }

        public TextWrapping TextWrapping { get; set; }

        public double Left
        {
            get { return _left; }
            set
            {
                _left = value;
                
                NotifyPropertyChanged("Left");
            }
        }
        

        public double Top {
            get { return _top; }
            set {
                _top = value;
                
                NotifyPropertyChanged("Top");
            }
        }

        public double Width { get; set; }
        public double Height { get; set; }

        private double _left;
        private double _top;
         
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        public ElementModel(double left, double top, FontWeight weight, TextWrapping wrap, Visibility visibility)
        {
            Left = left;
            Top = top;
            FontWeight = weight;
            TextWrapping = wrap;
            Visibility = visibility;
        }
    }
}
