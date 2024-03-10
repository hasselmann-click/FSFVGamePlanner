// See https://aka.ms/new-console-template for more information
using FSFV.Gameplanner.Appworks;
using FSFV.Gameplanner.Appworks.Mappings;
using FSFV.Gameplanner.Appworks.Mappings.File;
using FSFV.Gameplanner.Appworks.Serialization;
using FSFV.Gameplanner.Service.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Specify the file paths and tournament
string inputMappingsFilePath = "./mappings.csv";
string inputGameplanFilePath = "./matchplan (8).csv";
string outputFilePath = "./webimport.csv";

string tournament = "M";

var serviceProvider = new ServiceCollection()
    .AddLogging(builder => builder.AddConsole())
    .AddTransient<IAppworksMappingImporter>(sp => new AppworksMappingFileImporter(sp.GetRequiredService<ILogger<AppworksMappingFileImporter>>(), inputMappingsFilePath))
    .AddTransient<AppworksTransformer>()
    .AddTransient<FsfvCustomSerializerService>()
    .AddTransient<AppworksSerializer>()
    .BuildServiceProvider();

// Import the game plan
var gamePlanParser = serviceProvider.GetRequiredService<FsfvCustomSerializerService>();
var gamePlan = await gamePlanParser.ParseGameplanAsync(() => Task.FromResult((Stream)new FileStream(inputGameplanFilePath, FileMode.Open)));

// Import the mappings and transform the imports
var transformer = serviceProvider.GetRequiredService<AppworksTransformer>();
var transformedRecordsByTournament = await transformer.Transform(gamePlan, tournament);

// Write the transformed records to the output file
var serializer = serviceProvider.GetRequiredService<AppworksSerializer>();
await serializer.WriteCsvImportFile(() => Task.FromResult((Stream)new FileStream(outputFilePath, FileMode.Create)), transformedRecordsByTournament[tournament]);
