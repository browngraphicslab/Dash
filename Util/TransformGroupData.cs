using Windows.Foundation;

namespace Dash
{
    public struct TransformGroupData
    {
        public Point Translate { get; private set; }
        public Point ScaleAmount { get; private set; }
        public Point ScaleCenter { get; private set; }


        public TransformGroupData(Point translate, Point scaleAmount)
        {
            Translate = translate;
            ScaleAmount = scaleAmount;
        }

        public TransformGroupData(Point translate, Point scaleAmount, Point scaleCenter)
        {
            Translate = translate;
            ScaleAmount = scaleAmount;
            ScaleCenter = scaleCenter;

        }
    }
}
