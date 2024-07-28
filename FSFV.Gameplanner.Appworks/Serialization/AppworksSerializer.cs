using CsvHelper;
using CsvHelper.TypeConversion;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace FSFV.Gameplanner.Appworks.Serialization;
public class AppworksSerializer(ILogger<AppworksSerializer> logger) : IAppworksSerializer
{

    public async Task WriteCsvImportFile(Func<Task<Stream>> writeStreamProvider, List<AppworksImportRecord> records)
    {
        // write records to csv using the stream provider
        await using var stream = await writeStreamProvider();
        using var writer = new StreamWriter(stream);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        if (stream is FileStream fs)
        {
            logger.LogInformation("Writing to file {FileName}", fs.Name);
        }

        var options = new TypeConverterOptions { Formats = [AppworksImportRecord.DateFormat] };
        csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);

        csv.WriteRecords(records);
    }

}
