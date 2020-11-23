using FLS.OgnAnalyser.Service;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Linq;
using System.IO;
using System.Xml.Serialization;
using FLS.OgnAnalyser.Service.Airports;
using System.Collections.Generic;

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

                    Console.WriteLine($"{DateTime.UtcNow}: {e.Flight.Aircraft} - Took off from {e.NearLocation} ({e.Flight.DepartureLocation?.Y}, {e.Flight.DepartureLocation?.X})");
                };

                analyser.OnLanding += (sender, e) =>
                {
                    Console.WriteLine($"{DateTime.UtcNow}: {e.Flight.Aircraft} - Landed at {e.NearLocation} ({e.Flight.ArrivalLocation?.Y}, {e.Flight.ArrivalLocation?.X})");
                };

                analyser.OnLaunchCompleted += (sender, e) =>
                {
                    Console.WriteLine($"{DateTime.UtcNow}: {e.Flight.Aircraft} - launch completed {e.Flight.DepartureLocation?.Y}, {e.Flight.DepartureLocation?.X}");
                };

                analyser.OnRadarContact += (sender, e) =>
                {
                    if (e.Flight.PositionUpdates.Any() == false)
                    {
                        return;
                    }

                    var lastPositionUpdate = e.Flight.PositionUpdates.OrderByDescending(q => q.TimeStamp).First();

                    Console.WriteLine($"{DateTime.UtcNow}: {e.Flight.Aircraft} - Radar contact near {e.NearLocation} at {lastPositionUpdate.Latitude}, {lastPositionUpdate.Longitude} @ {lastPositionUpdate.Altitude}ft {lastPositionUpdate.Heading.ToHeadingArrow()}");
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
            services.AddTransient<List<Airport>>(x => LoadAirports());

            var serilogLogger = new LoggerConfiguration()
            .WriteTo.File("FLSOgnAnalyser.log")
            .CreateLogger();

            services.AddLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Debug);
                builder.AddSerilog(logger: serilogLogger, dispose: true);
            });
        }

        private static List<Airport> LoadAirports()
        {
            using (var fileStream = File.Open("openaip_airports_switzerland_ch.aip", FileMode.Open))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(OpenAipAirports));
                var xml = (OpenAipAirports)serializer.Deserialize(fileStream);

                return xml.Airports;
            }
        }
    }
}
