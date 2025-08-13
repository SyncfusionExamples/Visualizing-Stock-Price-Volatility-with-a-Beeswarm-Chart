using IImage = Microsoft.Maui.Graphics.IImage;

namespace Beeswarm
{
    public class BeeswarmModel
    {
        public DateTime Date { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal AdjClose { get; set; }
        public long Volume { get; set; }
        public string Company { get; set; }
        public double XPosition { get; set; }
        public IImage? CompanyLogo { get; set; }
        public decimal DailyVolatility => High - Low;
        public string FormattedVolatility => $"${DailyVolatility:F2}";

        // Constructor for creating stock data
        public BeeswarmModel(
            DateTime date,
            decimal open,
            decimal high,
            decimal low,
            decimal close,
            decimal adjClose,
            long volume,
            string company)
        {
            Date = date;
            Open = open;
            High = high;
            Low = low;
            Close = close;
            AdjClose = adjClose;
            Volume = volume;
            Company = company;

            // Initialize visualization properties with defaults
            XPosition = 0.5; // Default to center
            CompanyLogo = null;
        }

        // Default constructor
        public BeeswarmModel()
        {
            Company = string.Empty;
            XPosition = 0.5;
            CompanyLogo = null;
        }
    }
}
