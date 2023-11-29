using BlueApps.MaterialFlow.Common.Models;
using MF152004.Webservice.Data;
using MF152004.Webservice.Data.PageData;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace MF152004.Webservice.Pages.Statistics
{
    public class IndexModel : PageModel
    {
        public StatisticsDTO? Statistics { get; set; }

        private readonly ApplicationDbContext _context;
        private readonly ILogger<IndexModel> _logger;
        private readonly int _currentWeek;

        public IndexModel(ApplicationDbContext context, ILogger<IndexModel> logger)
        {
            _context = context;
            _logger = logger;

            _currentWeek = GetIso8601WeekOfYear(DateTime.Now);

            InitStatistics();
        }

        private async void InitStatistics()
        {
            var monthNoReads = await _context.NoReads
                .Where(_ => _.AtTime.Month == DateTime.Now.Month && _.AtTime.Year == DateTime.Now.Year)
                .ToListAsync();

            Statistics = new StatisticsDTO()
            {
                DayNoReads = monthNoReads
                    .Where(_ => _.AtTime.Date == DateTime.Now.Date)
                    .ToList(),

                WeekNoReads = monthNoReads
                    .Where(_ => GetIso8601WeekOfYear(_.AtTime) == _currentWeek && _.AtTime.Year == DateTime.Now.Year)
                    .ToList(),

                MonthNoReads = monthNoReads.ToList()
            };

            InitStatisticsAnalysis();
            InitDestinationsStatistic();
            InitDestinationsStatisticsAnalysis();
        }

        private void InitStatisticsAnalysis()
        {
            if (Statistics == null)
                return;

            Statistics.ScannerDayNoReadStatistics = GetAnalysis(Statistics.DayNoReads)
                .OrderBy(_ => _.ScannerPosition)
                .ToList();

            Statistics.ScannerWeekNoReadStatistics = GetAnalysis(Statistics.WeekNoReads)
                .OrderBy(_ => _.ScannerPosition)
                .ToList();

            Statistics.ScannerMonthNoReadStatistics = GetAnalysis(Statistics.MonthNoReads)
                .OrderBy(_ => _.ScannerPosition).ToList();
        }

        private List<StatisticsDTO.ScannerNoReadStatistic> GetAnalysis(List<NoRead>? noReads)
        {
            List<StatisticsDTO.ScannerNoReadStatistic> statistics = new();

            if (noReads != null)
            {
                foreach (var noRead in noReads.GroupBy(n => n.Position).ToList())
                {
                    var groupPositions = noReads.Where(n => n.Position == noRead.Key).ToList();
                    double groupPositionsCount = groupPositions.Count;
                    double noReadsCount = noReads.Count;

                    statistics.Add(new StatisticsDTO.ScannerNoReadStatistic
                    {
                        ScannerPosition = noRead.Key,
                        Frequency = (groupPositionsCount * 100) / noReadsCount
                    });
                } 
            }

            return statistics;
        }

        private async void InitDestinationsStatistic()
        {
            if (Statistics is null)
                return;

            var monthDestinations = await _context.Shipments
                .Where(s => s.DestinationReachedAt != null && s.DestinationReachedAt.Value.Month == DateTime.Now.Month && s.DestinationRouteReference != null && !s.DestinationRouteReference.Contains(";"))
                .Select(s => new DestinationData { AtTime = s.DestinationReachedAt.Value, DestinationName = s.DestinationRouteReference })
                .OrderByDescending(d => d.AtTime)
                .ToListAsync();

            Statistics.DaysDestinationsData = monthDestinations
                .Where(d => d.AtTime.Date == DateTime.Now.Date)
                .ToList();

            Statistics.WeekDestinationsData = monthDestinations
                .Where(d => GetIso8601WeekOfYear(d.AtTime) == _currentWeek && d.AtTime.Year == DateTime.Now.Year)
                .ToList();

            Statistics.MonthDestinationsData = monthDestinations;
        }

        private void InitDestinationsStatisticsAnalysis()
        {
            if (Statistics is null)
                return;

            Statistics.DestinationDayStatistic = GetDestinationAnalysis(Statistics.DaysDestinationsData)
                .OrderByDescending(_ => _.Amount)
                .ToList();

            Statistics.DestinationWeekStatistic = GetDestinationAnalysis(Statistics.WeekDestinationsData)
                .OrderByDescending(_ => _.Amount)
                .ToList();

            Statistics.DestinationMonthStatistic = GetDestinationAnalysis(Statistics.MonthDestinationsData)
                .OrderByDescending(_ => _.Amount)
                .ToList();
        }

        private List<StatisticsDTO.DestinationStatistic> GetDestinationAnalysis(List<DestinationData> destinationDatas)
        {
            List<StatisticsDTO.DestinationStatistic> statistics = new();

            if (destinationDatas != null)
            {
                foreach(var destinationData in destinationDatas.GroupBy(_ => _.DestinationName).ToList())
                {
                    var groupDestinations = destinationDatas.Where(_ => _.DestinationName == destinationData.Key).ToList();
                    double groupDestinationsCount = groupDestinations.Count();
                    double destinationsCount = destinationDatas.Count();

                    statistics.Add(new()
                    {
                        DestinationName = destinationData.Key,
                        Amount = groupDestinations.Count,
                        Frequency = (groupDestinationsCount * 100) / destinationsCount
                    });
                }
            }

            return statistics;
        }

        public void OnGet()
        {
        }

        public int GetIso8601WeekOfYear(DateTime time)
        {
            DayOfWeek day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(time);
            if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
            {
                time = time.AddDays(3);
            }

            return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(time, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }
    }
}
