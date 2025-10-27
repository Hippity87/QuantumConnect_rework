using static QuantumConnect.BlazorApp.Pages.Weather;

namespace QuantumConnect.BlazorApp.Components.WeatherDashboard
{
    public class HumidityChartConfig : IChartConfig
    {
        public string Title => "Kosteus nyt";
        public string Unit => "%";
        public string LineColor => "rgba(60,180,255,1)";
        public string BackgroundColor => "rgba(60,180,255,0.2)";
        public double YMin => 0;
        public double YMax => 100;
        public double YStep => 10;
        public int? YCount => 11;                // 0, 10, 20, ..., 100 = 11 tikkia
        public bool YBeginAtZero => true;
        public double? YSuggestedMin => 0;
        public double? YSuggestedMax => 100;
        public Func<SensorData, double> Selector => data => data.Humidity;
        public Func<double, string> ValueFormat => v => $"{v:F0}";
        public string IconName => "Tint";
        public string IconClass => "text-primary";
    }
}

