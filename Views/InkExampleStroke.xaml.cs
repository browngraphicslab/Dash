using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class InkExampleStroke : UserControl
    {
        public InkExampleStroke()
        {
            this.InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            InkStrokeBuilder builder = new InkStrokeBuilder();
            var stroke = builder.CreateStrokeFromInkPoints(InkPoints, Matrix3x2.CreateTranslation((float)-30600, (float) -30650));
            InkCanvas.InkPresenter.StrokeContainer.AddStroke(stroke);
            GlobalInkSettings.OnAttributesUpdated += attributes => stroke.DrawingAttributes = GlobalInkSettings.Attributes;

        }

        private List<InkPoint> InkPoints = new List<InkPoint>
        {
            new InkPoint( new Point(30609.14,30662.89), (float) 0.01171875, 0, 0, (ulong)747494233305),
            new InkPoint( new Point(30608.98,30663.32), (float) 0.01953125, 0, 0, (ulong)747494233363),
            new InkPoint( new Point(30608.83,30663.58), (float) 0.0234375, 0, 0, (ulong)747494233369),
            new InkPoint( new Point(30608.67,30664), (float) 0.02441406, 0, 0, (ulong)747494241580),
            new InkPoint( new Point(30608.51,30664.16), (float) 0.02832031, 0, 0, (ulong)747494248467),
            new InkPoint( new Point(30608.51,30664.42), (float) 0.03808594, 0, 0, (ulong)747494256966),
            new InkPoint( new Point(30608.35,30664.68), (float) 0.05175781, 0, 0, (ulong)747494263140),
            new InkPoint( new Point(30608.2,30665), (float) 0.0625, 0, 0, (ulong)747494271816),
            new InkPoint( new Point(30608.04,30665.52), (float) 0.07226563, 0, 0, (ulong)747494278515),
            new InkPoint( new Point(30607.88,30665.94), (float) 0.07617188, 0, 0, (ulong)747494287069),
            new InkPoint( new Point(30607.72,30666.52), (float) 0.07910156, 0, 0, (ulong)747494293382),
            new InkPoint( new Point(30607.57,30666.94), (float) 0.07910156, 0, 0, (ulong)747494301847),
            new InkPoint( new Point(30607.41,30667.47), (float) 0.078125, 0, 0, (ulong)747494308418),
            new InkPoint( new Point(30607.41,30667.89), (float) 0.078125, 0, 0, (ulong)747494316864),
            new InkPoint( new Point(30607.41,30668.47), (float) 0.078125, 0, 0, (ulong)747494323367),
            new InkPoint( new Point(30607.25,30668.89), (float) 0.07324219, 0, 0, (ulong)747494331954),
            new InkPoint( new Point(30607.25,30669.73), (float) 0.06933594, 0, 0, (ulong)747494338306),
            new InkPoint( new Point(30607.09,30670.68), (float) 0.06542969, 0, 0, (ulong)747494346683),
            new InkPoint( new Point(30606.99,30671.52), (float) 0.06054688, 0, 0, (ulong)747494353376),
            new InkPoint( new Point(30606.67,30672.2), (float) 0.05664063, 0, 0, (ulong)747494361563),
            new InkPoint( new Point(30606.51,30672.78), (float) 0.05273438, 0, 0, (ulong)747494368387),
            new InkPoint( new Point(30606.36,30673.46), (float) 0.05371094, 0, 0, (ulong)747494377077),
            new InkPoint( new Point(30606.2,30674.15), (float) 0.05761719, 0, 0, (ulong)747494383480),
            new InkPoint( new Point(30606.04,30674.83), (float) 0.06054688, 0, 0, (ulong)747494391817),
            new InkPoint( new Point(30605.72,30675.57), (float) 0.06152344, 0, 0, (ulong)747494398264),
            new InkPoint( new Point(30605.41,30676.09), (float) 0.0625, 0, 0, (ulong)747494406945),
            new InkPoint( new Point(30605.41,30676.67), (float) 0.0625, 0, 0, (ulong)747494413327),
            new InkPoint( new Point(30605.41,30677.2), (float) 0.05761719, 0, 0, (ulong)747494422032),
            new InkPoint( new Point(30605.41,30677.88), (float) 0.05175781, 0, 0, (ulong)747494428436),
            new InkPoint( new Point(30605.41,30678.62), (float) 0.04589844, 0, 0, (ulong)747494436856),
            new InkPoint( new Point(30605.41,30679.4), (float) 0.04394531, 0, 0, (ulong)747494443563),
            new InkPoint( new Point(30605.57,30680.4), (float) 0.04101563, 0, 0, (ulong)747494451844),
            new InkPoint( new Point(30605.57,30681.24), (float) 0.0390625, 0, 0, (ulong)747494458380),
            new InkPoint( new Point(30605.72,30682.09), (float) 0.04101563, 0, 0, (ulong)747494466859),
            new InkPoint( new Point(30606.04,30682.87), (float) 0.04394531, 0, 0, (ulong)747494473599),
            new InkPoint( new Point(30606.2,30683.61), (float) 0.04785156, 0, 0, (ulong)747494482174),
            new InkPoint( new Point(30606.51,30684.29), (float) 0.05371094, 0, 0, (ulong)747494488690),
            new InkPoint( new Point(30606.67,30684.98), (float) 0.05664063, 0, 0, (ulong)747494496837),
            new InkPoint( new Point(30606.99,30685.55), (float) 0.05957031, 0, 0, (ulong)747494503487),
            new InkPoint( new Point(30607.25,30686.24), (float) 0.05957031, 0, 0, (ulong)747494512124),
            new InkPoint( new Point(30607.57,30686.76), (float) 0.05664063, 0, 0, (ulong)747494518415),
            new InkPoint( new Point(30607.88,30687.34), (float) 0.05273438, 0, 0, (ulong)747494527007),
            new InkPoint( new Point(30608.35,30687.92), (float) 0.05175781, 0, 0, (ulong)747494533723),
            new InkPoint( new Point(30608.83,30688.45), (float) 0.05078125, 0, 0, (ulong)747494541592),
            new InkPoint( new Point(30609.14,30688.87), (float) 0.05078125, 0, 0, (ulong)747494548634),
            new InkPoint( new Point(30609.62,30689.45), (float) 0.05078125, 0, 0, (ulong)747494556530),
            new InkPoint( new Point(30610.09,30689.87), (float) 0.05078125, 0, 0, (ulong)747494563401),
            new InkPoint( new Point(30611.19,30690.39), (float) 0.0546875, 0, 0, (ulong)747494572099),
            new InkPoint( new Point(30612.14,30690.97), (float) 0.06054688, 0, 0, (ulong)747494578578),
            new InkPoint( new Point(30613.03,30691.5), (float) 0.06640625, 0, 0, (ulong)747494587102),
            new InkPoint( new Point(30614.14,30692.18), (float) 0.07128906, 0, 0, (ulong)747494593717),
            new InkPoint( new Point(30615.08,30692.5), (float) 0.07714844, 0, 0, (ulong)747494602191),
            new InkPoint( new Point(30616.03,30692.92), (float) 0.08398438, 0, 0, (ulong)747494608697),
            new InkPoint( new Point(30616.98,30693.34), (float) 0.09179688, 0, 0, (ulong)747494616945),
            new InkPoint( new Point(30617.92,30693.6), (float) 0.09960938, 0, 0, (ulong)747494623551),
            new InkPoint( new Point(30618.82,30693.86), (float) 0.1054688, 0, 0, (ulong)747494631974),
            new InkPoint( new Point(30619.61,30694.02), (float) 0.1103516, 0, 0, (ulong)747494638796),
            new InkPoint( new Point(30620.55,30694.13), (float) 0.1123047, 0, 0, (ulong)747494646928),
            new InkPoint( new Point(30621.34,30694.02), (float) 0.1152344, 0, 0, (ulong)747494653715),
            new InkPoint( new Point(30622.13,30694.13), (float) 0.1162109, 0, 0, (ulong)747494662302),
            new InkPoint( new Point(30623.23,30693.86), (float) 0.1162109, 0, 0, (ulong)747494668704),
            new InkPoint( new Point(30624.29,30693.6), (float) 0.1171875, 0, 0, (ulong)747494677247),
            new InkPoint( new Point(30625.39,30693.34), (float) 0.1191406, 0, 0, (ulong)747494683945),
            new InkPoint( new Point(30626.49,30693.02), (float) 0.1201172, 0, 0, (ulong)747494692173),
            new InkPoint( new Point(30627.6,30692.92), (float) 0.1230469, 0, 0, (ulong)747494698746),
            new InkPoint( new Point(30628.7,30692.6), (float) 0.1298828, 0, 0, (ulong)747494707268),
            new InkPoint( new Point(30629.75,30692.34), (float) 0.140625, 0, 0, (ulong)747494713645),
            new InkPoint( new Point(30631.17,30692.07), (float) 0.1494141, 0, 0, (ulong)747494722151),
            new InkPoint( new Point(30632.43,30691.81), (float) 0.1601563, 0, 0, (ulong)747494728914),
            new InkPoint( new Point(30633.7,30691.5), (float) 0.1669922, 0, 0, (ulong)747494737402),
            new InkPoint( new Point(30634.75,30691.23), (float) 0.171875, 0, 0, (ulong)747494743736),
            new InkPoint( new Point(30635.69,30690.81), (float) 0.1748047, 0, 0, (ulong)747494752104),
            new InkPoint( new Point(30636.48,30690.39), (float) 0.1748047, 0, 0, (ulong)747494758632),
            new InkPoint( new Point(30637.43,30690.13), (float) 0.1835938, 0, 0, (ulong)747494767388),
            new InkPoint( new Point(30638.38,30689.87), (float) 0.1962891, 0, 0, (ulong)747494773665),
            new InkPoint( new Point(30639.16,30689.45), (float) 0.2070313, 0, 0, (ulong)747494782384),
            new InkPoint( new Point(30639.95,30689.03), (float) 0.2167969, 0, 0, (ulong)747494788716),
            new InkPoint( new Point(30640.69,30688.61), (float) 0.2226563, 0, 0, (ulong)747494797283),
            new InkPoint( new Point(30641.48,30688.03), (float) 0.2294922, 0, 0, (ulong)747494803752),
            new InkPoint( new Point(30642.27,30687.5), (float) 0.2324219, 0, 0, (ulong)747494812382),
            new InkPoint( new Point(30642.9,30686.92), (float) 0.2353516, 0, 0, (ulong)747494812444),
            new InkPoint( new Point(30643.53,30686.4), (float) 0.2392578, 0, 0, (ulong)747494818946),
            new InkPoint( new Point(30644.16,30685.82), (float) 0.2431641, 0, 0, (ulong)747494827320),
            new InkPoint( new Point(30644.63,30685.13), (float) 0.2470703, 0, 0, (ulong)747494833770),
            new InkPoint( new Point(30645.11,30684.29), (float) 0.2529297, 0, 0, (ulong)747494842293),
            new InkPoint( new Point(30645.68,30683.29), (float) 0.2597656, 0, 0, (ulong)747494848765),
            new InkPoint( new Point(30646.16,30682.35), (float) 0.2607422, 0, 0, (ulong)747494857474),
            new InkPoint( new Point(30646.79,30681.35), (float) 0.2597656, 0, 0, (ulong)747494863743),
            new InkPoint( new Point(30647.26,30680.4), (float) 0.2558594, 0, 0, (ulong)747494872324),
            new InkPoint( new Point(30647.73,30679.4), (float) 0.2548828, 0, 0, (ulong)747494879052),
            new InkPoint( new Point(30648.05,30678.62), (float) 0.2539063, 0, 0, (ulong)747494887407),
            new InkPoint( new Point(30648.37,30677.77), (float) 0.2558594, 0, 0, (ulong)747494894051),
            new InkPoint( new Point(30648.68,30677.09), (float) 0.2607422, 0, 0, (ulong)747494902332),
            new InkPoint( new Point(30648.84,30676.51), (float) 0.2695313, 0, 0, (ulong)747494908775),
            new InkPoint( new Point(30648.84,30675.41), (float) 0.2910156, 0, 0, (ulong)747494924047),
            new InkPoint( new Point(30648.68,30674.41), (float) 0.3037109, 0, 0, (ulong)747494932348),
            new InkPoint( new Point(30648.52,30673.62), (float) 0.3115234, 0, 0, (ulong)747494939090),
            new InkPoint( new Point(30648.37,30672.88), (float) 0.3173828, 0, 0, (ulong)747494947379),
            new InkPoint( new Point(30648.05,30672.2), (float) 0.3232422, 0, 0, (ulong)747494953943),
            new InkPoint( new Point(30647.73,30671.36), (float) 0.3242188, 0, 0, (ulong)747494962473),
            new InkPoint( new Point(30647.42,30670.68), (float) 0.3242188, 0, 0, (ulong)747494969097),
            new InkPoint( new Point(30647.1,30669.83), (float) 0.3242188, 0, 0, (ulong)747494977555),
            new InkPoint( new Point(30646.79,30669.15), (float) 0.3261719, 0, 0, (ulong)747494984097),
            new InkPoint( new Point(30646.47,30668.47), (float) 0.3271484, 0, 0, (ulong)747494992505),
            new InkPoint( new Point(30646,30667.79), (float) 0.3300781, 0, 0, (ulong)747494999126),
            new InkPoint( new Point(30645.53,30667.21), (float) 0.3369141, 0, 0, (ulong)747495007470),
            new InkPoint( new Point(30645.11,30666.63), (float) 0.3496094, 0, 0, (ulong)747495013923),
            new InkPoint( new Point(30644.63,30666.1), (float) 0.359375, 0, 0, (ulong)747495022381),
            new InkPoint( new Point(30644.16,30665.52), (float) 0.3691406, 0, 0, (ulong)747495028984),
            new InkPoint( new Point(30643.69,30665), (float) 0.375, 0, 0, (ulong)747495037491),
            new InkPoint( new Point(30643.21,30664.42), (float) 0.3798828, 0, 0, (ulong)747495043932),
            new InkPoint( new Point(30642.74,30663.89), (float) 0.3789063, 0, 0, (ulong)747495052605),
            new InkPoint( new Point(30642.27,30663.47), (float) 0.3789063, 0, 0, (ulong)747495059042),
            new InkPoint( new Point(30641.63,30662.79), (float) 0.3789063, 0, 0, (ulong)747495067633),
            new InkPoint( new Point(30641,30661.95), (float) 0.3808594, 0, 0, (ulong)747495074136),
            new InkPoint( new Point(30640.37,30661.21), (float) 0.3818359, 0, 0, (ulong)747495082544),
            new InkPoint( new Point(30639.79,30660.69), (float) 0.3847656, 0, 0, (ulong)747495089349),
            new InkPoint( new Point(30639.16,30660.11), (float) 0.3857422, 0, 0, (ulong)747495097384),
            new InkPoint( new Point(30638.53,30659.58), (float) 0.3857422, 0, 0, (ulong)747495104094),
            new InkPoint( new Point(30637.74,30658.9), (float) 0.3837891, 0, 0, (ulong)747495112681),
            new InkPoint( new Point(30636.8,30658.16), (float) 0.3828125, 0, 0, (ulong)747495119247),
            new InkPoint( new Point(30635.85,30657.48), (float) 0.3847656, 0, 0, (ulong)747495127533),
            new InkPoint( new Point(30635.06,30656.95), (float) 0.3876953, 0, 0, (ulong)747495134329),
            new InkPoint( new Point(30634.33,30656.38), (float) 0.3867188, 0, 0, (ulong)747495142669),
            new InkPoint( new Point(30633.54,30655.8), (float) 0.3867188, 0, 0, (ulong)747495149077),
            new InkPoint( new Point(30632.75,30655.27), (float) 0.3847656, 0, 0, (ulong)747495157713),
            new InkPoint( new Point(30631.8,30654.85), (float) 0.3828125, 0, 0, (ulong)747495164118),
            new InkPoint( new Point(30630.54,30654.17), (float) 0.3808594, 0, 0, (ulong)747495172716),
            new InkPoint( new Point(30629.12,30653.59), (float) 0.3789063, 0, 0, (ulong)747495179469),
            new InkPoint( new Point(30627.91,30653.17), (float) 0.3720703, 0, 0, (ulong)747495187645),
            new InkPoint( new Point(30626.65,30652.91), (float) 0.3652344, 0, 0, (ulong)747495194135),
            new InkPoint( new Point(30625.39,30652.64), (float) 0.3486328, 0, 0, (ulong)747495202581),
            new InkPoint( new Point(30624.13,30652.48), (float) 0.3300781, 0, 0, (ulong)747495209108),
            new InkPoint( new Point(30622.76,30652.64), (float) 0.2373047, 0, 0, (ulong)747495224084),
            new InkPoint( new Point(30620.71,30652.91), (float) 0.109375, 0, 0, (ulong)747495232605),
            new InkPoint( new Point(30618.19,30653.33), (float) 0.04394531, 0, 0, (ulong)747495239292),
        };
    }
}
