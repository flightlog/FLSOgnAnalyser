using System;
using System.Collections.Generic;
using System.Text;
using Skyhop.FlightAnalysis.Models;

namespace FLS.OgnAnalyser.Service.Extensions
{
    public static class FlightExtensions
    {
        public static string GetFullFlightInformation(this Flight flight)
        {
            var sb = new StringBuilder();
            sb.Append("ID: ");
            sb.Append(flight.Id);
            sb.Append("Aircraft: ");
            sb.Append(flight.Aircraft);
            sb.Append("State: ");
            sb.Append(flight.State);
            sb.Append("Completed: ");
            sb.Append(flight.Completed);
            sb.Append("LaunchMethod: ");
            sb.Append(flight.LaunchMethod);
            sb.Append("LaunchFinished: ");
            sb.Append(flight.LaunchFinished);

            sb.Append("DepartureInfoFound: ");
            sb.Append(flight.DepartureInfoFound);
            sb.Append("DepartureTime: ");
            sb.Append(flight.DepartureTime);
            sb.Append("DepartureLocation: ");
            sb.Append(flight.DepartureLocation);
            sb.Append("DepartureHeading: ");
            sb.Append(flight.DepartureHeading);

            sb.Append("ArrivalInfoFound: ");
            sb.Append(flight.ArrivalInfoFound);
            sb.Append("ArrivalTime: ");
            sb.Append(flight.ArrivalTime);
            sb.Append("ArrivalLocation: ");
            sb.Append(flight.ArrivalLocation);
            sb.Append("ArrivalHeading: ");
            sb.Append(flight.ArrivalHeading);

            return sb.ToString();
        }
    }
}
