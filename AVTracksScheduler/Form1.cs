using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AVTracksScheduler
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            SendEmailNotification();
            InitializeComponent();
        }

        avtracksdbEntities db = new avtracksdbEntities();

        public List<NotificationModels> GetMapData()
        {
            var result = new List<NotificationModels>();
            var latestDataList = db.map_data.Where(x => x.device_type != null).GroupBy(x => x.device_id)
                .Select(x => new
                {
                    DeviceId = x.Key,
                    DeviceType = x.FirstOrDefault().device_type,
                    Time = x.Max(y => y.time)
                }).ToList();

            var interval = DateTime.Now.AddHours(-1);
            latestDataList = latestDataList.Where(x => x.Time < interval).ToList();

            TextInfo ti = CultureInfo.InvariantCulture.TextInfo;
            foreach(var item in latestDataList)
            {
                var data = db.map_data.FirstOrDefault(x => x.device_id == item.DeviceId && x.device_type == item.DeviceType && x.time == item.Time);
                var mappingData = db.device_fleet_mapping.FirstOrDefault(x => x.device_id == data.device_id && (x.end_date == null || x.end_date >= DateTime.Now));
                var fleetName = mappingData == null ? "not mapped" : mappingData.md_fleet == null ? "not mapped" : mappingData.md_fleet.fleet_name;
                var activeTime = db.time_setup.FirstOrDefault(x => x.device_type.ToLower() == data.device_type.ToLower()).time;

                var decLat = decimal.Parse(data.latitude.ToString());
                var decLng = decimal.Parse(data.longitude.ToString());

                NotificationModels temp = new NotificationModels
                {
                    ID = data.id,
                    DeviceId = data.device_id,
                    DeviceType = ti.ToTitleCase(data.device_type),
                    FleetID = mappingData == null ? 0 : mappingData.fleet_id,
                    FleetName = fleetName,
                    Date = data.date.ToString("yyyy-MM-dd"),
                    Time = data.time.ToString("HH:mm:ss"),
                    Latitude = data.latitude,
                    Longitude = data.longitude,
                    Altitude = data.altitude,
                    Course = data.course,
                    SpeedKnots = data.speed_knots,
                    Satellites = data.satellites,
                    HDOP = data.hdop,
                    Battery = data.battery == null ? 0 : data.battery,
                    CellularStrength = data.cellular_strength == null ? 0 : data.cellular_strength,
                    DeviceStatus = data.device_status == null ? "-" : ti.ToTitleCase(data.device_status),
                    IsActive = (DateTime.Now - data.time).TotalHours < activeTime ? true : false,
                    PowerStatus = data.power_status == null ? "-" : ti.ToTitleCase(data.power_status),
                    OriginalDateTime = data.time,
                    Position = data.position != null ? data.position : GetGeofenceName(decLat, decLng),
                    Timestamp = data.timestamp.Value.ToString("yyyy-MM-dd HH:mm:ss")
                };
                result.Add(temp);
            }

            return result.OrderByDescending(x => x.OriginalDateTime).ToList();
        }

        public List<GeofenceModels> GetGeofences()
        {
            List<GeofenceModels> models = new List<GeofenceModels>();
            var geofences = db.MD_Geofence.Where(x => x.status == 1).ToList();
            foreach (var geo in geofences)
            {
                List<LatLng> listLatLng = new List<LatLng>()
                {
                    new LatLng { Lat = geo.lat1, Lng = geo.long1 },
                    new LatLng { Lat = geo.lat2, Lng = geo.long2 },
                    new LatLng { Lat = geo.lat3, Lng = geo.long3 },
                    new LatLng { Lat = geo.lat4, Lng = geo.long4 },
                };

                listLatLng.OrderBy(x => x.Lat);

                var temp = new GeofenceModels
                {
                    GeofenceID = geo.geofenceID,
                    GeofenceName = geo.geofenceName,
                    Latitude1 = listLatLng[0].Lat,
                    Latitude2 = listLatLng[1].Lat,
                    Latitude3 = listLatLng[2].Lat,
                    Latitude4 = listLatLng[3].Lat,
                    Longitude1 = listLatLng[0].Lng,
                    Longitude2 = listLatLng[1].Lng,
                    Longitude3 = listLatLng[2].Lng,
                    Longitude4 = listLatLng[3].Lng,
                };

                models.Add(temp);
            }
            return models;
        }

        public string GetGeofenceName(decimal lat, decimal lon)
        {
            string geoName = "Uncovered Area";
            var geofence_data = GetGeofences();
            for (int i = 0; i < geofence_data.Count; i++)
            {
                decimal lat1 = geofence_data[i].Latitude1;
                decimal lat2 = geofence_data[i].Latitude2;
                decimal lat3 = geofence_data[i].Latitude3;
                decimal lat4 = geofence_data[i].Latitude4;
                decimal lon1 = geofence_data[i].Longitude1;
                decimal lon2 = geofence_data[i].Longitude2;
                decimal lon3 = geofence_data[i].Longitude3;
                decimal lon4 = geofence_data[i].Longitude4;
                List<List<decimal>> polygon = new List<List<decimal>>()
                {
                    new List<decimal>() { lat1, lon1 },
                    new List<decimal>() { lat2, lon2 },
                    new List<decimal>() { lat4, lon4 },
                    new List<decimal>() { lat3, lon3 }
                };
                bool checkInside = Inside(new List<decimal>() { lat, lon }, polygon);
                if (checkInside == true)
                {
                    geoName = geofence_data[i].GeofenceName;
                }
            }
            return geoName;
        }

        public bool Inside(List<decimal> point, List<List<decimal>> vs)
        {
            // ray-casting algorithm based on
            // https://wrf.ecse.rpi.edu/Research/Short_Notes/pnpoly.html
            decimal x = point[0], y = point[1];
            bool inside = false;
            for (int i = 0, j = vs.Count - 1; i < vs.Count; j = i++)
            {
                decimal xi = vs[i][0], yi = vs[i][1];
                decimal xj = vs[j][0], yj = vs[j][1];
                bool intersect = ((yi > y) != (yj > y))
                    && (x < (xj - xi) * (y - yi) / (yj - yi) + xi);
                if (intersect) inside = !inside;
            }
            return inside;
        }

        public void SendEmailNotification()
        {
            List<NotificationModels> mapDataList = GetMapData();

            foreach (var data in mapDataList)
            {
                string body = "<h3>Device Not Active Alert</h3><br />";
                body += "<table><tbody>";
                body += "<tr><td>Device ID</td><td>:</td><td>" + data.DeviceId + "</td></tr>";
                body += "<tr><td>Device Type</td><td>:</td><td>" + data.DeviceType + "</td></tr>";
                body += "<tr><td>Fleet</td><td>:</td><td>" + data.FleetName + "</td></tr>";
                body += "<tr><td>Last Update</td><td>:</td><td>" + data.OriginalDateTime.ToString("yyyy-MM-dd HH:mm:ss") + "</td></tr>";
                body += "<tr><td>Position</td><td>:</td><td>" + data.Position + "</td></tr>";
                body += "<tr><td>Latitude</td><td>:</td><td>" + data.Latitude + "</td></tr>";
                body += "<tr><td>Longitude</td><td>:</td><td>" + data.Longitude + "</td></tr>";
                body += "<tr><td>Device Status</td><td>:</td><td>" + data.DeviceStatus + "</td></tr>";
                body += "<tr><td>Power Status</td><td>:</td><td>" + data.PowerStatus + "</td></tr>";
                body += "<tr><td>Timestamp</td><td>:</td><td>" + data.Timestamp + "</td></tr>";
                body += "</tbody></table>";


                using (MailMessage message = new MailMessage())
                {
                    message.From = new MailAddress("muhamad.azis@adaro.com");
                    message.To.Add("muhamad.azis@adaro.com");

                    message.Subject = "Alert Not Active Device";
                    message.Body = "<h1>Alert Testing</h1>";
                    message.IsBodyHtml = true;

                    using (SmtpClient client = new SmtpClient
                    {
                        Host = "www.adaro.com",
                        Port = 25,
                        Credentials = new NetworkCredential("muhamad.azis@adaro.com", "12345678")
                    })
                    {
                        client.Send(message);
                    }
                }
            }

            Environment.Exit(0);
        }

        public class NotificationModels
        {
            public int ID { get; set; }
            public string DeviceId { get; set; }
            public int? FleetID { get; set; }
            public string FleetName { get; set; }
            public string DeviceType { get; set; }
            public string Date { get; set; }
            public string Time { get; set; }
            public double? Latitude { get; set; }
            public double? Longitude { get; set; }
            public double? Altitude { get; set; }
            public double? SpeedKnots { get; set; }
            public double? Course { get; set; }
            public int? Satellites { get; set; }
            public int? HDOP { get; set; }
            public int? Battery { get; set; }
            public int? CellularStrength { get; set; }
            public string DeviceStatus { get; set; }
            public bool IsActive { get; set; }

            public string PowerStatus { get; set; }

            public DateTime OriginalDateTime { get; set; }
            public bool IsPairing { get; set; }

            public string Position { get; set; }
            public string Tug { get; set; }
            public string Barge { get; set; }

            public int? FleetParentId { get; set; }
            public int? FleetChildId { get; set; }

            public string stringTime { get; set; }
            public string Timestamp { get; set; }
        }
        public class GeofenceModels
        {
            public int GeofenceID { get; set; }
            public string GeofenceName { get; set; }
            public decimal Latitude1 { get; set; }
            public decimal Longitude1 { get; set; }
            public decimal Latitude2 { get; set; }
            public decimal Longitude2 { get; set; }
            public decimal Latitude3 { get; set; }
            public decimal Longitude3 { get; set; }
            public decimal Latitude4 { get; set; }
            public decimal Longitude4 { get; set; }
            public string CoalAreaType { get; set; }
            public string FuelAreaType { get; set; }
            public byte CycleIdentifier { get; set; }
            public int OrderGeofence { get; set; }
            public int TripSectionID { get; set; }
            public int? PortID { get; set; }
            public int? LayerZIndex { get; set; }
            public int Distance { get; set; }
            public decimal UpstreamSteamingDuration { get; set; }
            public decimal DownstreamSteamingDuration { get; set; }
            public decimal UpstreamSteamingDuration_MBP { get; set; }
            public decimal DownstreamSteamingDuration_MBP { get; set; }
            public decimal? AvgDurationMinutes { get; set; }
            public byte Status { get; set; }
            public int? LocationAreaID { get; set; }
        }
        public class LatLng
        {
            public decimal Lat { get; set; }
            public decimal Lng { get; set; }
        }
    }
}
