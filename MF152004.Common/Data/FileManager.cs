﻿namespace MF152004.Common.Data;

public static class FileManager
{
    private static readonly string _companyFolder = "C:\\BlueApplications";
    private static readonly string _applicationFolder = "\\MF152004";
    private static readonly string _mainFolder = "\\Files";

    static FileManager() 
    {
        CreateDirs();
    }

    private static void CreateDirs()
    {
        if (!Directory.Exists(_companyFolder))
            Directory.CreateDirectory(_companyFolder);

        if (!Directory.Exists(_companyFolder + _applicationFolder))
            Directory.CreateDirectory(_companyFolder + _applicationFolder);

        if (!Directory.Exists(_companyFolder + _applicationFolder + _mainFolder))
            Directory.CreateDirectory(_companyFolder + _applicationFolder + _mainFolder);
    }

    public static async void SetZplFile(Stream file, int shipmentId)
    {
        var path = _companyFolder + _applicationFolder + _mainFolder;
        var filePath = Path.Combine(path, shipmentId.ToString() + ".zpl");

        await using var fileStream = File.Create(filePath);
        await file.CopyToAsync(fileStream);
    }

    public static bool ZplExists(int shipmentId)
    {
        var path = _companyFolder + _applicationFolder + _mainFolder;

        return Directory.GetFiles(path).Any(file => Path.GetFileNameWithoutExtension(file) == shipmentId.ToString());
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="shipmentId"></param>
    /// <returns>An empty array if something went wrong</returns>
    public static byte[] GetZplFile(int shipmentId) //TODO: Dateien nach einem best. t löschen
    {
        var path = _companyFolder + _applicationFolder + _mainFolder;
        var filePath = Path.Combine(path, $"{shipmentId}.zpl");

        return File.Exists(filePath) ? File.ReadAllBytes(filePath) : Array.Empty<byte>();
    }

    /// <summary>
    /// Zpl-files which are older than provided days will removed.
    /// </summary>
    /// <param name="daysOlderThan"></param>
    /// <param name="shipmentIds">Additional condition: only shipment IDs will be checked</param>
    /// <returns>A list of deleted files or the exception as first element in the collection</returns>
    public static List<string> RemoveZplFiles(int daysOlderThan, params string[] shipmentIds)
    {
        var path = _companyFolder + _applicationFolder + _mainFolder;
        List<string> deletedFileList = new();

        try
        {
            var files = Directory.EnumerateFiles(path)
                .Where(file => File.GetCreationTime(file) <= DateTime.Now.AddDays(-daysOlderThan));

            if (shipmentIds is {Length: > 0})
                files = files
                    .Where(file => shipmentIds != null && shipmentIds.Contains(Path.GetFileNameWithoutExtension(file)));

            foreach (var file in files)
            {
                if (File.Exists(file))
                {
                    deletedFileList.Add(Path.GetFileName(file));
                    File.Delete(file);
                }
            }
        }
        catch (Exception exception)
        {
            deletedFileList.Add(exception.ToString()); //change it later
        }

        return deletedFileList;
    }
}