using BlueApps.MaterialFlow.Common.Models;

namespace MF152004.Webservice.Data.PageData;

public class StatisticsDTO
{
    public List<NoRead>? DayNoReads { get; set; }

    public List<NoRead>? WeekNoReads { get; set; }

    public List<NoRead>? MonthNoReads { get; set; }

    public List<ScannerNoReadStatistic>? ScannerDayNoReadStatistics { get; set; }

    public List<ScannerNoReadStatistic>? ScannerWeekNoReadStatistics { get; set; }

    public List<ScannerNoReadStatistic>? ScannerMonthNoReadStatistics { get; set; }
        

    public struct ScannerNoReadStatistic
    {
        public string ScannerPosition { get; set; }

        public double Frequency { get; set; }
    }

    public List<DestinationData> DaysDestinationsData { get; set; } = new();

    public List<DestinationData> WeekDestinationsData { get; set; } = new();

    public List<DestinationData> MonthDestinationsData { get; set; } = new();

    public List<DestinationStatistic> DestinationDayStatistic { get; set; } = new();

    public List<DestinationStatistic> DestinationWeekStatistic { get; set; } = new();

    public List<DestinationStatistic> DestinationMonthStatistic { get; set; } = new();

    public struct DestinationStatistic
    {
        public string DestinationName { get; set; }

        public int Amount { get; set; }

        public double Frequency { get; set; }
    }
}