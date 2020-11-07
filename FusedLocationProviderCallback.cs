using System.Linq;

using Android.Gms.Location;
using Android.Util;

namespace PinPointTrackAp
{
    public class FusedLocationProviderCallback : LocationCallback
    {
        readonly MainActivity activity;
        public double currentLat;
        public double currentLng;
        private SeGeofencingActivity seGeofencingActivity;

        public FusedLocationProviderCallback(MainActivity activity)
        {
            this.activity = activity;
        }

        public FusedLocationProviderCallback(SeGeofencingActivity seGeofencingActivity)
        {
            this.seGeofencingActivity = seGeofencingActivity;
        }

        public override void OnLocationAvailability(LocationAvailability locationAvailability)
        {
            Log.Debug("FusedLocationProviderSample", "IsLocationAvailable: {0}",locationAvailability.IsLocationAvailable);
        }


        public override void OnLocationResult(LocationResult result)
        {
            if (result.Locations.Any())
            {
                var location = result.Locations.First();
                currentLat = location.Latitude;
                currentLng = location.Longitude;
            }
            else
            {
                currentLat = 0;
                currentLng = 0;
            }
        }
    }
}
