using BlueApps.MaterialFlow.Common.Models.Configurations;
using MF152004.Models.Configurations;
using MF152004.Models.Values.Types;
using MF152004.Webservice.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MF152004.Webservice.Services;

public class ConfigurationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ConfigurationService> _logger;

    public ConfigurationService(ApplicationDbContext context, ILogger<ConfigurationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SetConfiguration(params Configuration[] configurations)
    {
        if (configurations is null)
            return;

        ServiceConfiguration configuration = new();

        foreach (var config in configurations) //TODO: Updaten kommt auch in Frage!
        {
            if (config.Key == ConfigurationName.weight_tolerance.ToString())
            {
                if (double.TryParse(config.Value?.ToString(), out var weight_tolerance))
                {
                    configuration.WeightToleranceConfig.WeigthTolerance = weight_tolerance;
                }
            }

            else if (config.Key == ConfigurationName.label_printer_reference.ToString())
            {
                var printerReference = GetObject<LabelPrinter[]>(config.Value);
                    
                if (printerReference != null)
                    configuration.LablePrinterConfigs.AddRange(printerReference);
            }

            else if (config.Key == ConfigurationName.sealer_route_reference.ToString())
            {
                var sealerRoute = GetObject<SealerRoute[]>(config.Value);

                if (sealerRoute != null)
                    configuration.SealerRouteConfigs.AddRange(sealerRoute);
            }

            else if (config.Key == ConfigurationName.branding_pdf_reference.ToString())
            {
                var brandingReference = GetObject<BrandingPdf[]>(config.Value);

                if (brandingReference != null)
                    configuration.BrandingPdfConfigs.AddRange(brandingReference);
            }
        }

        await UpdateConfiguration(configuration);
    }

    private T? GetObject<T>(object? obj)
    {
        if (obj is null)
            return default;

        try
        {
            var tObj = JsonSerializer.Deserialize<T>(obj.ToString());

            return tObj;
        }
        catch (Exception exception)
        {
            //TODO: Logging
            return default;
        }
    }

    private async Task UpdateConfiguration(ServiceConfiguration configuration)
    {
        await UpdateWeightConfiguration(configuration.WeightToleranceConfig);
        await UpdateSealerRouteConfiguration(configuration.SealerRouteConfigs);
        await UpdateBrandingPdfConfiguration(configuration.BrandingPdfConfigs);
        await UpdateLabelPrinterConfigs(configuration.LablePrinterConfigs);

        await _context.SaveChangesAsync();
    }

    private async Task UpdateWeightConfiguration(WeightTolerance weightToleranceConfig)
    {
        var weightConfigs = await _context.WeightToleranceConfigs.ToListAsync();

        if (!weightConfigs.Any(x => x.WeigthTolerance.Equals(weightToleranceConfig.WeigthTolerance)))
        {
            weightConfigs.ForEach(_ => _.ConfigurationInUse = false);
            weightToleranceConfig.ConfigurationInUse = true;
            await _context.WeightToleranceConfigs.AddAsync(weightToleranceConfig);
        }
        else
        {
            if (!weightConfigs.First(_ => _.WeigthTolerance.Equals(weightToleranceConfig.WeigthTolerance)).ConfigurationInUse)
            {
                weightConfigs.ForEach(_ => _.ConfigurationInUse = false);
                weightConfigs.First(_ => _.WeigthTolerance.Equals(weightToleranceConfig.WeigthTolerance)).ConfigurationInUse = true;
                _context.UpdateRange(weightConfigs);
            }
        }
    }

    private async Task UpdateSealerRouteConfiguration(List<SealerRoute> sealerRouteConfigs) 
    {
        if (sealerRouteConfigs is null || sealerRouteConfigs.Count == 0)
            return;

        var existingSealerRouteConfigs = await _context.SealerRoutesConfigs.ToListAsync();

        foreach (var incomingSealerRoute in sealerRouteConfigs)
        {
            var existsSealerRouteConfig = existingSealerRouteConfigs
                .FirstOrDefault(_ => _.BoxBarcodeReference == incomingSealerRoute.BoxBarcodeReference);

            if (existsSealerRouteConfig is null)
            {
                await _context.SealerRoutesConfigs.AddAsync(incomingSealerRoute);
            }
            else
            {
                existsSealerRouteConfig.SealerRouteReference = incomingSealerRoute.SealerRouteReference;
                _context.SealerRoutesConfigs.Update(existsSealerRouteConfig);
            }
        }

        await _context.SaveChangesAsync();
        var newListOfExistingSealerRouteConfigs = await _context.SealerRoutesConfigs.ToListAsync();

        newListOfExistingSealerRouteConfigs.ForEach(_ => _.ConfigurationInUse = false);
        newListOfExistingSealerRouteConfigs
            .Where(x => sealerRouteConfigs.Any(y => y.BoxBarcodeReference == x.BoxBarcodeReference))
            .ToList()
            .ForEach(_ => _.ConfigurationInUse = true);

        _context.UpdateRange(newListOfExistingSealerRouteConfigs);
    }

    private async Task UpdateBrandingPdfConfiguration(List<BrandingPdf> brandingConfigs)
    {
        if (brandingConfigs is null || brandingConfigs.Count == 0)
            return;

        var existingBrandingConfigs = await _context.BradingPdfCongigs.ToListAsync();

        foreach (var incomingBrandingConfig in brandingConfigs)
        {
            var existsBrandingConfig = existingBrandingConfigs
                .FirstOrDefault(_ => _.BoxBarcodeReference == incomingBrandingConfig.BoxBarcodeReference && 
                                     _.ClientReference == incomingBrandingConfig.ClientReference);

            if (existsBrandingConfig is null)
            {
                await _context.BradingPdfCongigs.AddAsync(incomingBrandingConfig);
            }
            else
            {
                existsBrandingConfig.BrandingPdfReference = incomingBrandingConfig.BrandingPdfReference;
                _context.BradingPdfCongigs.Update(existsBrandingConfig);
            }
        }

        await _context.SaveChangesAsync();
        var newListOfExistingBrandingConfigs = await _context.BradingPdfCongigs.ToListAsync();

        newListOfExistingBrandingConfigs.ForEach(_ => _.ConfigurationInUse = false);
        newListOfExistingBrandingConfigs
            .Where(x => brandingConfigs.Any(y => y.BoxBarcodeReference == x.BoxBarcodeReference && y.ClientReference == x.ClientReference))
            .ToList()
            .ForEach(_ => _.ConfigurationInUse = true);

        _context.UpdateRange(newListOfExistingBrandingConfigs);
    }

    private async Task UpdateLabelPrinterConfigs(List<LabelPrinter> labelPrinterConfigs)
    {
        if (labelPrinterConfigs == null || labelPrinterConfigs.Count == 0)
            return;

        var existingLabelPrinterConfigs = await _context.LabelPrinterConfigs.ToListAsync();

        foreach (var incomingLabelPrinterConfig in labelPrinterConfigs)
        {
            var existsLabelPrinterConfig = existingLabelPrinterConfigs
                .FirstOrDefault(_ => _.BoxBarcodeReference == incomingLabelPrinterConfig.BoxBarcodeReference);

            if (existsLabelPrinterConfig is null)
            {
                await _context.LabelPrinterConfigs.AddAsync(incomingLabelPrinterConfig);
            }
            else
            {
                existsLabelPrinterConfig.LabelPrinterReference = incomingLabelPrinterConfig.LabelPrinterReference;
                _context.LabelPrinterConfigs.Update(existsLabelPrinterConfig);
            }
        }

        await _context.SaveChangesAsync();
        var newListOfExistingLabelPrinterConfigs = await _context.LabelPrinterConfigs.ToListAsync();

        newListOfExistingLabelPrinterConfigs.ForEach(_ => _.ConfigurationInUse = false);
        newListOfExistingLabelPrinterConfigs
            .Where(x => labelPrinterConfigs.Any(y => y.BoxBarcodeReference == x.BoxBarcodeReference))
            .ToList()
            .ForEach(_ => _.ConfigurationInUse = true);

        _context.UpdateRange(newListOfExistingLabelPrinterConfigs);
    }

    public async Task<ServiceConfiguration> GetActiveConfiguration()
    {
        ServiceConfiguration activeConfiguration = new()
        {
            WeightToleranceConfig = await _context.WeightToleranceConfigs.FirstOrDefaultAsync(_ => _.ConfigurationInUse),
            SealerRouteConfigs = await _context.SealerRoutesConfigs.Where(_ => _.ConfigurationInUse).ToListAsync(),
            BrandingPdfConfigs = await _context.BradingPdfCongigs.Where(_ => _.ConfigurationInUse).ToListAsync(),
            LablePrinterConfigs = await _context.LabelPrinterConfigs.Where(_ => _.ConfigurationInUse).ToListAsync()
        };

        return activeConfiguration;
    }
}