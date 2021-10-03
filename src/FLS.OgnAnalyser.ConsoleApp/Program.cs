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
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using FLS.OgnAnalyser.Service.Ogn;
using FLS.OgnAnalyser.Common.Converters;
using Serilog.Filters;
using Serilog.Context;

namespace FLS.OgnAnalyser.ConsoleApp
{
    public class Program
    {
        private static Microsoft.Extensions.Logging.ILogger _logger;
        static void Main(string[] args)
        {
            var services = new ServiceCollection();

            ConfigureServices(services);

            using (ServiceProvider serviceProvider = services.BuildServiceProvider())
            {
                _logger = serviceProvider.GetService<ILogger<Program>>();
                AnalyserService analyser = serviceProvider.GetService<AnalyserService>();

                analyser.OnTakeoff += (sender, e) =>
                {
                    using (LogContext.PushProperty("FLS", true))
                    {
                        _logger.LogInformation("{Aircraft} {Immatriculation} - Took off from {NearLocation} ({DepartureLocationY}, {DepartureLocationX})", e.Flight.Aircraft, e.Immatriculation, e.NearLocation, e.Flight.DepartureLocation?.Y, e.Flight.DepartureLocation?.X);
                    }

                    Console.WriteLine($"{DateTime.UtcNow}: {e.Flight.Aircraft} {e.Immatriculation} - Took off from {e.NearLocation} ({e.Flight.DepartureLocation?.Y}, {e.Flight.DepartureLocation?.X})");
                };

                analyser.OnLanding += (sender, e) =>
                {
                    using (LogContext.PushProperty("FLS", true))
                    {
                        _logger.LogInformation("{Aircraft} {Immatriculation} - Landed at {NearLocation} ({ArrivalLocationY}, {ArrivalLocationX})", e.Flight.Aircraft, e.Immatriculation, e.NearLocation, e.Flight.ArrivalLocation?.Y, e.Flight.ArrivalLocation?.X);
                    }

                    Console.WriteLine($"{DateTime.UtcNow}: {e.Flight.Aircraft} {e.Immatriculation} - Landed at {e.NearLocation} ({e.Flight.ArrivalLocation?.Y}, {e.Flight.ArrivalLocation?.X})");
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

                    Console.WriteLine($"{DateTime.UtcNow}: {e.Flight.Aircraft}  {e.Immatriculation} - Radar contact near {e.NearLocation} at {lastPositionUpdate.Latitude}, {lastPositionUpdate.Longitude} @ {lastPositionUpdate.Altitude}ft {lastPositionUpdate.Heading.ToHeadingArrow()}");
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
            services.AddTransient<OgnDevices>(x => FetchOgnDevices());

            var serilogLogger = new LoggerConfiguration()
                .WriteTo
                .File("FLSOgnAnalyser.log")
                .WriteTo.Logger(lc => lc
                    .Enrich.FromLogContext()
                    .Filter.ByIncludingOnly(Matching.WithProperty("FLS"))
                    .WriteTo
                    .File("FLSOgnAnalyserEvents.log"))
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

        private static OgnDevices FetchOgnDevices()
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var ddbTask = Task.Run(async () => await httpClient.GetAsync("http://ddb.glidernet.org/download?j=1"));

                    if (ddbTask.Result.IsSuccessStatusCode)
                    {
                        var ddbContent = ddbTask.Result.Content.ReadAsStringAsync();
                        var ognDevices = JsonConvert.DeserializeObject<OgnDevices>(ddbContent.Result, new JsonBooleanConverter());
                        return ognDevices;
                    }
                }
            }
            catch (Exception ex)
            {
                //Logger.Error(ex, $"Error while processing synchronisation. Error: {ex.Message}");
            }
            
            return new OgnDevices();
        }
    }
}
