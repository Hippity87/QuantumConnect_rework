using static QuantumConnect.BlazorApp.Pages.Weather;

namespace QuantumConnect.BlazorApp.Components.WeatherDashboard
{
    public interface IChartConfig
    {
        string Title { get; }
        string Unit { get; }
        string LineColor { get; }
        string BackgroundColor { get; }
        double YMin { get; }
        double YMax { get; }
        double YStep { get; }
        int? YCount { get; }                 // Uusi: haluttu tikki-määrä (optional)
        bool YBeginAtZero { get; }           // Uusi: aloita nollasta (optional)
        double? YSuggestedMin { get; }       // Uusi: suositeltu minimi (optional)
        double? YSuggestedMax { get; }       // Uusi: suositeltu maksimi (optional)
        Func<SensorData, double> Selector { get; }
        Func<double, string> ValueFormat { get; }
        string IconName { get; }
        string IconClass { get; }
    }
}

