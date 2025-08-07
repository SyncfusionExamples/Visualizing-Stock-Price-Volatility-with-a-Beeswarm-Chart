using Syncfusion.Maui.Toolkit.Charts;

namespace Beeswarm
{
    public partial class MainPage : ContentPage
    { 
        public MainPage()
        {
            InitializeComponent();
            BindingContext = new BeeswarmViewModel();
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
            // First draw the default scatter dot (this serves as background/fallback)
            base.Draw(canvas);

            // Check if we have a valid logo to draw
            if (Series is ScatterExt && Series.BindingContext is BeeswarmViewModel viewModel)
            {
                // Get the data for the current point
                var companyData = GetCompanyData(viewModel);

                if (companyData?.CompanyLogo != null)
                {
                    // Calculate image size and position
                    float imageSize = Math.Max(PointWidth, PointHeight) * 1.2f; // Slightly larger than the dot
                    float imageX = CenterX - (imageSize / 2);
                    float imageY = CenterY - (imageSize / 2);

                    // Draw the company logo
                    canvas.DrawImage(companyData.CompanyLogo, imageX, imageY, imageSize, imageSize);
                }
            }
        }

        private BeeswarmModel? GetCompanyData(BeeswarmViewModel viewModel)
        {
            // Determine which collection this point belongs to based on the ItemsSource
             if (Series.ItemsSource == viewModel.GoogleData && Index < viewModel.GoogleData.Count)
                return viewModel.GoogleData[Index];
            else if (Series.ItemsSource == viewModel.AmazonData && Index < viewModel.AmazonData.Count)
                return viewModel.AmazonData[Index]; 
            else if (Series.ItemsSource == viewModel.NetflixData && Index < viewModel.NetflixData.Count)
                return viewModel.NetflixData[Index];

            return null;
        }
    }
}
