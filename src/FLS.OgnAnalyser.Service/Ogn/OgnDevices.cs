using Newtonsoft.Json;
using System.Collections.Generic;

namespace FLS.OgnAnalyser.Service.Ogn
{
    public class OgnDevices
    {
        public OgnDevices()
        {
            Devices = new List<OgnDevice>();
        }

        [JsonProperty(PropertyName = "devices")]
        public List<OgnDevice> Devices { get; set; }
    }
}
