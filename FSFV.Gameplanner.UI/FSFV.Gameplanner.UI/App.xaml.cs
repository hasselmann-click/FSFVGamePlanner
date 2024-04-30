// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using FSFV.Gameplanner.Appworks;
using FSFV.Gameplanner.Fixtures;
using FSFV.Gameplanner.Pdf;
using FSFV.Gameplanner.Service.Serialization;
using FSFV.Gameplanner.Service.Slotting.RuleBased.Extensions;
using FSFV.Gameplanner.UI.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using System;
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

        private static readonly Random RNG = new(23432546);

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
                logger.LogError(e.Exception, "Unhandled exception occurred");
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
            var configuration = new ConfigurationBuilder()
                // TODO make this configurable in app. Especially for the ZK teams rule!
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            return new ServiceCollection()
                .AddSingleton(RNG)
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
                .AddTransient<FsfvCustomSerializerService>()
                .AddTransient<PdfGenerator>()

                .AddRuleBasedSlotting()
                .AddAppworksServices()

                .BuildServiceProvider(new ServiceProviderOptions
                {
                    ValidateOnBuild = true
                });
        }

    }
}
