using Microsoft.Maui.Graphics.Platform;
using System.Reflection;
using IImage = Microsoft.Maui.Graphics.IImage;

namespace Beeswarm
{
    public class BeeswarmViewModel
    {
        public List<BeeswarmModel> BeeswarmData { get; set; }

        public BeeswarmViewModel()
        {
            BeeswarmData = new List<BeeswarmModel>(ReadCSV());
        }

        public IEnumerable<BeeswarmModel> ReadCSV()
        {
            Assembly executingAssembly = typeof(App).GetTypeInfo().Assembly;
            Stream inputStream = executingAssembly.GetManifestResourceStream("Beeswarm.Resources.Raw.independence_data.csv");

            if (inputStream == null)
            {
                throw new FileNotFoundException("CSV file not found.");
            }

            string line;
            List<string> lines = new();
            using StreamReader reader = new(inputStream);

            reader.ReadLine();

            while ((line = reader.ReadLine()) != null)
            {
                lines.Add(line);
            }

            var rawData = lines.Select(line =>
            {
                string[] data = line.Split(',');

                if (data[2].Trim().ToUpper() == "NA" ||
                    !int.TryParse(data[2].Trim(), out int year) ||
                    year < 1900)
                {
                    return null; 
                }

                return new BeeswarmModel(
                    data[0].Trim(), 
                    year, 
                    data[5].Trim() 
                );
            })
            .Where(model => model != null) 
            .ToList();

            var jitteredData = ApplyTightClustering(rawData);

            LoadCountryImages(jitteredData);

            return jitteredData;
        }

        private void LoadCountryImages(List<BeeswarmModel> data)
        {
            foreach (var country in data)
            {
                string resourcePath = country.CountryName.Replace(" ", "").ToLower();

                country.CountryFlag = GetImage(resourcePath);
            }
        }

        private static IImage? GetImage(string resourcePath)
        {
                Assembly assembly = typeof(MainPage).GetTypeInfo().Assembly;
                using Stream? stream = assembly.GetManifestResourceStream("Beeswarm.Resources.Images." + resourcePath + ".png");
                if (stream != null)
                {
                    return PlatformImage.FromStream(stream);
                }
            
            return null;
        }

        private List<BeeswarmModel> ApplyTightClustering(List<BeeswarmModel> data)
        {
            var result = new List<BeeswarmModel>();
            var occupiedPositions = new HashSet<(int year, double position)>();

            const double VERTICAL_STEP = 0.12; 
            const double CENTER_LINE = 1.0;   

            var yearGroups = data.GroupBy(x => x.IndependenceYear).OrderBy(g => g.Key);

            foreach (var yearGroup in yearGroups)
            {
                var countries = yearGroup.ToList();
                var positions = GenerateClusterPositions(countries.Count, CENTER_LINE, VERTICAL_STEP);

                for (int i = 0; i < countries.Count; i++)
                {
                    var country = countries[i];
                    var targetPosition = positions[i];

                    var finalPosition = FindAvailablePosition(
                        country.IndependenceYear,
                        targetPosition,
                        occupiedPositions,
                        VERTICAL_STEP
                    );

                    country.JitteredPosition = finalPosition;
                    occupiedPositions.Add((country.IndependenceYear, finalPosition));
                    result.Add(country);
                }
            }

            return result;
        }

        private List<double> GenerateClusterPositions(int count, double center, double step)
        {
            var positions = new List<double>();

            if (count == 1)
            {
                positions.Add(center);
                return positions;
            }

            var halfCount = count / 2;
            var hasMiddle = count % 2 == 1;

            if (hasMiddle)
            {
                positions.Add(center);
            }

            for (int i = 1; i <= halfCount; i++)
            {
                positions.Add(center + (i * step));  
                positions.Add(center - (i * step)); 
            }

            return positions.OrderBy(x => Guid.NewGuid()).ToList();
        }

        private double FindAvailablePosition(int year, double preferredPosition,
            HashSet<(int year, double position)> occupied, double step)
        {
            if (!IsPositionOccupied(year, preferredPosition, occupied))
            {
                return preferredPosition;
            }

            for (int offset = 1; offset <= 20; offset++)
            {
                var upperPos = preferredPosition + (offset * step);
                if (!IsPositionOccupied(year, upperPos, occupied))
                {
                    return upperPos;
                }

                var lowerPos = preferredPosition - (offset * step);
                if (!IsPositionOccupied(year, lowerPos, occupied))
                {
                    return lowerPos;
                }
            }

            return preferredPosition;
        }

        private bool IsPositionOccupied(int year, double position, HashSet<(int year, double position)> occupied)
        {
            const double COLLISION_THRESHOLD = 0.01; 
            for (int yearOffset = -1; yearOffset <= 1; yearOffset++)
            {
                int checkYear = year + yearOffset;

                foreach (var (occupiedYear, occupiedPos) in occupied)
                {
                    if (occupiedYear == checkYear && Math.Abs(position - occupiedPos) < COLLISION_THRESHOLD)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}