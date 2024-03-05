using CsvHelper;
using CsvHelper.TypeConversion;
using System.Globalization;

namespace FSFV.Gameplanner.Appworks.Serialization;
public class AppworksSerializer : IAppworksSerializer
{

    public async Task WriteCsvImportFile(Func<Task<Stream>> writeStreamProvider, List<AppworksImportRecord> records)
    {
        // write records to csv using the stream provider
        await using var stream = await writeStreamProvider();
        using var writer = new StreamWriter(stream);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        var options = new TypeConverterOptions { Formats = [AppworksImportRecord.DateFormat] };
        csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);

        csv.WriteRecords(records);
    }

}
