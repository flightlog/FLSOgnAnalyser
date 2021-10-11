using Skyhop.FlightAnalysis.Models;
using System;

namespace FLS.OgnAnalyser.ConsoleApp.FLS
{
    public class LandingDetails
    {
        public string OgnDeviceId { get; set; }

        public AircraftType AircraftType { get; set; }

        public string Immatriculation { get; set; }

        public string LandingLocationIcaoCode { get; set; }

        public DateTime LandingTimeUtc { get; set; }
    }
}
