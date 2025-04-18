using FSFV.Gameplanner.Pdf;
using FSFV.Gameplanner.Service.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

var inputFilePath = "./matchplan (3)_edited.csv";
var holidayFilePath = "./holidays.csv";
var outputFilePath = "./output.pdf";

var configFilePath = "./pdfconfig.json";
var configFileStream = File.OpenRead(configFilePath);
var pdfConfig = await JsonSerializer.DeserializeAsync<PdfConfig>(configFileStream);
if (pdfConfig is null)
{
    throw new InvalidOperationException("Could not deserialize the configuration file.");
}

var services = new ServiceCollection()
    .AddLogging(builder => builder.AddConsole())
    .AddTransient<PdfGenerator>()
    .AddTransient<CsvSerializerService>()
    .AddSingleton(pdfConfig)
    .BuildServiceProvider();

var generator = services.GetRequiredService<PdfGenerator>();

var gamePlanStream = () => Task.FromResult<Stream>(File.OpenRead(inputFilePath));
var holidaysStream = () => Task.FromResult<Stream>(File.OpenRead(holidayFilePath));
var outputStream = () => Task.FromResult<Stream>(File.OpenWrite(outputFilePath));

await generator.GenerateAsync(outputStream, gamePlanStream, holidaysStream, showDocument: true);