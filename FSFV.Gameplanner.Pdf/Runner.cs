using FSFV.Gameplanner.Service.Serialization;

namespace FSFV.Gameplanner.Pdf;

public class PdfGenerator(FsfvCustomSerializerService serializer)
{
    public async Task GenerateAsync(Func<Task<Stream>> gameplanCsvStream)
    {
        var games = await serializer.ParseGameplanAsync(gameplanCsvStream);
    }
}
