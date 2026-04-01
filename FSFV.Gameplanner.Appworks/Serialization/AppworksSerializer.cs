using CsvHelper;
using CsvHelper.TypeConversion;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace FSFV.Gameplanner.Appworks.Serialization;
public class AppworksSerializer(ILogger<AppworksSerializer> logger) : IAppworksSerializer
{

    public Task WriteCsvImportFile(Stream writeStream, List<AppworksImportRecord> records)
    {
        using var writer = new StreamWriter(writeStream, leaveOpen: true);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        if (writeStream is FileStream fs)
        {
            logger.LogInformation("Writing to file {FileName}", fs.Name);
        }

        var options = new TypeConverterOptions { Formats = [AppworksImportRecord.DateFormat] };
        csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);

        csv.WriteRecords(records);
        writer.Flush();
        return Task.CompletedTask;
    }

}
