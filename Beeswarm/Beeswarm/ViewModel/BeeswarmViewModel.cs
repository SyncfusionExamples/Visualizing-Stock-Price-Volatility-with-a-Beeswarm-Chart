using Microsoft.Maui.Graphics.Platform;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;
using IImage = Microsoft.Maui.Graphics.IImage;

namespace Beeswarm
{
    public class BeeswarmViewModel
    {
        // Collections for all companies
        public ObservableCollection<BeeswarmModel> GoogleData { get; set; }
        public ObservableCollection<BeeswarmModel> AmazonData { get; set; }
        public ObservableCollection<BeeswarmModel> NetflixData { get; set; }

        // Chart properties
        public string ChartTitle { get; set; }
        public string YAxisTitle { get; set; }

        // Company colors
        public Color GoogleColor => Color.FromArgb("#4285F4");
        public Color AmazonColor => Color.FromArgb("#FF9900");
        public Color NetflixColor => Color.FromArgb("#E50914");

        // Min and max volatility for scaling (across all companies)
        public decimal MinVolatility { get; private set; }
        public decimal MaxVolatility { get; private set; }

        // Company X-axis ranges
        private const double GOOGLE_MIN_X = 0.0;
        private const double GOOGLE_MAX_X = 0.33;
        private const double AMAZON_MIN_X = 0.33;
        private const double AMAZON_MAX_X = 0.66;
        private const double NETFLIX_MIN_X = 0.66;
        private const double NETFLIX_MAX_X = 1.0;

        // Overlap detection constants
        private const double OVERLAP_DETECTION_RADIUS = 12;
        private const double MIN_MOVE_DISTANCE = 5;
        private const double CHART_WIDTH_PIXELS = 800;
        private const double CHART_HEIGHT_PIXELS = 400;

        public BeeswarmViewModel()
        {
            // Initialize collections
            GoogleData = new ObservableCollection<BeeswarmModel>();
            AmazonData = new ObservableCollection<BeeswarmModel>();
            NetflixData = new ObservableCollection<BeeswarmModel>();

            // Set chart properties
            ChartTitle = "Stock Volatility Distribution - Segmented View";
            YAxisTitle = "Daily Volatility ($)";

            // Load all company data
            LoadAllCompanyData();
        }

        private void LoadAllCompanyData()
        {
            // Read data for all companies
            var googleData = ReadCSV("Google.csv", "Google").ToList();
            var amazonData = ReadCSV("Amazon.csv", "Amazon").ToList();
            var netflixData = ReadCSV("Netflix.csv", "Netflix").ToList();

            // Filter data to specific time periods for each company
            var filteredGoogleData = FilterDataToSpecificPeriod(googleData, "Google");
            var filteredAmazonData = FilterDataToSpecificPeriod(amazonData, "Amazon");
            var filteredNetflixData = FilterDataToSpecificPeriod(netflixData, "Netflix");

            // Find min and max volatility across ALL companies
            var allData = new List<BeeswarmModel>();
            allData.AddRange(filteredGoogleData);
            allData.AddRange(filteredAmazonData);
            allData.AddRange(filteredNetflixData);

            if (allData.Any())
            {
                MinVolatility = allData.Min(s => s.DailyVolatility);
                MaxVolatility = allData.Max(s => s.DailyVolatility);
            }

            // Apply clustering logic to each company in their allocated ranges
            var clusteredGoogleData = ApplyCompanySpecificClustering(filteredGoogleData, "Google", GOOGLE_MIN_X, GOOGLE_MAX_X);
            var clusteredAmazonData = ApplyCompanySpecificClustering(filteredAmazonData, "Amazon", AMAZON_MIN_X, AMAZON_MAX_X);
            var clusteredNetflixData = ApplyCompanySpecificClustering(filteredNetflixData, "Netflix", NETFLIX_MIN_X, NETFLIX_MAX_X);

            // Load company logos
            LoadCompanyLogos(clusteredGoogleData, "google");
            LoadCompanyLogos(clusteredAmazonData, "amazon");
            LoadCompanyLogos(clusteredNetflixData, "netflix");

            // Add to collections
            foreach (var stock in clusteredGoogleData) GoogleData.Add(stock);
            foreach (var stock in clusteredAmazonData) AmazonData.Add(stock);
            foreach (var stock in clusteredNetflixData) NetflixData.Add(stock);

            Console.WriteLine($"Google data processed: {GoogleData.Count} points (X: {GOOGLE_MIN_X:F2} - {GOOGLE_MAX_X:F2})");
            Console.WriteLine($"Amazon data processed: {AmazonData.Count} points (X: {AMAZON_MIN_X:F2} - {AMAZON_MAX_X:F2})");
            Console.WriteLine($"Netflix data processed: {NetflixData.Count} points (X: {NETFLIX_MIN_X:F2} - {NETFLIX_MAX_X:F2})");
            Console.WriteLine($"Total volatility range: ${MinVolatility:F2} - ${MaxVolatility:F2}");
        }

        /// <summary>
        /// Apply clustering logic to each company within their specific X-axis range
        /// </summary>
        private List<BeeswarmModel> ApplyCompanySpecificClustering(List<BeeswarmModel> data, string companyName, double minX, double maxX)
        {
            if (!data.Any()) return data;

            // Sort by volatility for natural clustering
            var sortedData = data.OrderBy(d => d.DailyVolatility).ToList();
            var placedPoints = new List<(double pixelX, double pixelY, BeeswarmModel data)>();

            // Calculate company-specific center X
            double centerX = (minX + maxX) / 2.0;
            double centerPixelX = centerX * CHART_WIDTH_PIXELS;
            double minPixelX = minX * CHART_WIDTH_PIXELS;
            double maxPixelX = maxX * CHART_WIDTH_PIXELS;

            // Initial positioning within company range
            foreach (var stock in sortedData)
            {
                double normalizedY = CalculateNormalizedY(stock.DailyVolatility);
                double pixelY = normalizedY * CHART_HEIGHT_PIXELS;

                // Find available position within company's allocated range
                var finalPosition = FindPositionInRange(centerPixelX, pixelY, placedPoints, minPixelX, maxPixelX);
                stock.XPosition = finalPosition.pixelX / CHART_WIDTH_PIXELS;

                placedPoints.Add((finalPosition.pixelX, finalPosition.pixelY, stock));
            }

            Console.WriteLine($"{companyName} clustering completed: {sortedData.Count} points in range [{minX:F2}, {maxX:F2}]");
            return sortedData;
        }

        /// <summary>
        /// Find available position within specified X range
        /// </summary>
        private (double pixelX, double pixelY) FindPositionInRange(
            double preferredPixelX,
            double pixelY,
            List<(double pixelX, double pixelY, BeeswarmModel data)> placedPoints,
            double minPixelX,
            double maxPixelX)
        {
            // Check if preferred position is available
            if (!HasCollision(preferredPixelX, pixelY, placedPoints, OVERLAP_DETECTION_RADIUS))
            {
                return (preferredPixelX, pixelY);
            }

            // Search within the allocated range
            double searchStep = MIN_MOVE_DISTANCE * 0.5;
            double maxSearchRadius = (maxPixelX - minPixelX) * 0.4; // Stay within range

            for (double searchRadius = searchStep; searchRadius <= maxSearchRadius; searchRadius += searchStep)
            {
                // Try right side within range
                double rightX = preferredPixelX + searchRadius;
                if (rightX <= maxPixelX && !HasCollision(rightX, pixelY, placedPoints, OVERLAP_DETECTION_RADIUS))
                {
                    return (rightX, pixelY);
                }

                // Try left side within range
                double leftX = preferredPixelX - searchRadius;
                if (leftX >= minPixelX && !HasCollision(leftX, pixelY, placedPoints, OVERLAP_DETECTION_RADIUS))
                {
                    return (leftX, pixelY);
                }
            }

            // Clamp to range if no position found
            double clampedX = Math.Max(minPixelX, Math.Min(maxPixelX, preferredPixelX));
            return (clampedX, pixelY);
        }

        /// <summary>
        /// Check for collision with existing points
        /// </summary>
        private bool HasCollision(double pixelX, double pixelY, List<(double pixelX, double pixelY, BeeswarmModel data)> placedPoints, double radius)
        {
            foreach (var point in placedPoints)
            {
                double distance = CalculateDistance(pixelX, pixelY, point.pixelX, point.pixelY);
                if (distance < radius)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Calculate distance between two points
        /// </summary>
        private double CalculateDistance(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2));
        }

        /// <summary>
        /// Calculate normalized Y position for volatility value
        /// </summary>
        private double CalculateNormalizedY(decimal volatility)
        {
            if (MaxVolatility == MinVolatility) return 0.5;

            return (double)((volatility - MinVolatility) / (MaxVolatility - MinVolatility));
        }

        // Your existing helper methods remain the same...
        private void LoadCompanyLogos(List<BeeswarmModel> data, string companyLogoName)
        {
            foreach (var stock in data)
            {
                stock.CompanyLogo = GetCompanyLogo(companyLogoName);
            }
        }

        private static IImage? GetCompanyLogo(string resourcePath)
        {
            try
            {
                Assembly assembly = typeof(BeeswarmViewModel).GetTypeInfo().Assembly;
                string fullPath = $"Beeswarm.Resources.Images.{resourcePath}.png";

                using Stream? stream = assembly.GetManifestResourceStream(fullPath);
                if (stream != null)
                {
                    return PlatformImage.FromStream(stream);
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading logo {resourcePath}: {ex.Message}");
                return null;
            }
        }

        private List<BeeswarmModel> FilterDataToSpecificPeriod(List<BeeswarmModel> stockData, string companyName)
        {
            if (!stockData.Any()) return stockData;

            switch (companyName.ToLower())
            {
                case "google":
                    return stockData.Where(d => d.Date.Year == 2018 && d.Date.Month <= 4).ToList();

                case "amazon":
                    return stockData.Where(d => d.Date.Year == 2016 && d.Date.Month >= 2 && d.Date.Month <= 5).ToList();

                case "netflix":
                    return stockData.Where(d => d.Date.Year == 2020 && d.Date.Month >= 1 && d.Date.Month <= 4).ToList();

                default:
                    return stockData.Take(50).ToList();
            }
        }

        // Your existing CSV reading methods remain exactly the same...
        private static IEnumerable<BeeswarmModel> ReadCSV(string fileName, string companyName)
        {
            try
            {
                Assembly executingAssembly = typeof(App).GetTypeInfo().Assembly;
                string resourcePath = $"Beeswarm.Resources.Raw.{fileName}";

                using Stream? inputStream = executingAssembly.GetManifestResourceStream(resourcePath);
                if (inputStream == null)
                {
                    Console.WriteLine($"CSV file not found: {resourcePath}");
                    return GetSampleData(companyName);
                }

                var stockData = new List<BeeswarmModel>();
                using StreamReader reader = new(inputStream);

                string? headerLine = reader.ReadLine();
                if (headerLine == null)
                {
                    Console.WriteLine($"Empty CSV file: {fileName}");
                    return GetSampleData(companyName);
                }

                string? line;
                int lineNumber = 1;
                while ((line = reader.ReadLine()) != null)
                {
                    lineNumber++;
                    try
                    {
                        var stockDataPoint = ParseCSVLine(line, companyName);
                        if (stockDataPoint != null)
                        {
                            stockData.Add(stockDataPoint);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing line {lineNumber} in {fileName}: {ex.Message}");
                    }
                }

                Console.WriteLine($"Successfully loaded {stockData.Count} records from {fileName}");
                return stockData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading CSV file {fileName}: {ex.Message}");
                return GetSampleData(companyName);
            }
        }

        private static BeeswarmModel? ParseCSVLine(string line, string companyName)
        {
            if (string.IsNullOrWhiteSpace(line)) return null;

            string[] data = line.Split(',');
            if (data.Length < 7) return null;

            try
            {
                DateTime date;
                if (!DateTime.TryParse(data[0], out date))
                {
                    string[] dateFormats = { "yyyy-MM-dd", "MM/dd/yyyy", "dd/MM/yyyy", "yyyy/MM/dd" };
                    if (!DateTime.TryParseExact(data[0], dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                        return null;
                }

                decimal open = ParseDecimal(data[1]);
                decimal high = ParseDecimal(data[2]);
                decimal low = ParseDecimal(data[3]);
                decimal close = ParseDecimal(data[4]);
                decimal adjClose = ParseDecimal(data[5]);
                long volume = ParseLong(data[6]);

                return new BeeswarmModel(date, open, high, low, close, adjClose, volume, companyName);
            }
            catch
            {
                return null;
            }
        }

        private static decimal ParseDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;
            value = value.Trim();

            if (decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal result))
                return result;
            if (decimal.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out result))
                return result;

            return 0;
        }

        private static long ParseLong(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;
            value = value.Trim();

            if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long result))
                return result;

            return 0;
        }

        private static IEnumerable<BeeswarmModel> GetSampleData(string companyName)
        {
            var random = new Random(42);
            var sampleData = new List<BeeswarmModel>();

            // Different volatility ranges for each company
            var volatilityClusters = companyName.ToLower() switch
            {
                "google" => new[] { 15.2, 18.7, 22.1, 25.8, 29.3, 32.7, 36.1 },
                "amazon" => new[] { 8.5, 12.3, 15.7, 18.9, 22.1, 25.4, 28.8 },
                "netflix" => new[] { 12.1, 16.5, 20.8, 24.2, 27.9, 31.5, 35.2 },
                _ => new[] { 10.0, 15.0, 20.0, 25.0, 30.0, 35.0, 40.0 }
            };

            for (int cluster = 0; cluster < volatilityClusters.Length; cluster++)
            {
                double baseVolatility = volatilityClusters[cluster];
                int pointsInCluster = random.Next(6, 15);

                for (int i = 0; i < pointsInCluster; i++)
                {
                    var date = new DateTime(2016, 2, 1).AddDays(cluster * 10 + i);
                    var basePrice = 575m;
                    var volatility = (decimal)(baseVolatility + random.NextDouble() * 2 - 1);

                    sampleData.Add(new BeeswarmModel(
                        date,
                        basePrice - volatility,
                        basePrice + volatility,
                        basePrice - volatility * 1.2m,
                        basePrice,
                        basePrice,
                        random.Next(1000000, 5000000),
                        companyName));
                }
            }

            return sampleData;
        }

        public double GetYAxisMinimum()
        {
            double min = (double)MinVolatility;
            return min - (min * 0.1);
        }

        public double GetYAxisMaximum()
        {
            double max = (double)MaxVolatility;
            return max + (max * 0.1);
        }
    }
}