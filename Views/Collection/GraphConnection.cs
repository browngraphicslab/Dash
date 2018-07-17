using System.ComponentModel;
using System.Runtime.CompilerServices;
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
        private Point _fromPoint;
        private GraphNodeView _toDoc;
        private Point _toPoint;

        public GraphConnection()
        {
            //sets visual styling and positioning of line
            Connection = new Polyline
            {
                Stroke = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0)),
                StrokeThickness = 2
            };
            _fromPoint = new Point();
            _toPoint = new Point();
            PropertyChanged += GraphConnection_PropertyChanged;
        }

        //sets the points to which the link is connected
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

        //specifies which document the link connects from
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
                    FromDoc.PropertyChanged += FromDoc_PropertyChanged;
                }
            }
        }

        //specifies which document the link connects to
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
                    ToDoc.PropertyChanged += ToDoc_PropertyChanged;
                }
            }
        }

        //the actual, graphical line that represents a link
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

        public event PropertyChangedEventHandler PropertyChanged;

        //recalculates positioning when fromdoc is changed
        private void FromDoc_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Connection.Points.Remove(_fromPoint);
            //for some reason must remove/add or else will not update
            _fromPoint.X = FromDoc.Center.X;
            _fromPoint.Y = FromDoc.Center.Y;
            Connection.Points.Add(_fromPoint);
        }

        //recalculates positioning when todoc is changed
        private void ToDoc_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Connection.Points.Remove(_toPoint);
            //for some reason must remove/add or else will not update
            _toPoint.X = ToDoc.Center.X;
            _toPoint.Y = ToDoc.Center.Y;
            Connection.Points.Add(_toPoint);
        }

        //calculates positioning when fromdoc is loaded 
        private void FromDoc_Loaded()
        {
            Connection.Points.Remove(_fromPoint);
            //for some reason must remove/add or else will not update
            _fromPoint.X = FromDoc.Center.X;
            _fromPoint.Y = FromDoc.Center.Y;
            Connection.Points.Add(_fromPoint);
        }

        //calculates positioning when todoc is loaded 
        private void ToDoc_Loaded()
        {
            Connection.Points.Remove(_toPoint);
            //for some reason must remove/add or else will not update
            _toPoint.X = ToDoc.Center.X;
            _toPoint.Y = ToDoc.Center.Y;
            Connection.Points.Add(_toPoint);
        }

        //when either of the nodes are changed
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

        //when the link itself is changed
        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateConnection();
        }

        //updates the positioning of the link when the nodes' positions are changed
        private void UpdateConnection()
        {
            if (ToDoc != null && FromDoc != null)
            {
                //clears the previous points, and recalculates
                Connection.Points.Clear();
                _toPoint.X = ToDoc.Center.X;
                _toPoint.Y = ToDoc.Center.Y;
                _fromPoint.X = FromDoc.Center.X;
                _fromPoint.Y = FromDoc.Center.Y;
                Connection.Points.Add(_toPoint);
                Connection.Points.Add(_fromPoint);
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}