using IImage = Microsoft.Maui.Graphics.IImage;

namespace Beeswarm
{
    public class BeeswarmModel
    {
        public string CountryName { get; set; }
        public int IndependenceYear { get; set; }
        public double JitteredPosition { get; set; }
        public IImage? CountryFlag { get; set; }
        public string IndependenceFrom { get; set; } 

        public BeeswarmModel(string countryName, int independenceYear, string independenceFrom)
        {
            CountryName = countryName;
            IndependenceYear = independenceYear;
            IndependenceFrom = independenceFrom;
            JitteredPosition = 1;  
            CountryFlag = null;  
        }
    }
}
