using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using DashShared;

namespace Dash
{
    // TODO: name this to something more descriptive
    public static class Util
    {
        


        /// <summary>
        /// Transforms point p to relative point in Window.Current.Content 
        /// </summary>
        /// <param name="p"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static Point DeltaTransformFromVisual(Point p, UIElement to)
        {
            //GeneralTransform r = this.TransformToVisual(Window.Current.Content).Inverse;
            //Rect rect = new Rect(new Point(0, 0), new Point(1, 1));
            //Rect newRect = r.TransformBounds(rect);
            //Point p = new Point(rect.Width * e.Delta.Translation.X, rect.Height * e.Delta.Translation.Y);

            MatrixTransform r = to.TransformToVisual(Window.Current.Content) as MatrixTransform;
            Debug.Assert(r != null);
            Matrix m = r.Matrix;
            return new MatrixTransform { Matrix = new Matrix(1 / m.M11, 0, 0, 1 / m.M22, 0, 0) }.TransformPoint(p);
        }

        public static Point PointTransformFromVisual(Point p, UIElement to)
        {
            GeneralTransform r = to.TransformToVisual(Window.Current.Content).Inverse;
            Debug.Assert(r != null);
            return r.TransformPoint(p);
        }

        /// <summary>
        /// Create TranslateTransform to translate "totranslate" by "delta" amount relative to canvas space  
        /// </summary>
        /// <returns></returns>
        public static TranslateTransform TranslateInCanvasSpace(Point delta, UIElement toTranslate, double elemScale = 1.0)
        {
            Point p = DeltaTransformFromVisual(delta, toTranslate);
            return new TranslateTransform
            {
                X = p.X * elemScale,
                Y = p.Y * elemScale
            };
        }

        public static HashSet<DocumentController> GetIntersection(DocumentCollectionFieldModelController setA, DocumentCollectionFieldModelController setB)
        {
            HashSet<DocumentController> result = new HashSet<DocumentController>();
            foreach (DocumentController contA in setA.GetDocuments())
            {
                foreach (DocumentController contB in setB.GetDocuments())
                {
                    if (result.Contains(contB)) continue;

                    var enumFieldsA = contA.EnumFields().ToList();
                    var enumFieldsB = contB.EnumFields().ToList();
                    if (enumFieldsA.Count != enumFieldsB.Count) continue;

                    bool equal = true;
                    foreach (KeyValuePair<Key, FieldModelController> pair in enumFieldsA)
                    {
                        if (enumFieldsB.Select(p => p.Key).Contains(pair.Key))
                        {
                            if (pair.Value is TextFieldModelController)
                            {
                                TextFieldModelController fmContA = pair.Value as TextFieldModelController;
                                TextFieldModelController fmContB = enumFieldsB.First(p => p.Key.Equals(pair.Key)).Value as TextFieldModelController;
                                if (!fmContA.Data.Equals(fmContB?.Data))
                                {
                                    equal = false;
                                    break;
                                }
                            }
                            else if (pair.Value is NumberFieldModelController)
                            {
                                NumberFieldModelController fmContA = pair.Value as NumberFieldModelController;
                                NumberFieldModelController fmContB = enumFieldsB.First(p => p.Key.Equals(pair.Key)).Value as NumberFieldModelController;
                                if (!fmContA.Data.Equals(fmContB?.Data))
                                {
                                    equal = false;
                                    break;
                                }
                            }
                            else if (pair.Value is ImageFieldModelController)
                            {
                                ImageFieldModelController fmContA = pair.Value as ImageFieldModelController;
                                ImageFieldModelController fmContB = enumFieldsB.First(p => p.Key.Equals(pair.Key)).Value as ImageFieldModelController;
                                if (!fmContA.Data.UriSource.AbsoluteUri.Equals(fmContB?.Data.UriSource.AbsoluteUri))
                                {
                                    equal = false;
                                    break;
                                }
                            }
                        }
                    }
                    if (equal) result.Add(contB);
                }
            }
            return result; 
        }
    }
}
