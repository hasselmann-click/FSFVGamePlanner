
using FSFV.Gameplanner.Pdf;
using FSFV.Gameplanner.Service.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var inputFilePath = "./matchplan (3)_edited.csv";
var holidayFilePath = "./holidays.csv";
var outputFilePath = "./output.pdf";

var services = new ServiceCollection()
    .AddLogging(builder => builder.AddConsole())
    .AddTransient<PdfGenerator>()
    .AddTransient<FsfvCustomSerializerService>()
    .BuildServiceProvider();

var generator = services.GetRequiredService<PdfGenerator>();

var gamePlanStream = () => Task.FromResult<Stream>(File.OpenRead(inputFilePath));
var holidaysStream = () => Task.FromResult<Stream>(File.OpenRead(holidayFilePath));
var outputStream = () => Task.FromResult<Stream>(File.OpenWrite(outputFilePath));

await generator.GenerateAsync(outputStream, gamePlanStream, holidaysStream, showDocument: true);