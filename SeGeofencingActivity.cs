using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Gms.Common;
using Android.Gms.Location;
using Android.Locations;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Util;
using Android.Views;
using Android.Widget;
using Firebase;
using Firebase.Firestore;
using Firebase.Auth;
using Firebase.Database;

namespace PinPointTrackAp
{
    [Activity(Theme = "@style/AppTheme",Label = "SeGeofencingActivity")]
    public class SeGeofencingActivity : Activity,BottomNavigationView.IOnNavigationItemSelectedListener
    {
        public double LocLat = 0;
        public double LocLng = 0;
        public double LocRadius = 0;
        string UserEmail = "";

        //******************* Fused *******************************
        const long ONE_MINUTE = 60 * 1000;
        const long FIVE_MINUTES = 5 * ONE_MINUTE;
        const long TWO_MINUTES = 2 * ONE_MINUTE;
        static readonly int RC_LAST_LOCATION_PERMISSION_CHECK = 1000;


        static readonly string KEY_REQUESTING_LOCATION_UPDATES = "requesting_location_updates";

        FusedLocationProviderClient fusedLocationProviderClient;
        bool isGooglePlayServicesInstalled;
        bool isRequestingLocationUpdates;
        LocationCallback locationCallback;
        LocationRequest locationRequest;

        CoordinatorLayout cRootLayoutGeo;

        FirebaseAuth mAuth;
        FirebaseFirestore database;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.ActivitySetGeofencing);
            // Create your application here

            //navigation listener
            BottomNavigationView navigation = FindViewById<BottomNavigationView>(Resource.Id.navigation);
            navigation.SetOnNavigationItemSelectedListener(this);
            IMenu m = navigation.Menu;
            IMenuItem i = m.GetItem(2);
            i.SetChecked(true);

            //Get user location
            Button btnUserLocation = FindViewById<Button>(Resource.Id.btnGetCurrentLocation);
            btnUserLocation.Click += BtnUserLocation_Click;

            //add geo
            Button btnADDGEO = FindViewById<Button>(Resource.Id.btnAddGeofencing);
            btnADDGEO.Click += BtnADDGEO_Click;

            //get email
            if (System.String.IsNullOrEmpty(UserEmail))
                UserEmail = Intent.Extras.GetString("Email");


            //********************************************************
            if (savedInstanceState != null)
            {
                isRequestingLocationUpdates = savedInstanceState.KeySet().Contains(KEY_REQUESTING_LOCATION_UPDATES) &&
                                              savedInstanceState.GetBoolean(KEY_REQUESTING_LOCATION_UPDATES);
            }
            else
            {
                isRequestingLocationUpdates = false;
            }
            //********************************************************

            cRootLayoutGeo = (CoordinatorLayout)FindViewById(Resource.Id.cRootLayoutGeo);

            //*******************************
            // Set our view from the "main" layout resource
            isGooglePlayServicesInstalled = IsGooglePlayServicesInstalled();

            locationRequest = new LocationRequest()
                               .SetPriority(LocationRequest.PriorityHighAccuracy)
                               .SetInterval(FIVE_MINUTES)
                               .SetFastestInterval(TWO_MINUTES);
            locationCallback = new FusedLocationProviderCallback(this);

            fusedLocationProviderClient = LocationServices.GetFusedLocationProviderClient(this);

            //initialise firebase
            InitializeFirebase();

            mAuth = FirebaseAuth.Instance;


        }

        void InitializeFirebase()
        {
            FirebaseApp app = FirebaseApp.InitializeApp(this);

            if (app == null)
            {
                var options = new FirebaseOptions.Builder()
                    .SetProjectId("mobileappdb20")
                    .SetApplicationId("mobileappdb20")
                    .SetApiKey("AIzaSyDMIUDM5MP_Zmbz3SbbgyYB8RnsHkhsQ3E")
                    .SetDatabaseUrl("https://mobileappdb20.firebaseio.com")
                    .SetStorageBucket("mobileappdb20.appspot.com")
                    .Build();

                app = FirebaseApp.InitializeApp(this, options);
                database = FirebaseFirestore.GetInstance(app);
            }
            else
            {
                database = FirebaseFirestore.GetInstance(app);
            }
        }
        private void BtnADDGEO_Click(object sender, EventArgs e)
        {
            EditText radius = FindViewById<EditText>(Resource.Id.txtRadius);


            if (LocLat <= 0 && LocLng <= 0)
            {
                Snackbar.Make(cRootLayoutGeo, "Sorry!, Get current location first.", Snackbar.LengthShort).Show();
                return;
            }
            
            if (String.IsNullOrEmpty(radius.Text))
            {
                Snackbar.Make(cRootLayoutGeo, "Radius in meters not provided.", Snackbar.LengthShort).Show();
                return;
            }

            LocRadius = Double.Parse(radius.Text);

            if (LocRadius < 5)
            {
                Snackbar.Make(cRootLayoutGeo, "Radius must be 5 meters minimum.", Snackbar.LengthShort).Show();
                return;
            }
            //add to firebase
            DocumentReference docRef = database.Collection("User").Document(UserEmail);
            docRef.Update("l_lat", LocLat);
            docRef.Update("l_long", LocLng);
            docRef.Update("l_rD", LocRadius);

            Snackbar.Make(cRootLayoutGeo, "Geofencing successfully added.", Snackbar.LengthLong).Show();
        }

        //getcurrentlocation and pass geofencing details
        private void BtnUserLocation_Click(object sender, EventArgs e)
        {

            GetLastLocation();
            RequestLocationUpdates();
        }

        public bool OnNavigationItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.navigation_monitor:
                    Intent main = new Intent(this, typeof(MainActivity));
                    main.PutExtra("Email", UserEmail);
                    StartActivity(main);
                    //animation
                    OverridePendingTransition(Resource.Animation.@Side_In_Right, Resource.Animation.@Side_In_Left);
                    break;
                case Resource.Id.navigation_addchild:
                    Intent Register = new Intent(this, typeof(RegisterChildActivity));
                    Register.PutExtra("Email", UserEmail);
                    StartActivity(Register);
                    //animation
                    OverridePendingTransition(Resource.Animation.@Side_In_Right, Resource.Animation.@Side_In_Left);
                    break;
                case Resource.Id.navigation_geofencing:
                    Intent Geo = new Intent(this, typeof(SeGeofencingActivity));
                    Geo.PutExtra("Email", UserEmail);
                    StartActivity(Geo);

                    //animation
                    OverridePendingTransition(Resource.Animation.@Side_In_Right, Resource.Animation.@Side_In_Left);
                    //*********************************

                    break;
            }

            return true;
        }

        //**********************************************
        protected async void RequestLocationUpdates()
        {
            // No need to request location updates if we're already doing so.
            if (isRequestingLocationUpdates)
            {
                isRequestingLocationUpdates = false;
            }
            else
            {
                if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) == Permission.Granted)
                {
                    await StartRequestingLocationUpdates();
                    isRequestingLocationUpdates = true;
                }
                else
                {
                    RequestLocationPermission(RC_LAST_LOCATION_PERMISSION_CHECK);
                }
            }
        }

        protected async void GetLastLocation()
        {
            if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) == Permission.Granted)
            {
                await GetLastLocationFromDevice();
            }
            else
            {
                RequestLocationPermission(RC_LAST_LOCATION_PERMISSION_CHECK);
            }
        }

        async Task GetLastLocationFromDevice()
        {



            var location = await fusedLocationProviderClient.GetLastLocationAsync();

            if (location == null)
            {
                LocLat = 0;
                LocLng = 0;

            }
            else
            {
                TextView lat = FindViewById<TextView>(Resource.Id.txtLatitude);

                lat.Text = location.Latitude.ToString();

                LocLat = location.Latitude;

                TextView lng = FindViewById<TextView>(Resource.Id.txtLongitude);

                lng.Text = location.Longitude.ToString();

                LocLng = location.Longitude;
                // Handle exception that may have occurred in geocoding
               // Snackbar.Make(cRootLayoutGeo, LocLat.ToString() + "-----" + LocLng.ToString() + "=" + UserEmail, Snackbar.LengthLong).Show();
            }
        }

        void RequestLocationPermission(int requestCode)
        {
            if (ActivityCompat.ShouldShowRequestPermissionRationale(this, Manifest.Permission.AccessFineLocation))
            {
                Snackbar.Make(cRootLayoutGeo, Resource.String.permission_location_rationale, Snackbar.LengthIndefinite)
                        .SetAction(Resource.String.ok,
                                   delegate
                                   {
                                       ActivityCompat.RequestPermissions(this, new[] { Manifest.Permission.AccessFineLocation }, requestCode);
                                   })
                        .Show();
            }
            else
            {
                ActivityCompat.RequestPermissions(this, new[] { Manifest.Permission.AccessFineLocation }, requestCode);
            }
        }

        private async Task StartRequestingLocationUpdates()
        {
            await fusedLocationProviderClient.RequestLocationUpdatesAsync(locationRequest, locationCallback);
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            outState.PutBoolean(KEY_REQUESTING_LOCATION_UPDATES, isRequestingLocationUpdates);
            base.OnSaveInstanceState(outState);
        }

        protected override async void OnResume()
        {
            base.OnResume();
            if (CheckSelfPermission(Manifest.Permission.AccessFineLocation) == Permission.Granted)
            {
                if (isRequestingLocationUpdates)
                {
                    await StartRequestingLocationUpdates();
                }
            }
            else
            {
                RequestLocationPermission(RC_LAST_LOCATION_PERMISSION_CHECK);
            }
        }


        bool IsGooglePlayServicesInstalled()
        {
            var queryResult = GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(this);
            if (queryResult == ConnectionResult.Success)
            {
                Log.Info("MainActivity", "Google Play Services is installed on this device.");
                return true;
            }

            if (GoogleApiAvailability.Instance.IsUserResolvableError(queryResult))
            {
                var errorString = GoogleApiAvailability.Instance.GetErrorString(queryResult);
                Log.Error("MainActivity", "There is a problem with Google Play Services on this device: {0} - {1}",
                          queryResult, errorString);
            }

            return false;
        }

    }
}