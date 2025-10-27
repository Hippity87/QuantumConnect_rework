using static QuantumConnect.BlazorApp.Pages.Weather;

namespace QuantumConnect.BlazorApp.Components.WeatherDashboard
{
    public class TemperatureChartConfig : IChartConfig
    {
        public string Title => "Lämpötila nyt";
        public string Unit => "°C";
        public string LineColor => "rgba(255,99,132,1)";
        public string BackgroundColor => "rgba(255,99,132,0.2)";
        public double YMin => 15;
        public double YMax => 40;
        public double YStep => 5;
        public int? YCount => 6;          // <-- Lisää tämä IChartConfigiin ja toteutuksiin!
        public bool YBeginAtZero => false;
        public double? YSuggestedMin => 15;
        public double? YSuggestedMax => 40;

        public Func<SensorData, double> Selector => data => data.Temperature;
        public Func<double, string> ValueFormat => v => $"{v:F1}";
        public string IconName => "Sun";
        public string IconClass => "text-danger";
    }




}
