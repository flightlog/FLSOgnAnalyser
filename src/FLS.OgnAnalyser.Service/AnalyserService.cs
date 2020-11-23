using System;
using Boerman.AprsClient;
using Boerman.AprsClient.Enums;
using Skyhop.FlightAnalysis;
using Skyhop.FlightAnalysis.Models;
using NetTopologySuite.Geometries;
using Microsoft.Extensions.Logging;
using System.Linq;
using Humanizer;

namespace FLS.OgnAnalyser.Service
{
    public class AnalyserService : IDisposable
    {
        public event EventHandler<OnCompletedWithErrorsEventArgs> OnCompletedWithErrors;
        public event EventHandler<OnTakeoffEventArgs> OnTakeoff;
        public event EventHandler<OnLaunchCompletedEventArgs> OnLaunchCompleted;
        public event EventHandler<OnLandingEventArgs> OnLanding;
        public event EventHandler<OnRadarContactEventArgs> OnRadarContact;
        public event EventHandler<OnContextDisposedEventArgs> OnContextDispose;

        private Listener AprsClient;
        private readonly ILogger _logger;
        //private OgnDevices OgnDevices;
        //public List<Airport> Airports = new List<Airport>();

        private FlightContextFactory FlightContextFactory = new FlightContextFactory(options =>
        {
            options.NearbyRunwayAccessor = (Point point, double distance) =>
            {
                return new[]
                {
                        new Runway(
                            new Point(8.385181, 46.970523, 1476),
                            new Point(8.408432, 46.978157, 1476)
                        ),
                        new Runway(
                            new Point(8.760951, 47.374918, 1759),
                            new Point(8.754110, 47.377973, 1759))
                    };
            };
        });
        private bool disposedValue;

        public AnalyserService(ILogger<AnalyserService> logger)
        {
            _logger = logger;
        }

        public void Run()
        {
            _logger.LogInformation("Starting AnalyserService");
            SubscribeContextFactoryEventHandlers(FlightContextFactory);


            var builder = new AprsFilterBuilder();
            builder.AddFilter(new AprsFilter.Range(46.801111, 8.226667, 250)); // geographical center of switzerland
            var filter = builder.GetFilter();

            AprsClient = new Listener(new Config()
            {
                Callsign = @"Speck78",
                Password = "-1",
                Uri = "aprs.glidernet.org", // glidern1.glidernet.org // aprs.glidernet.org
                UseOgnAdditives = true,
                Port = 14580,
                Filter = filter
            });

            AprsClient.PacketReceived += (sender, e) =>
            {
                try
                {
                    if (e.AprsMessage.DataType == DataType.Status) return;

                    var posUpdate = new Skyhop.FlightAnalysis.Models.PositionUpdate(
                        e.AprsMessage.Callsign,
                        e.AprsMessage.ReceivedDate,
                        e.AprsMessage.Latitude.AbsoluteValue,
                        e.AprsMessage.Longitude.AbsoluteValue,
                        e.AprsMessage.Altitude.FeetAboveSeaLevel,
                        e.AprsMessage.Speed.Knots,
                        e.AprsMessage.Direction.ToDegrees());
                    try
                    {
                        FlightContextFactory.Process(posUpdate);
                    }
                    catch (Exception exception)
                    {

                    }
                }
                catch (Exception ex)
                {
                    //ColorConsole.WriteLine($"Error: {ex.Message}: AprsMessage: {e.AprsMessage}", ConsoleColor.Red);
                    _logger.LogError(ex, "Error: {ExceptionMessage}: AprsMessage: {AprsMessage}", ex.Message, e.AprsMessage);
                }
            };

            AprsClient.Open();
        }

        private void SubscribeContextFactoryEventHandlers(FlightContextFactory factory)
        {
            // Subscribe to the events so we can propagate 'em via the factory
            factory.OnTakeoff += (sender, args) =>
            {
                _logger.LogInformation("{UtcNow}: {Aircraft} - Took off from {DepartureLocationX}, {DepartureLocationY}", DateTime.UtcNow, args.Flight.Aircraft, args.Flight.DepartureLocation.X, args.Flight.DepartureLocation.Y);
                OnTakeoff?.Invoke(sender, args);
            };

            factory.OnLaunchCompleted += (sender, args) =>
            {
                _logger.LogInformation("{UtcNow}: {Aircraft} - launch completed", DateTime.UtcNow, args.Flight.Aircraft);
                OnLaunchCompleted?.Invoke(sender, args);
            };

            factory.OnLanding += (sender, args) =>
            {
                _logger.LogInformation("{UtcNow}: {Aircraft} - Landed at {DepartureLocationX}, {DepartureLocationY}", DateTime.UtcNow, args.Flight.Aircraft, args.Flight.DepartureLocation.X, args.Flight.DepartureLocation.Y);
                OnLanding?.Invoke(sender, args);
            };

            factory.OnRadarContact += (sender, args) =>
            {
                if (args.Flight.PositionUpdates.Any() == false)
                {
                    return;
                }

                var lastPositionUpdate = args.Flight.PositionUpdates.OrderByDescending(q => q.TimeStamp).First();

                _logger.LogInformation("{UtcNow}: {Aircraft} - Radar contact at {LastPositionLatitude}, {LastPositionLongitude} @ {LastPositionAltitude}ft {LastPositionHeading}", DateTime.UtcNow, args.Flight.Aircraft, lastPositionUpdate.Latitude, lastPositionUpdate.Longitude, lastPositionUpdate.Altitude, lastPositionUpdate.Heading.ToHeadingArrow());

                OnRadarContact?.Invoke(sender, args);
            };

            factory.OnCompletedWithErrors += (sender, args) =>
            {
                _logger.LogInformation("{UtcNow}: {Aircraft} - Flight completed with errors", DateTime.UtcNow, args.Flight.Aircraft);
                OnCompletedWithErrors?.Invoke(sender, args);
            };

            factory.OnContextDispose += (sender, args) =>
            {
                _logger.LogInformation("{UtcNow}: {Aircraft} - Context disposed", DateTime.UtcNow, args.Context.Flight.Aircraft);
                OnContextDispose?.Invoke(sender, args);
            };
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _logger.LogInformation("Disposing AnalyserService");

                    if (AprsClient != null)
                    {
                        AprsClient.Close();
                        AprsClient.Dispose();
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~AnalyserService()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
