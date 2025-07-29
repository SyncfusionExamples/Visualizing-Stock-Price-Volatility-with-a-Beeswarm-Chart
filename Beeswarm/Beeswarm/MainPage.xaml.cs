using Syncfusion.Maui.Toolkit.Charts;

namespace Beeswarm
{
    public partial class MainPage : ContentPage
    { 
        public MainPage()
        {
            InitializeComponent();
        }  
    }
    public class ScatterExt : ScatterSeries
    {
        protected override ChartSegment CreateSegment()
        {
            return new ScatterSegmentExt();
        }
    }

    public class ScatterSegmentExt : ScatterSegment
    {
        protected override void Draw(ICanvas canvas)
        {
            base.Draw(canvas);
            if (Series is ScatterExt && Series.BindingContext is BeeswarmViewModel viewModel)
            {
                var countryData = viewModel.BeeswarmData[Index];

                if (countryData.CountryFlag != null)
                { 
                    float imageSize = Math.Max(PointWidth, PointHeight);
                    float imageX = CenterX - (imageSize / 2);
                    float imageY = CenterY - (imageSize / 2);

                    canvas.DrawImage(countryData.CountryFlag, imageX, imageY, imageSize, imageSize);
                     
                }
            } 
        }
    }
}
