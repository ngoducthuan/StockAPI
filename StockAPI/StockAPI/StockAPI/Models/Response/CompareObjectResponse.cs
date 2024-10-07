using Newtonsoft.Json;

namespace StockAPI.Models.Response
{
    public class CompareObjectResponse
    {
        public DateTime Time { get; set; }


        public double Open { get; set; }

        public double Hight { get; set; }

        public double Low { get; set; }
        public double Close { get; set; }
        public int Volume { get; set; }
    }
}
