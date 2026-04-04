namespace Refline.Models;
using System.IO;

public class AppSettings
{
    public int Id { get; set; } = 1;
    public int Version { get; set; } = 1;

    public string ReportsPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ReflineReports");
    public bool AutoStartWindows { get; set; }
    public bool AllowBackgroundTracking { get; set; } = true;
    public bool EnableLocalLog { get; set; } = true;
}
