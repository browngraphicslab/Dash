using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Dash.Annotations;

namespace Dash
{
    public class GraphConnection : INotifyPropertyChanged
    {
        private GraphNodeView _fromDoc;
        private GraphNodeView _toDoc;
        private Point _toPoint;
        private Point _fromPoint;

        public Point ToPoint
        {
            get => _toPoint;
            set => _toPoint = value;
        }

        public Point FromPoint
        {
            get => _fromPoint;
            set => _fromPoint = value;
        }

        public GraphNodeView FromDoc
        {
            get => _fromDoc;
            set
            {
                _fromDoc = value;
                OnPropertyChanged(nameof(FromDoc));
                FromDoc.ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
        }
        public GraphNodeView ToDoc
        {
            get => _toDoc;
            set
            {
                _toDoc = value;
                OnPropertyChanged(nameof(ToDoc));
                ToDoc.ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        public Polyline Connection { get; }

        public double Thickness
        {
            get => Connection.StrokeThickness;
            set => Connection.StrokeThickness = value;
        }

        public Brush Stroke
        {
            get => Connection.Stroke;
            set => Connection.Stroke = value;
        }

        public GraphConnection()
        {
            Connection = new Polyline
            {
                Stroke = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0)),
                StrokeThickness = 2
            };
            _fromPoint = new Point();
            _toPoint = new Point();
            PropertyChanged += GraphConnection_PropertyChanged;
        }

        private void GraphConnection_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ToDoc):
                case nameof(FromDoc):
                    UpdateConnection();
                    break;
            }
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(nameof(ToDoc.ViewModel.XPosition)) ||
                e.PropertyName.Equals(nameof(ToDoc.ViewModel.YPosition)))
            {
                UpdateConnection();
            }
        }

        private void UpdateConnection()
        {
            if (ToDoc != null && FromDoc != null)
            {
                Connection.Points.Clear();
                _toPoint.X = ToDoc.ViewModel.XPosition + ToDoc.xGrid.ActualWidth / 2;
                _toPoint.Y = ToDoc.ViewModel.YPosition + ToDoc.xGrid.ActualHeight / 2;
                _fromPoint.X = FromDoc.ViewModel.XPosition + FromDoc.xGrid.ActualWidth / 2;
                _fromPoint.Y = FromDoc.ViewModel.YPosition + FromDoc.xGrid.ActualHeight / 2;
                Connection.Points.Add(_toPoint);
                Connection.Points.Add(_fromPoint);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
