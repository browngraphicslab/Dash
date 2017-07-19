using Dash.Models.OperatorModels.Set;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class OperatorsFilter : UserControl
    {
        ObservableCollection<OperatorFieldModel> Arithmetics = new ObservableCollection<OperatorFieldModel>() { new OperatorFieldModel("Divide") };
        ObservableCollection<OperatorFieldModel> Sets = new ObservableCollection<OperatorFieldModel>() { new OperatorFieldModel("Union"), new OperatorFieldModel("Intersection") };
        ObservableCollection<OperatorFieldModel> Maps = new ObservableCollection<OperatorFieldModel> () { new OperatorFieldModel("ImageToUri") };
        ObservableCollection<OperatorFieldModel> All = new ObservableCollection<OperatorFieldModel>() { new OperatorFieldModel("Divide"), new OperatorFieldModel("Union"), new OperatorFieldModel("Intersection"), new OperatorFieldModel("ImageToUri") };
        ObservableCollection<OperatorFieldModel> FilteredArithmetics { get; set; }
        ObservableCollection<OperatorFieldModel> FilteredSets { get; set; }
        ObservableCollection<OperatorFieldModel> FilteredMaps { get; set; }
        ObservableCollection<OperatorFieldModel> FilteredAll { get; set; }
        private static OperatorsFilter instance;
        public static OperatorsFilter Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new OperatorsFilter();
                }
                return instance;
            }
        }
        private OperatorsFilter()
        {
            this.InitializeComponent();
            FilteredAll = All;
            FilteredMaps = Maps;
            FilteredSets = Sets;
            FilteredArithmetics = Arithmetics;
            this.SetManipulation();
            xAllList.SelectionChanged += delegate { this.SelectionChanged(xAllList); };
            xArithmeticList.SelectionChanged += delegate { this.SelectionChanged(xArithmeticList); };
            xMapList.SelectionChanged += delegate { this.SelectionChanged(xMapList); };
            xSetList.SelectionChanged += delegate { this.SelectionChanged(xSetList); };
        }

        private void SetManipulation()
        {
            ManipulationMode = ManipulationModes.All;
            RenderTransform = new CompositeTransform();
            ManipulationDelta += delegate (object sender, ManipulationDeltaRoutedEventArgs e)
            {
                var transform = RenderTransform as CompositeTransform;
                if (transform != null)
                {
                    transform.TranslateX += e.Delta.Translation.X;
                    transform.TranslateY += e.Delta.Translation.Y;
                }
            };
        }
        private void SelectionChanged(ListView listView)
        {
            var model = listView.SelectedItem as OperatorFieldModel;
            var docController = this.CreateOperator(model);
            MainPage.Instance.DisplayDocument(docController);
        }

        private DocumentController CreateOperator(OperatorFieldModel model)
        {
            DocumentController opModel = null;
            if (model.Type == "Divide")
            {
                opModel =
                OperatorDocumentModel.CreateOperatorDocumentModel(
                    new DivideOperatorFieldModelController(model));
                var view = new DocumentView
                {
                    Width = 200,
                    Height = 200
                };
                var opvm = new DocumentViewModel(opModel);
                //OperatorDocumentViewModel opvm = new OperatorDocumentViewModel(opModel);
                view.DataContext = opvm;
            } else if (model.Type == "Union")
            {
                opModel =
                    OperatorDocumentModel.CreateOperatorDocumentModel(
                        new UnionOperatorFieldModelController(model));
                var unionView = new DocumentView
                {
                    Width = 200,
                    Height = 200
                };
                var unionOpvm = new DocumentViewModel(opModel);
                unionView.DataContext = unionOpvm;
            } else if (model.Type == "Intersection")
            {
                // add union operator for testing 
                opModel =
                    OperatorDocumentModel.CreateOperatorDocumentModel(
                        new IntersectionOperatorModelController(model));
                var intersectView = new DocumentView
                {
                    Width = 200,
                    Height = 200
                };
                var intersectOpvm = new DocumentViewModel(opModel);
                intersectView.DataContext = intersectOpvm;
            } else if (model.Type == "ImageToUri")
            {
                // add image url -> image operator for testing
                opModel =
                    OperatorDocumentModel.CreateOperatorDocumentModel(
                        new ImageOperatorFieldModelController(model));
                var imgOpView = new DocumentView
                {
                    Width = 200,
                    Height = 200
                };
                var imgOpvm = new DocumentViewModel(opModel);
                imgOpView.DataContext = imgOpvm;
            }
            return opModel;
        }
    }

}
