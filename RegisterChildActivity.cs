using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Views;
using Android.Widget;
using Android.Content.PM;
using Android.Gms.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Firestore;
using Java.Util;
using PinPointTrackAp.EventListeners;

namespace PinPointTrackAp
{
    [Activity(Theme = "@style/AppTheme", Label = "RegisterChildActivity")]
    public class RegisterChildActivity : Activity, BottomNavigationView.IOnNavigationItemSelectedListener
    {
        //form controls
        CoordinatorLayout cRootView;
        EditText uName;
        EditText uSurname;
        EditText uUsername;
        EditText uPassword;
        EditText uconfirmPassword;
        Button btnRegister;
        string UserEmail = "";

        //Firebase
        FirebaseAuth mAuth;
        FirebaseFirestore database;

        //Class instance
        TaskConfirmationListeners taskcomlistener = new TaskConfirmationListeners();
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.ActivityRegisterChild);
            // Create your application here

            //navigation listener
            BottomNavigationView navigation = FindViewById<BottomNavigationView>(Resource.Id.navigation);
            navigation.SetOnNavigationItemSelectedListener(this);
            IMenu m = navigation.Menu;
            IMenuItem i = m.GetItem(1);
            i.SetChecked(true);

            //initialise firebase
            InitializeFirebase();

            mAuth = FirebaseAuth.Instance;

            ConnectControl();

            //get email
            if (System.String.IsNullOrEmpty(UserEmail))
                UserEmail = Intent.Extras.GetString("Email");

        }

        public bool OnNavigationItemSelected(IMenuItem item)
        {
            //navigate between fragments
            Android.Support.V4.App.Fragment fragment = null;
            CoordinatorLayout rview = (CoordinatorLayout)FindViewById(Resource.Id.cRootLayout);

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
                    break;
            }

            return true;
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

        //set controls
        void ConnectControl()
        {
            //inputtext
            uName = (EditText)FindViewById(Resource.Id.txtPersonNameChild);
            uSurname = (EditText)FindViewById(Resource.Id.txtPersonSurnameChild);
            uUsername = (EditText)FindViewById(Resource.Id.txtUsernameChild);
            uPassword = (EditText)FindViewById(Resource.Id.txtPasswordChild);
            uconfirmPassword = (EditText)FindViewById(Resource.Id.txtconfirmPasswordChild);

            cRootView = (CoordinatorLayout)FindViewById(Resource.Id.cRootLayoutChild);
            //Button
            btnRegister = (Button)FindViewById(Resource.Id.btnRegisterChild);

            btnRegister.Click += BtnRegister_Click;
        }

        private void BtnRegister_Click(object sender, EventArgs e)
        {
            string name, surname, username, password, confirmpassword;
            name = uName.Text;
            surname = uSurname.Text;
            username = uUsername.Text;
            password = uPassword.Text;
            confirmpassword = uconfirmPassword.Text;
            if (name.Length < 3)
            {
                Snackbar.Make(cRootView, "Name can't be less than 3 characters", Snackbar.LengthShort).Show();
                return;
            }
            else if (surname.Length < 3)
            {
                Snackbar.Make(cRootView, "Surname can't be less than 3 characters", Snackbar.LengthShort).Show();
                return;
            }
            else if (!username.Contains('@'))
            {
                Snackbar.Make(cRootView, "Email not valid", Snackbar.LengthShort).Show();
                return;
            }
            else if (password.Length < 6)
            {
                Snackbar.Make(cRootView, "Password can't be less than 6 characters", Snackbar.LengthShort).Show();
                return;
            }
            else if (confirmpassword.Length < 6)
            {
                Snackbar.Make(cRootView, "confirm Password can't be less than 6 characters.", Snackbar.LengthShort).Show();
                return;
            }
            else if (!String.Equals(confirmpassword, password))
            {
                Snackbar.Make(cRootView, "Password do not match.", Snackbar.LengthShort).Show();
                return;
            }

            //add user
            RegisterUser(username, password);

            // Create Document
            HashMap Doc = new HashMap();
            Doc.Put("c_name", name);
            Doc.Put("c_surname", surname);
            Doc.Put("pg_Email", UserEmail);
            Doc.Put("c_Email", username);
            Doc.Put("c_lat", null);
            Doc.Put("c_long", null);
            Doc.Put("c_signal", null);

            DocumentReference docref = database.Collection("/Child").Document(username);
            docref.Set(Doc);

        }

        void RegisterUser(string username, string password)
        {
            taskcomlistener.success += Taskcomlistener_success;
            taskcomlistener.failure += Taskcomlistener_failure;
            mAuth.CreateUserWithEmailAndPassword(username, password)
                .AddOnSuccessListener(this, taskcomlistener)
                .AddOnFailureListener(this, taskcomlistener);
        }

        private void Taskcomlistener_failure(object sender, EventArgs e)
        {
            Snackbar.Make(cRootView, "Unable to register child", Snackbar.LengthShort).Show();
            return;
        }

        private void Taskcomlistener_success(object sender, EventArgs e)
        {
            Snackbar.Make(cRootView, "Child successfully registered", Snackbar.LengthShort).Show();
            uName.Text = "";
            uSurname.Text = "";
            uUsername.Text = "";
            uPassword.Text = "";
            uconfirmPassword.Text = "";
            return;
        }

    }
}