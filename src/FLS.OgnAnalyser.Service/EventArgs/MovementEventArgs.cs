using Skyhop.FlightAnalysis.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace FLS.OgnAnalyser.Service.EventArgs
{
    public class MovementEventArgs
    {
        public MovementEventArgs(Flight flight, string nearLocation)
        {
            Flight = flight;
            NearLocation = nearLocation;
        }

        public Flight Flight { get; }

        public string NearLocation { get; }
    }
}
