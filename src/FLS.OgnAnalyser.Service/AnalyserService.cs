using System;
using System.Collections.Generic;
using System.Text;
using System;
using System.Collections.Generic;
using Boerman.AprsClient;
using Boerman.AprsClient.Enums;
using Skyhop.FlightAnalysis;
using Skyhop.FlightAnalysis.Models;
using NetTopologySuite.Geometries;

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

        public AnalyserService()
        {

        }

        public void Run()
        {
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

                }
            };

            AprsClient.Open();
        }

        private void SubscribeContextFactoryEventHandlers(FlightContextFactory factory)
        {
            // Subscribe to the events so we can propagate 'em via the factory
            factory.OnTakeoff += (sender, args) => OnTakeoff?.Invoke(sender, args);
            factory.OnLaunchCompleted += (sender, args) => OnLaunchCompleted?.Invoke(sender, args);
            factory.OnLanding += (sender, args) => OnLanding?.Invoke(sender, args);
            factory.OnRadarContact += (sender, args) => OnRadarContact?.Invoke(sender, args);
            factory.OnCompletedWithErrors += (sender, args) => OnCompletedWithErrors?.Invoke(sender, args);
            factory.OnContextDispose += (sender, args) => OnContextDispose?.Invoke(sender, args);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
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
