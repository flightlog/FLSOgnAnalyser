using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Distance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FLS.OgnAnalyser.Service.Airports
{
    //<OPENAIP VERSION="2794205" DATAFORMAT="1.1">
    //<WAYPOINTS>
    [XmlRoot("OPENAIP")]
    public class OpenAipAirports
    {
        [XmlArray("WAYPOINTS")]
        [XmlArrayItem(ElementName = "AIRPORT")]
        public List<Airport> Airports { get; set; }
    }

    public class Airport
    {
        [XmlAttribute("TYPE")]
        public string Type { get; set; }

        [XmlElement("COUNTRY")]
        public string Country { get; set; }

        [XmlElement("NAME")]
        public string Name { get; set; }

        [XmlElement("ICAO")]
        public string Icao { get; set; }

        [XmlElement("GEOLOCATION")]
        public GeoLocation GeoLocation { get; set; }
    }

    public class GeoLocation
    {
        [XmlElement("LAT")]
        public double Latitude { get; set; }

        [XmlElement("LON")]
        public double Longitude { get; set; }

        [XmlElement("ELEV")]
        public Elevation Elevation { get; set; }

        [XmlIgnore]
        public Point Point
        {
            get
            {
                return new Point(Longitude, Latitude, Elevation.Altitude);
            }
        }

    }

    public class Elevation
    {
        [XmlText(DataType = "double")]
        public double Altitude { get; set; }

        [XmlAttribute("UNIT")]
        public string Unit { get; set; }
    }
}
