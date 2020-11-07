using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Firebase;
using Firebase.Firestore;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using What3Words;
using What3Words.Models;
using What3Words.Enums;
using System;
using Xamarin.Essentials;
using Android.Views.InputMethods;
using Android.Graphics.Drawables;
using Android.Content;
using Android.Support.V4.Content;
using Android.Graphics;
using Android.Gms.Common.Apis;
using Android.Gms.Location;
using Android.Gms.Common;
using Android.Util;
using Android;
using System.Threading.Tasks;
using Android.Support.V4.App;
using System.Timers;
using Java.Lang;
using Firebase.Auth;
using Firebase.Database;
using PinPointTrackAp.EventListeners;
using Android.Gms.Tasks;
using Android.Preferences;

namespace PinPointTrackAp
{
    public enum TimerType
    {
        POMODORO,
        SHORTBREAK,
        LONGBREAK
    }
    public enum TimerState
    {
        RUNNING,
        STOPPED
    }
    [Activity(Theme = "@style/AppTheme", ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, ScreenOrientation = ScreenOrientation.FullUser)]
    public class MainActivity : AppCompatActivity, BottomNavigationView.IOnNavigationItemSelectedListener,IOnMapReadyCallback, GoogleApiClient.IConnectionCallbacks, GoogleApiClient.IOnConnectionFailedListener, IOnSuccessListener
    {
        //Global Variables
        private GoogleMap GMap;
        private string w3waddress = "";
        private MarkerOptions optionsChild = null;

        //coordinates -25.710077, 28.263949
        int sigCount = 0;
        double Lat = 0;
        double Long = 0;
        double lRadius = 0;
        double latitudeDB = 0;
        double longitudeDB  = 0;
        double cLatitude, cLongitude;
        string UserEmail = "";
        string cName = "";
        int Out = 0;
        int countcheck = 0;
        GoogleApiClient api;

        //Radius colors
        int rAlpha = 100;
        int rRed = 100;
        int rGreen = 250;
        int rBlue = 100;
        

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
        
        CoordinatorLayout cRootView;
        //********************************************

        // Timer
        private Timer _timer = new Timer();
        private int _shortBreak = 300;
        private int _longBreak = 900;
        private int _pomodroTime = 1500;
        private int _countseconds = 1500;
        private int _longBreakInterval = 3;
        private int _currentPomodoro = 1;
        private int _totalBreak = 1;
        private TimerType _currentTimer = TimerType.POMODORO;
        private TimerState _timerState = TimerState.STOPPED;

        //Notification
        // Unique ID for our notification: 
        int NOTIFICATION_ID = 1000;
        static readonly string CHANNEL_ID = "location_notification";
        internal static readonly string COUNT_KEY = "count";

        FirebaseAuth mAuth;
        FirebaseFirestore database;

        Button btnNotify;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);



            btnNotify = FindViewById<Button>(Resource.Id.btnNotifyParent);
            btnNotify.Click += BtnNotify_Click;

            btnNotify.Visibility = ViewStates.Gone;

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


            //*******************************
            cRootView = (CoordinatorLayout)FindViewById(Resource.Id.cRootLayout);
            // Set our view from the "main" layout resource
            isGooglePlayServicesInstalled = IsGooglePlayServicesInstalled();
         
            locationRequest = new LocationRequest()
                               .SetPriority(LocationRequest.PriorityHighAccuracy)
                               .SetInterval(FIVE_MINUTES)
                               .SetFastestInterval(TWO_MINUTES);
            locationCallback = new FusedLocationProviderCallback(this);

            fusedLocationProviderClient = LocationServices.GetFusedLocationProviderClient(this);

            //========================================

            //initialise firebase
            InitializeFirebase();

            mAuth = FirebaseAuth.Instance;

            ////sharing data between interfaces
            //ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(this);
            //ISharedPreferencesEditor editor = prefs.Edit();
            //editor.PutInt("keyNotification", 0);
            //editor.Apply();

            //timer
            _timer.Interval = 10000;
            _timer.Elapsed += TimerElapsedEvent;

            if (_timerState == TimerState.STOPPED)
            {
                _timer.Start();
                _timerState = TimerState.RUNNING;
            }

            //=========================================

            api = new GoogleApiClient.Builder(Application.Context, this, this)
             .AddApi(Android.Gms.Location.LocationServices.API)
                .Build();

            //get email
            if (System.String.IsNullOrEmpty(UserEmail))
            {
                //ISharedPreferences p = PreferenceManager.GetDefaultSharedPreferences(this);
                //int v = p.GetInt("keyNotification",0);

                //if (v == 0)
                //{
                    if (Intent.Extras.GetString("Email") != null)
                    {
                        UserEmail = Intent.Extras.GetString("Email");
                    }
               // }

            }
  

            //Get secibd currrent Location
            GetLastLocation();
            RequestLocationUpdates();

            //get user geofencing
            database.Collection("User").WhereEqualTo("Email", UserEmail).Get().AddOnSuccessListener(this);


            //child location
            database.Collection("Child").WhereEqualTo("pg_Email", UserEmail).Get()
                .AddOnSuccessListener(this);


            //child location
            database.Collection("Child").WhereEqualTo("c_Email", UserEmail).Get()
                .AddOnSuccessListener(this);


            //get map
            SetUpMap();

            //navigation listener
            BottomNavigationView navigation = FindViewById<BottomNavigationView>(Resource.Id.navigation);
            navigation.SetOnNavigationItemSelectedListener(this);
            IMenu m = navigation.Menu;
            IMenuItem i = m.GetItem(0);
            i.SetChecked(true);

        }

        private void BtnNotify_Click(object sender, EventArgs e)
        {
            //Update child signal
            DocumentReference docRef = database.Collection("Child").Document(UserEmail);
            docRef.Update("c_signal", 1);

            Snackbar.Make(cRootView, "Alert sent", Snackbar.LengthShort).Show();
        }

        void InitializeFirebase()
        {
            FirebaseApp app = FirebaseApp.InitializeApp(this);

            if (app == null)
            {
                var options = new FirebaseOptions.Builder()
                    .SetProjectId("woven-fountain-276508")
                    .SetApplicationId("woven-fountain-276508")
                    .SetApiKey("AIzaSyAFLyDdGyzqeZkYWw87MErSaoRA6grdWcE")
                    .SetDatabaseUrl("https://woven-fountain-276508.firebaseio.com")
                    .SetStorageBucket("woven-fountain-276508.appspot.com")
                    .Build();

                app = FirebaseApp.InitializeApp(this, options);
                database = FirebaseFirestore.GetInstance(app);
            }
            else
            {
                database = FirebaseFirestore.GetInstance(app);
            }
        }

        //******************************************************************************
        private void TimerElapsedEvent(object sender, ElapsedEventArgs e)
        {
            _countseconds--;
            RunOnUiThread(SetTimerBackground);
             
            if (_countseconds == 0)
            {
                _timer.Stop();
                GetNextTimer();
                _timer.Start();
            }
        }
        private void GetNextTimer()
        {
            switch (_currentTimer)
            {
                case TimerType.POMODORO:
                    _currentPomodoro++;
                    if ((_totalBreak % _longBreakInterval) == 0)
                    {
                        _currentTimer = TimerType.LONGBREAK;
                        _countseconds = _longBreak;
                    }
                    else
                    {
                        _currentTimer = TimerType.SHORTBREAK;
                        _countseconds = _shortBreak;
                    }
                    break;
                case TimerType.SHORTBREAK:
                    _totalBreak++;
                    _currentTimer = TimerType.POMODORO;
                    _countseconds = _pomodroTime;
                    break;
                case TimerType.LONGBREAK:
                    _totalBreak++;
                    _currentTimer = TimerType.POMODORO;
                    _countseconds = _pomodroTime;
                    break;
            }
            RunOnUiThread(SetTimerBackground);
        }
        private void SetTimerBackground()
        {
            //Get Location
            GetLastLocation();
            RequestLocationUpdates();

            //Update Userlocation
            DocumentReference docRef = database.Collection("Child").Document(UserEmail);
            docRef.Update("c_lat", cLatitude);
            docRef.Update("c_long", cLongitude);

        }

        //**************************************************************************
        //Map function
        private void SetUpMap()
        {
            SupportMapFragment _mapFragment = (SupportMapFragment)SupportFragmentManager.FindFragmentByTag("mapFragment");
            if (_mapFragment == null)
            {
                GoogleMapOptions mapOptions = new GoogleMapOptions()
                    .InvokeRotateGesturesEnabled(true)
                    .InvokeScrollGesturesEnabled(true)
                    .InvokeCompassEnabled(true)
                    .InvokeAmbientEnabled(true)
                    .InvokeMapType(GoogleMap.MapTypeHybrid)
                    .InvokeZoomControlsEnabled(true)
                    .InvokeCompassEnabled(true);

                _mapFragment = SupportMapFragment.NewInstance(mapOptions);

                Android.Support.V4.App.FragmentTransaction fragTx = SupportFragmentManager.BeginTransaction();
                fragTx.Add(Resource.Id.googlemap, _mapFragment, "googlemap");
                fragTx.Commit();

                _mapFragment.GetMapAsync(this);


            }
        }
        public async void OnMapReady(GoogleMap googleMap)
        {
            //Geocoding
            try
            {
                //if no radius set then get current location
                if(Lat == 0 && Long == 0)
                {
                    Lat = cLatitude;
                    Long = cLongitude;
                }

                //Get what3words address
                What3WordsService w3s = new What3WordsService("YCS8X9K7", LanguageCode.EN);

                //Convert coordinates to what3words address
                GeocodingRoot result = await w3s.GetReverseGeocodingAsync(Convert.ToDouble(Lat), Convert.ToDouble(Long));

                w3waddress = result.Words;

                //Set marker
                this.GMap = googleMap;

                GMap.UiSettings.ZoomControlsEnabled = true;

                //getLocation
                

                LatLng latlng = new LatLng(Convert.ToDouble(Lat), Convert.ToDouble(Long));
                CameraUpdate camera = CameraUpdateFactory.NewLatLngZoom(latlng, 19);
                GMap.MoveCamera(camera);

                MarkerOptions options = new MarkerOptions().SetPosition(latlng).SetTitle(w3waddress).SetIcon(bitmapDescriptorFromVector(this, Resource.Drawable.what3wordsmarker));
                GMap.AddMarker(options);

                CircleOptions circleOptions = new CircleOptions()
                .InvokeCenter(options.Position)
                .InvokeStrokeColor(Color.Argb(50, 70, 70, 70))
                .InvokeFillColor(Color.Argb(rAlpha, rRed, rGreen, rBlue))
                .InvokeRadius(lRadius);
                 GMap.AddCircle(circleOptions);

                //Chiild location

                if (latitudeDB != 0 && longitudeDB != 0)
                {
                    //Convert coordinates to what3words address
                    GeocodingRoot result1 = await w3s.GetReverseGeocodingAsync(Convert.ToDouble(latitudeDB), Convert.ToDouble(longitudeDB));

                    w3waddress = result1.Words;

                    LatLng latlngChild = new LatLng(Convert.ToDouble(latitudeDB), Convert.ToDouble(longitudeDB));
                    optionsChild = new MarkerOptions().SetPosition(latlngChild).SetTitle("Name:"+cName+" : Address:"+ w3waddress).SetIcon(bitmapDescriptorFromVector(this, Resource.Drawable.Child_7));
                    GMap.AddMarker(optionsChild);

                    Log.Info("User location", "Child Found");

                    //Snackbar.Make(cRootView, latitudeDB.ToString() + "--Child location--" + longitudeDB.ToString() , Snackbar.LengthLong).Show();
                }

            }
            catch (FeatureNotSupportedException fnsEx)
            {
                // Feature not supported on device
                Toast.MakeText(ApplicationContext, fnsEx.Message, ToastLength.Long).Show();
            }
            catch (NullReferenceException e)
            {
                // null exception
                Toast.MakeText(ApplicationContext, "Wrong What3Words Address", ToastLength.Long).Show();

            }
            catch (Java.Lang.Exception ex)
            {
                // Handle exception that may have occurred in geocoding
                Toast.MakeText(ApplicationContext, ex.Message, ToastLength.Long).Show();
            }

        }
        //-------------------

        public bool OnNavigationItemSelected(IMenuItem item)
        {

            switch (item.ItemId)
            {
                case Resource.Id.navigation_monitor:
                    Intent main = new Intent(this, typeof(MainActivity));
                    main.PutExtra("Email", UserEmail);
                    StartActivity(main);
                    //get map
                    SetUpMap();

                    //animation
                    OverridePendingTransition(Resource.Animation.@Side_In_Right, Resource.Animation.@Side_In_Left);

                    break;
                case Resource.Id.navigation_addchild:
                    Intent Register = new Intent(this, typeof(RegisterChildActivity));
                    Register.PutExtra("Email", UserEmail);
                    Out = 1;
                    StartActivity(Register);
                    //animation
                    OverridePendingTransition(Resource.Animation.@Side_In_Right, Resource.Animation.@Side_In_Left);
                    break;
                case Resource.Id.navigation_geofencing:
                    Intent Geo = new Intent(this, typeof(SeGeofencingActivity));
                    Geo.PutExtra("Email", UserEmail);
                    Out = 1;
                    StartActivity(Geo);
                    //animation
                    OverridePendingTransition(Resource.Animation.@Side_In_Right, Resource.Animation.@Side_In_Left);
                    break;
            }

            return true;
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
        private BitmapDescriptor bitmapDescriptorFromVector(Context context, int vectorResId)
        {
            Drawable vectorDrawable = ContextCompat.GetDrawable(context, vectorResId);
            vectorDrawable.SetBounds(0, 0, vectorDrawable.IntrinsicWidth, vectorDrawable.IntrinsicHeight);
            Bitmap bitmap = Bitmap.CreateBitmap(vectorDrawable.IntrinsicWidth, vectorDrawable.IntrinsicHeight, Bitmap.Config.Argb8888);
            Canvas canvas = new Canvas(bitmap);
            vectorDrawable.Draw(canvas);
            return BitmapDescriptorFactory.FromBitmap(bitmap);
        }

        public void OnConnected(Bundle connectionHint)
        {
            throw new NotImplementedException();
        }

        public void OnConnectionSuspended(int cause)
        {
            throw new NotImplementedException();
        }

        public void OnConnectionFailed(ConnectionResult result)
        {
            throw new NotImplementedException();
        }

        //**************************** fused ****************************************************************************
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

            database.Collection("Child").WhereEqualTo("pg_Email", UserEmail).Get()
                .AddOnSuccessListener(this);

        }

        //Get data location
        public void OnSuccess(Java.Lang.Object result)
        {
            var snapshot = (QuerySnapshot)result;

            if (!snapshot.IsEmpty)
            {
                var docs = snapshot.Documents;
                foreach (DocumentSnapshot doc in docs)
                {

                    if (doc.Get("l_lat") != null && doc.Get("l_long") != null && doc.Get("l_rD") != null)
                    {
                        if (!System.String.IsNullOrEmpty(doc.Get("l_lat").ToString()) && !System.String.IsNullOrEmpty(doc.Get("l_long").ToString()) && !System.String.IsNullOrEmpty(doc.Get("l_rD").ToString()))
                        {
                            Lat = Convert.ToDouble(doc.Get("l_lat"));
                            Long = Convert.ToDouble(doc.Get("l_long"));
                            lRadius = Convert.ToDouble(doc.Get("l_rD"));

                            // Handle exception that may have occurred in geocoding
                            // Snackbar.Make(cRootView, Lat.ToString() + "--DB--" + Long.ToString() + "=" + lRadius.ToString(), Snackbar.LengthLong).Show();
                        }

                        btnNotify.Visibility = ViewStates.Gone;
                    }
                    else
                    {
                        btnNotify.Visibility = ViewStates.Gone;
                    }

                    if (doc.Get("c_lat") != null && doc.Get("c_long") != null)
                    {

                        if (!System.String.IsNullOrEmpty(doc.Get("c_lat").ToString()) && !System.String.IsNullOrEmpty(doc.Get("c_long").ToString()))
                        {
                            latitudeDB = Convert.ToDouble(doc.Get("c_lat"));
                            longitudeDB = Convert.ToDouble(doc.Get("c_long"));

                            
                            cName = doc.Get("c_name").ToString();

                            //get distance
                            double userDistance = GetDistance(latitudeDB, longitudeDB, Lat, Long);

                            bool check = false;
                            //Check if child not far
                            if ((userDistance > lRadius) && (lRadius > 0))
                            {
                                //Sendnotification
                                CreateNotificationChannel();
                                SendNotification("Boundary Notification", "Child is out of the boundry");

                                rRed = 250; rGreen = 100;

                                if (Out == 0)
                                {
                                    //update map
                                    SetUpMap();
                                }

                                check = true;
                            }


                            if (doc.Get("pg_Email").ToString().ToLower() == UserEmail.ToLower())
                            {
                                if (doc.Get("c_signal") != null)
                                {
                                    if (!System.String.IsNullOrEmpty(doc.Get("c_signal").ToString()))
                                    {

                                        if ((int)doc.Get("c_signal") == 1)
                                        {
                                            //Sendnotification
                                            NOTIFICATION_ID = 2000;
                                            CreateNotificationChannel();
                                            SendNotification("Alert", "Child sent safety alert");
                                            NOTIFICATION_ID = 1000;
                                        }
                                    }
                                }
                            }

                            //refresh the map  for last time.
                            if (countcheck == 0)
                            {
                                if (check == false)
                                {
                                    //update map
                                    SetUpMap();
                                    countcheck++;
                                }
                            }

                            // Handle exception that may have occurred in geocoding
                            //  Snackbar.Make(cRootView, latitudeDB.ToString() + "--DB--" + longitudeDB.ToString() + "=" + userDistance.ToString(), Snackbar.LengthLong).Show();
                        }

                        if (doc.Get("c_Email") != null)
                        {

                            if (!System.String.IsNullOrEmpty(doc.Get("c_Email").ToString()))
                            {
                                if (doc.Get("c_Email").ToString().ToLower() == UserEmail.ToLower())
                                {
                                    btnNotify.Visibility = ViewStates.Visible;
                                }
                            }
                        }

                    }
                }

            }
            Log.Info("Data", "Data Retrieved");
        }

        //Calculate distance
        public double GetDistance(double longitude, double latitude, double otherLongitude, double otherLatitude)
        {
            var d1 = latitude * (System.Math.PI / 180.0);
            var num1 = longitude * (System.Math.PI / 180.0);
            var d2 = otherLatitude * (System.Math.PI / 180.0);
            var num2 = otherLongitude * (System.Math.PI / 180.0) - num1;
            var d3 = System.Math.Pow(System.Math.Sin((d2 - d1) / 2.0), 2.0) + System.Math.Cos(d1) * System.Math.Cos(d2) * System.Math.Pow(System.Math.Sin(num2 / 2.0), 2.0);

            return 6376500.0 * (2.0 * System.Math.Atan2(System.Math.Sqrt(d3), System.Math.Sqrt(1.0 - d3)));
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

        async System.Threading.Tasks.Task GetLastLocationFromDevice()
        {
           

  
            var location = await fusedLocationProviderClient.GetLastLocationAsync();

            if (location == null)
            {
                cLongitude = 0;
                cLatitude = 0;
               
            }
            else
            {
                cLatitude = location.Latitude;
                cLongitude = location.Longitude;
            }
        }

        void RequestLocationPermission(int requestCode)
        {
            if (ActivityCompat.ShouldShowRequestPermissionRationale(this, Manifest.Permission.AccessFineLocation))
            {
                Snackbar.Make(cRootView, Resource.String.permission_location_rationale, Snackbar.LengthIndefinite)
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

        private async System.Threading.Tasks.Task StartRequestingLocationUpdates()
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

            //load map
            SetUpMap();
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

        //************************************************************************************************ 

        //=============  Notification ==================================

        public void SendNotification(string Name,string Message)
        {
            //sharing data between interfaces
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(this);
            ISharedPreferencesEditor editor = prefs.Edit();
            editor.PutInt("keyNotification",1);
            editor.Apply();        // applies changes asynchronously on newer APIs

            // When the user clicks the notification, SecondActivity will start up.
            var resultIntent = new Intent(this, typeof(MainActivity));
            resultIntent.AddFlags(ActivityFlags.ClearTop);
            resultIntent.PutExtra("Email", UserEmail);

            var pendingIntent = PendingIntent.GetActivity(this, 0, resultIntent, PendingIntentFlags.OneShot);


            // Construct a back stack for cross-task navigation:
            var stackBuilder = Android.Support.V4.App.TaskStackBuilder.Create(this);
            stackBuilder.AddParentStack(Class.FromType(typeof(MainActivity)));
            stackBuilder.AddNextIntent(resultIntent);

            // Create the PendingIntent with the back stack:            
            var resultPendingIntent = stackBuilder.GetPendingIntent(0, (int)PendingIntentFlags.UpdateCurrent);

            // Build the notification:
            var builder = new NotificationCompat.Builder(this, CHANNEL_ID)
                          .SetAutoCancel(true) // Dismiss the notification from the notification area when the user clicks on it
                          .SetContentIntent(resultPendingIntent) // Start up this activity when the user clicks the intent.
                          .SetContentTitle(Name) // Set the title
                          .SetSmallIcon(Resource.Drawable.what3wordsmarker) // This is the icon to display
                          .SetContentText(Message); // the message to display.

            // Finally, publish the notification:
            var notificationManager = NotificationManagerCompat.From(this);
            notificationManager.Notify(NOTIFICATION_ID, builder.Build());

        }
        void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            {
                // Notification channels are new in API 26 (and not a part of the
                // support library). There is no need to create a notification 
                // channel on older versions of Android.
                return;
            }

            var name = Resources.GetString(Resource.String.channel_name);
            var description = GetString(Resource.String.channel_description);
            var channel = new NotificationChannel(CHANNEL_ID, name, NotificationImportance.Default)
            {
                Description = description
            };

            var notificationManager = (NotificationManager)GetSystemService(NotificationService);
            notificationManager.CreateNotificationChannel(channel);
        }
    }
}

