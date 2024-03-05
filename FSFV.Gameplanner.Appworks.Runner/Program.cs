// See https://aka.ms/new-console-template for more information
using FSFV.Gameplanner.Appworks;
using FSFV.Gameplanner.Appworks.Mappings.File;
using FSFV.Gameplanner.Appworks.Serialization;
using FSFV.Gameplanner.Service.Serialization;
using Microsoft.Extensions.Logging;

// Specify the file paths and tournament
string inputMappingsFilePath = "./mappings.csv";
string inputGameplanFilePath = "./matchplan (8).csv";
string outputFilePath = "./webimport.csv";

string tournament = "M";

var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

// Import the game plan
var logger = loggerFactory.CreateLogger<FsfvCustomSerializerService>();
var gamePlanParser = new FsfvCustomSerializerService(logger);
var gamePlan = await gamePlanParser.ParseGameplanAsync(() => Task.FromResult((Stream)new FileStream(inputGameplanFilePath, FileMode.Open)));

// Import the mappings and transform the imports
var importer = new AppworksMappingFileImporter(inputMappingsFilePath);
var transformer = new AppworksTransformer(loggerFactory.CreateLogger<AppworksTransformer>(), importer);
var transformedRecordsByTournament = await transformer.Transform(gamePlan);

// Create the serializer
var serializer = new AppworksSerializer();
// Write the transformed records to the output file
await serializer.WriteCsvImportFile(() => Task.FromResult((Stream)new FileStream(outputFilePath, FileMode.Create)), transformedRecordsByTournament[tournament]);
