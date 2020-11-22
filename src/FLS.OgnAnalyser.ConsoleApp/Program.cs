using FLS.OgnAnalyser.Service;
using Humanizer;
using System;
using System.Linq;

namespace FLS.OgnAnalyser.ConsoleApp
{
    public class Program
    {
        private static AnalyserService _analyser;
        static void Main(string[] args)
        {
            _analyser = new AnalyserService();

            _analyser.OnTakeoff += (sender, e) => {

                Console.WriteLine($"{DateTime.UtcNow}: {e.Flight.Aircraft} - Took off from {e.Flight.DepartureLocation.X}, {e.Flight.DepartureLocation.Y}");
            };

            _analyser.OnLanding += (sender, e) => {
                Console.WriteLine($"{DateTime.UtcNow}: {e.Flight.Aircraft} - Landed at {e.Flight.ArrivalLocation.X}, {e.Flight.ArrivalLocation.Y}");
            };

            _analyser.OnRadarContact += (sender, e) => {
                if (e.Flight.PositionUpdates.Any() == false)
                {
                    return;
                }

                var lastPositionUpdate = e.Flight.PositionUpdates.OrderByDescending(q => q.TimeStamp).First();

                Console.WriteLine($"{DateTime.UtcNow}: {e.Flight.Aircraft} - Radar contact at {lastPositionUpdate.Latitude}, {lastPositionUpdate.Longitude} @ {lastPositionUpdate.Altitude}ft {lastPositionUpdate.Heading.ToHeadingArrow()}");
            };

            _analyser.OnContextDispose += (sender, e) =>
            {
                Console.WriteLine($"{DateTime.UtcNow}: {e.Context.Flight.Aircraft} - Context disposed");
            };

            _analyser.Run();

            Console.WriteLine("Currently checking to see if we can receive some information!");
            Console.Read();

            _analyser.Dispose();
        }
    }
}
