using Skyhop.FlightAnalysis.Models;
using System;

namespace FLS.OgnAnalyser.ConsoleApp.FLS
{
    public class TakeOffDetails
    {
        public string OgnDeviceId { get; set; }

        public AircraftType AircraftType { get; set; }

        public string Immatriculation { get; set; }

        public string TakeOffLocationIcaoCode { get; set; }

        public DateTime TakeOffTimeUtc { get; set; }
    }
}