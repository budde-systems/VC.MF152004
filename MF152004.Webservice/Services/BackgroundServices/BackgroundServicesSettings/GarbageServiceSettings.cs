namespace MF152004.Webservice.Services.BackgroundServices.BackgroundServicesSettings;

public class GarbageServiceSettings
{
    public int Period { get; set; }
    public TimeOnly ExecuteTime { get; set; }
    public int KeepOldZplFilesDays { get; set; }
    public int KeepDeliveredZplFileDays { get; set; }        
    public int KeepShipmentDays { get; set; }
}