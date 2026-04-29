namespace SDI.Back.Template.Models.Dto.Output;

public class HealthCheckResponse
{
    public string Status { get; set; } = string.Empty;
    public string TotalDuration { get; set; } = string.Empty;
    public Dictionary<string, HealthCheckEntryResponse> Entries { get; set; } = new();
}

public class HealthCheckEntryResponse
{
    public string Status { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public IReadOnlyDictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
}
