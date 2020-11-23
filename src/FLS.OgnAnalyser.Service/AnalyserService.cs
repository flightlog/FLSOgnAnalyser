using System;
using System.Collections.Generic;
using System.Linq;
using Boerman.AprsClient;
using Boerman.AprsClient.Enums;
using Skyhop.FlightAnalysis;
using Skyhop.FlightAnalysis.Models;
using NetTopologySuite.Geometries;
using Microsoft.Extensions.Logging;
using Humanizer;
using FLS.OgnAnalyser.Service.Extensions;
using FLS.OgnAnalyser.Service.Airports;
using FLS.OgnAnalyser.Service.EventArgs;
using FLS.OgnAnalyser.Service.Ogn;

namespace FLS.OgnAnalyser.Service
{
    public class AnalyserService : IDisposable
    {
        public event EventHandler<OnCompletedWithErrorsEventArgs> OnCompletedWithErrors;
        public event EventHandler<OnLaunchCompletedEventArgs> OnLaunchCompleted;
        public event EventHandler<OnContextDisposedEventArgs> OnContextDispose;
        public event EventHandler<MovementEventArgs> OnLanding;
        public event EventHandler<MovementEventArgs> OnTakeoff;
        public event EventHandler<MovementEventArgs> OnRadarContact;

        private Listener AprsClient;
        private readonly ILogger _logger;
        private OgnDevices _ognDevices;
        private List<Airport> _airports = new List<Airport>();

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

        public AnalyserService(List<Airport> airports, OgnDevices ognDevices, ILogger<AnalyserService> logger)
        {
            _airports = airports;
            _ognDevices = ognDevices;
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
                var location = GetLocation(args.Flight.DepartureLocation);
                var immatriculation = GetImmatriculation(args.Flight.Aircraft);

                _logger.LogInformation("{UtcNow}: {Aircraft} {Immatriculation} - Took off from {Location} - Flight Info: {FlightInfo}", DateTime.UtcNow, args.Flight.Aircraft, immatriculation, location, args.Flight.GetFullFlightInformation());
                //OnTakeoff?.Invoke(sender, args);
                OnTakeoff?.Invoke(this, new MovementEventArgs(args.Flight, location, immatriculation));
            };

            factory.OnLaunchCompleted += (sender, args) =>
            {
                var immatriculation = GetImmatriculation(args.Flight.Aircraft);
                _logger.LogInformation("{UtcNow}: {Aircraft} {Immatriculation} - launch completed - Flight Info: {FlightInfo}", DateTime.UtcNow, args.Flight.Aircraft, immatriculation, args.Flight.GetFullFlightInformation());
                OnLaunchCompleted?.Invoke(sender, args);
            };

            factory.OnLanding += (sender, args) =>
            {
                var location = GetLocation(args.Flight.ArrivalLocation);
                var immatriculation = GetImmatriculation(args.Flight.Aircraft);

                _logger.LogInformation("{UtcNow}: {Aircraft} {Immatriculation} - Landed at {Location} - Flight Info: {FlightInfo}", DateTime.UtcNow, args.Flight.Aircraft, immatriculation, location, args.Flight.GetFullFlightInformation());
                //OnLanding?.Invoke(sender, args);
                OnLanding?.Invoke(this, new MovementEventArgs(args.Flight, location, immatriculation));
            };

            factory.OnRadarContact += (sender, args) =>
            {
                if (args.Flight.PositionUpdates.Any() == false)
                {
                    return;
                }

                var lastPositionUpdate = args.Flight.PositionUpdates.OrderByDescending(q => q.TimeStamp).First();
                var location = GetLocation(lastPositionUpdate.Location);
                var immatriculation = GetImmatriculation(args.Flight.Aircraft);

                _logger.LogInformation("{UtcNow}: {Aircraft} {Immatriculation} - Radar contact near {Location} at {LastPositionLatitude}, {LastPositionLongitude} @ {LastPositionAltitude}ft {LastPositionHeading} - Flight Info: {FlightInfo}", DateTime.UtcNow, args.Flight.Aircraft, immatriculation, location, lastPositionUpdate.Latitude, lastPositionUpdate.Longitude, lastPositionUpdate.Altitude, lastPositionUpdate.Heading.ToHeadingArrow(), args.Flight.GetFullFlightInformation());

                //OnRadarContact?.Invoke(sender, args);
                OnRadarContact?.Invoke(this, new MovementEventArgs(args.Flight, location, immatriculation));
            };

            factory.OnCompletedWithErrors += (sender, args) =>
            {
                var immatriculation = GetImmatriculation(args.Flight.Aircraft);
                _logger.LogInformation("{UtcNow}: {Aircraft} {Immatriculation} - Flight completed with errors", DateTime.UtcNow, args.Flight.Aircraft, immatriculation);
                OnCompletedWithErrors?.Invoke(sender, args);
            };

            factory.OnContextDispose += (sender, args) =>
            {
                var immatriculation = GetImmatriculation(args.Context.Flight.Aircraft);
                _logger.LogInformation("{UtcNow}: {Aircraft} {Immatriculation} - Context disposed", DateTime.UtcNow, args.Context.Flight.Aircraft, immatriculation);
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

        private string GetLocation(Point flightLocation)
        {
            if (flightLocation == null) return "no Location";

            foreach (var airport in _airports)
            {
                if (Distance(airport.GeoLocation.Point, flightLocation) <= 5)
                {
                    return $"{airport.Name} ({airport.Icao})";
                }
            }

            return "Unknown Airfield";
        }

        private string GetImmatriculation(string ognDeviceId)
        {
            foreach (var device in _ognDevices.Devices)
            {
                if (device.DeviceId == ognDeviceId.Substring(3, 6))
                {
                    return device.Registration;
                }
            }

            return "unknown";
        }

        /// <summary>
        /// see https://www.geodatasource.com/developers/c-sharp
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        //:::                                                                         :::
        //:::  This routine calculates the distance between two points (given the     :::
        //:::  latitude/longitude of those points). It is being used to calculate     :::
        //:::  the distance between two locations using GeoDataSource(TM) products    :::
        //:::                                                                         :::
        //:::  Definitions:                                                           :::
        //:::    South latitudes are negative, east longitudes are positive           :::
        //:::                                                                         :::
        //:::  Passed to function:                                                    :::
        //:::    lat1, lon1 = Latitude and Longitude of point 1 (in decimal degrees)  :::
        //:::    lat2, lon2 = Latitude and Longitude of point 2 (in decimal degrees)  :::
        //:::    unit = the unit you desire for results                               :::
        //:::           where: 'M' is statute miles (default)                         :::
        //:::                  'K' is kilometers                                      :::
        //:::                  'N' is nautical miles                                  :::
        //:::                                                                         :::
        //:::  Worldwide cities and other features databases with latitude longitude  :::
        //:::  are available at https://www.geodatasource.com                         :::
        //:::                                                                         :::
        //:::  For enquiries, please contact sales@geodatasource.com                  :::
        //:::                                                                         :::
        //:::  Official Web site: https://www.geodatasource.com                       :::
        //:::                                                                         :::
        //:::           GeoDataSource.com (C) All Rights Reserved 2018                :::
        //:::                                                                         :::
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private double Distance(Point p1, Point p2)
        {
            var lat1 = p1.Y;
            var lon1 = p1.X;
            var lat2 = p2.Y;
            var lon2 = p2.X;
            var unit = 'K';

            if ((lat1 == lat2) && (lon1 == lon2))
            {
                return 0;
            }
            else
            {
                double theta = lon1 - lon2;
                double dist = Math.Sin(deg2rad(lat1)) * Math.Sin(deg2rad(lat2)) + Math.Cos(deg2rad(lat1)) * Math.Cos(deg2rad(lat2)) * Math.Cos(deg2rad(theta));
                dist = Math.Acos(dist);
                dist = rad2deg(dist);
                dist = dist * 60 * 1.1515;
                if (unit == 'K')
                {
                    dist = dist * 1.609344;
                }
                else if (unit == 'N')
                {
                    dist = dist * 0.8684;
                }
                return (dist);
            }
        }

        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        //::  This function converts decimal degrees to radians             :::
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private double deg2rad(double deg)
        {
            return (deg * Math.PI / 180.0);
        }

        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        //::  This function converts radians to decimal degrees             :::
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private double rad2deg(double rad)
        {
            return (rad / Math.PI * 180.0);
        }
    }
}
