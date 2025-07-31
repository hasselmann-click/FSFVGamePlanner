// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using FSFV.Gameplanner.Appworks;
using FSFV.Gameplanner.Common.Rng;
using FSFV.Gameplanner.Fixtures;
using FSFV.Gameplanner.Pdf;
using FSFV.Gameplanner.Service.Migration;
using FSFV.Gameplanner.Service.Serialization;
using FSFV.Gameplanner.Service.Slotting.RuleBased.Extensions;
using FSFV.Gameplanner.UI.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.UI.Xaml;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Windows.ApplicationModel.Core;
using Windows.Graphics;
using Windows.UI.Core;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FSFV.Gameplanner.UI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {

        public new static App Current => (App)Application.Current;

        public ServiceProvider Services { get; }

        private Window m_window;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            Services = ConfigureServices();

            var logger = Services.GetRequiredService<ILogger<App>>();

            // Handle unhandled exceptions
            Application.Current.UnhandledException += (sender, e) =>
            {
                logger.LogError(e.Exception, "Unexpected exception occurred");
                var inner = e.Exception.InnerException;
                while (inner != null)
                {
                    logger.LogError("{innerMessage}", inner.Message);
                    inner = inner.InnerException;
                }

                e.Handled = true; // Set this to true to prevent the exception from crashing the app
            };

        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();
            WindowHelper.TrackWindow(m_window);
            m_window.Activate();
        }

        private static ServiceProvider ConfigureServices()
        {
            // TODO make this configurable in app. Especially for the ZK teams rule!
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
#if !DEBUG
                .AddJsonFile("appsettings.prod.json", optional: false, reloadOnChange: false)
#endif
                .Build();

            var services = new ServiceCollection();

            if (configuration.GetSection("PdfConfig").Get<PdfConfig>() is PdfConfig pdfConfig)
            {
                // explicitly convert the dictionary of strings to dictionary of colors
                // since IConfigurationSections doesn't use custom json converters
                var pdfConigLeagueColors = configuration.GetSection("PdfConfig:LeagueColors").Get<Dictionary<string, string>>();
                pdfConfig.LeagueColors = pdfConigLeagueColors?.ToDictionary(x => x.Key, x => Color.FromHex(x.Value)) ?? [];
                services
                    .AddSingleton(pdfConfig)
                    .AddTransient<PdfGenerator>();
            }
            else
            {
                throw new ArgumentException("Missing PdfConfig in configuration");
            }

            return services
                .AddSingleton<IRngProvider, RngProvider>()
                .AddSingleton<IConfiguration>(configuration)
                .AddLogging(config =>
                {
                    config.ClearProviders();
                    config.AddConfiguration(configuration.GetSection("Logging"));
                    config.AddConsole();
                    config.AddEventLog();
                    config.AddProvider(new UILoggerProvider());
                })
                .AddTransient<GeneratorService>()
                .AddScoped<CsvSerializerService>()
                .AddScoped<IMigrationService, MigrationService>()

                .AddRuleBasedSlotting()
                .AddAppworksServices()

                .BuildServiceProvider(new ServiceProviderOptions
                {
                    ValidateOnBuild = true
                });
        }

    }
}
