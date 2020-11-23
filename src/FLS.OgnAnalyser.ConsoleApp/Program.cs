using FLS.OgnAnalyser.Service;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Linq;

namespace FLS.OgnAnalyser.ConsoleApp
{
    public class Program
    {
        static void Main(string[] args)
        {
            var services = new ServiceCollection();
            ConfigureServices(services);

            using (ServiceProvider serviceProvider = services.BuildServiceProvider())
            {
                AnalyserService analyser = serviceProvider.GetService<AnalyserService>();

                analyser.OnTakeoff += (sender, e) =>
                {

                    Console.WriteLine($"{DateTime.UtcNow}: {e.Flight.Aircraft} - Took off from {e.Flight.DepartureLocation.X}, {e.Flight.DepartureLocation.Y}");
                };

                analyser.OnLanding += (sender, e) =>
                {
                    Console.WriteLine($"{DateTime.UtcNow}: {e.Flight.Aircraft} - Landed at {e.Flight.ArrivalLocation.X}, {e.Flight.ArrivalLocation.Y}");
                };

                analyser.OnRadarContact += (sender, e) =>
                {
                    if (e.Flight.PositionUpdates.Any() == false)
                    {
                        return;
                    }

                    var lastPositionUpdate = e.Flight.PositionUpdates.OrderByDescending(q => q.TimeStamp).First();

                    Console.WriteLine($"{DateTime.UtcNow}: {e.Flight.Aircraft} - Radar contact at {lastPositionUpdate.Latitude}, {lastPositionUpdate.Longitude} @ {lastPositionUpdate.Altitude}ft {lastPositionUpdate.Heading.ToHeadingArrow()}");
                };

                analyser.OnContextDispose += (sender, e) =>
                {
                    Console.WriteLine($"{DateTime.UtcNow}: {e.Context.Flight.Aircraft} - Context disposed");
                };

                analyser.Run();

                Console.WriteLine("Currently checking to see if we can receive some information!");
                Console.Read();
            }
        }

        private static void ConfigureServices(ServiceCollection services)
        {
            services.AddTransient<AnalyserService>();

            var serilogLogger = new LoggerConfiguration()
            .WriteTo.File("FLSOgnAnalyser.log")
            .CreateLogger();

            services.AddLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Debug);
                builder.AddSerilog(logger: serilogLogger, dispose: true);
            });
        }
    }
}
