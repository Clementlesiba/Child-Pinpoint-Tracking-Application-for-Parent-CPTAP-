using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Gms.Tasks;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Firestore;
using Java.Util;
using PinPointTrackAp.EventListeners;

namespace PinPointTrackAp
{
    [Activity(ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, ScreenOrientation = ScreenOrientation.FullUser)]
    public class RegisterActivity : AppCompatActivity
    {
        //form controls
        CoordinatorLayout cRootView;
        EditText uName;
        EditText uSurname;
        EditText uUsername;
        EditText uPassword;
        EditText uConfirmPassword;

        Button btnRegister;
        Button btnLogin;
        //Firebase
        FirebaseAuth mAuth;
        FirebaseFirestore database;

        //Class instance
        TaskConfirmationListeners taskcomlistener = new TaskConfirmationListeners();
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            //call the view
            SetContentView(Resource.Layout.Register);

            //initialise firebase
            InitializeFirebase();

            mAuth = FirebaseAuth.Instance;

            ConnectControl();

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
            uName = (EditText)FindViewById(Resource.Id.txtPersonName);
            uSurname = (EditText)FindViewById(Resource.Id.txtPersonSurname);
            uUsername = (EditText)FindViewById(Resource.Id.txtUsername);
            uPassword = (EditText)FindViewById(Resource.Id.txtPassword);
            uConfirmPassword = (EditText)FindViewById(Resource.Id.txtconfirmpassword);

            cRootView = (CoordinatorLayout)FindViewById(Resource.Id.cRootLayout);
            //Button
            btnRegister = (Button)FindViewById(Resource.Id.btnRegister);

            btnRegister.Click += BtnRegister_Click;

            btnLogin = (Button)FindViewById(Resource.Id.btnLoginHere);
            btnLogin.Click += BtnLogin_Click;
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            StartActivity(new Android.Content.Intent(this, typeof(LoginActivity)));
            //animation
            OverridePendingTransition(Resource.Animation.@Side_In_Right, Resource.Animation.@Side_In_Left);
        }

        private void BtnRegister_Click(object sender, EventArgs e)
        {
            string name, surname, username, password, confirmpassword;
            name = uName.Text;
            surname = uSurname.Text;
            username = uUsername.Text;
            password = uPassword.Text;
            confirmpassword = uConfirmPassword.Text;

            if (name.Length < 3)
            {
                Snackbar.Make(cRootView, "Name can't be less than 3 characters.", Snackbar.LengthShort).Show();
                return;
            }else if(surname.Length < 3){
                Snackbar.Make(cRootView, "Surname can't be less than 3 characters.", Snackbar.LengthShort).Show();
                return;
            }
            else if (!username.Contains('@'))
            {
                Snackbar.Make(cRootView, "Email not valid.", Snackbar.LengthShort).Show();
                return;
            }
            else if (password.Length < 6)
            {
                Snackbar.Make(cRootView, "Password can't be less than 6 characters.", Snackbar.LengthShort).Show();
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
            Doc.Put("name", name);
            Doc.Put("surname",surname);
            Doc.Put("Email", username);
            Doc.Put("l_lat", null);
            Doc.Put("l_long", null);
            Doc.Put("l_rD", null);

            DocumentReference docref = database.Collection("/User").Document(username);
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
            Snackbar.Make(cRootView, "Unable to register user", Snackbar.LengthShort).Show();
            return;
        }

        private void Taskcomlistener_success(object sender, EventArgs e)
        {
            //StartActivity(typeof(MainActivity));
            Snackbar.Make(cRootView, "User successfully registered", Snackbar.LengthShort).Show();
            uName.Text = "";
            uSurname.Text = "";
            uUsername.Text ="";
            uPassword.Text = "";
            uConfirmPassword.Text ="";
            return;
        }
    }
}