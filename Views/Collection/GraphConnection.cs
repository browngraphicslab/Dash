using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
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
                if (_fromDoc != null)
                {
                    OnPropertyChanged(nameof(FromDoc));
                    FromDoc.PositionsLoaded += FromDoc_Loaded;
                    FromDoc.ViewModel.PropertyChanged += ViewModel_PropertyChanged;
                }
            }
        }

        public GraphNodeView ToDoc
        {
            get => _toDoc;
            set
            {
                _toDoc = value;
                if (_toDoc != null)
                {
                    OnPropertyChanged(nameof(ToDoc));
                    ToDoc.PositionsLoaded += ToDoc_Loaded;
                    ToDoc.ViewModel.PropertyChanged += ViewModel_PropertyChanged;
                }
            }
        }

        private void FromDoc_Loaded()
        {
            Connection.Points.Remove(_fromPoint);
            _fromPoint.X = FromDoc.Center.X;
            _fromPoint.Y = FromDoc.Center.Y;
            Connection.Points.Add(_fromPoint);
        }

        private void ToDoc_Loaded()
        {
            Connection.Points.Remove(_toPoint);
            _toPoint.X = ToDoc.Center.X;
            _toPoint.Y = ToDoc.Center.Y;
            Connection.Points.Add(_toPoint);
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
            UpdateConnection();
        }

        private void UpdateConnection()
        {
            if (ToDoc != null && FromDoc != null)
            {
                Connection.Points.Clear();
                _toPoint.X = ToDoc.Center.X;
                _toPoint.Y = ToDoc.Center.Y;
                _fromPoint.X = FromDoc.Center.X;
                _fromPoint.Y = FromDoc.Center.Y;
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
