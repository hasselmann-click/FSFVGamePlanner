// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using FSFV.Gameplanner.Fixtures;
using FSFV.Gameplanner.Service.Serialization;
using FSFV.Gameplanner.Service.Slotting.RuleBased.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using System;
using Windows.Graphics;

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

        public IServiceProvider Services { get; }

        private static readonly Random RNG = new(23432546);
        private static readonly SizeInt32 LaunchWindowSize = new SizeInt32(1000, 1400);

        private Window m_window;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            Services = ConfigureServices();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();

            WindowHelper.TrackWindow(m_window);

            var appWindow = WindowHelper.GetAppWindow(m_window);
            appWindow.Resize(LaunchWindowSize);

            m_window.Activate();
        }

        private static IServiceProvider ConfigureServices()
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
                })
                .AddRuleBasedSlotting()
                .AddTransient<GeneratorService>()
                .AddTransient<FsfvCustomSerializerService>()
                .BuildServiceProvider(new ServiceProviderOptions
                {
                    ValidateOnBuild = true
                });
        }

    }
}
