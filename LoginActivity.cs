using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Firebase;
using Firebase.Auth;
using PinPointTrackAp.EventListeners;

namespace PinPointTrackAp
{
    [Activity(ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, ScreenOrientation = ScreenOrientation.FullUser)]
    public class LoginActivity : AppCompatActivity
    {
        //controls
        EditText user_name;
        EditText user_password;
        Button btnLoginUser;
        Button btnRegister;

        CoordinatorLayout cRootViewLogin;
        FirebaseAuth mAuth;


        //hide keyboard
        HideKeyBoardClass hdKey = new HideKeyBoardClass();
        
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            //call the view
            SetContentView(Resource.Layout.Login);
            InitializeFirebase();
            //getting values
            user_name = (EditText)FindViewById(Resource.Id.txtLoginUsername);
            user_password = (EditText)FindViewById(Resource.Id.txtLoginPassword);
            //layout
            cRootViewLogin = (CoordinatorLayout)FindViewById(Resource.Id.cRootLayoutLogin);
            //button
            btnLoginUser = (Button)FindViewById(Resource.Id.btnLogin);
            btnLoginUser.Click += BtnLoginUser_Click;

            //if you can't login register
            btnRegister = (Button)FindViewById(Resource.Id.btnRegisterHere);
            btnRegister.Click += BtnRegister_Click;
        }

        private void BtnRegister_Click(object sender, EventArgs e)
        {
            StartActivity(new Android.Content.Intent(this, typeof(RegisterActivity)));
            //animation
            OverridePendingTransition(Resource.Animation.@Side_In_Right, Resource.Animation.@Side_In_Left);
        }

        private void BtnLoginUser_Click(object sender, EventArgs e)
        {
            string email, password;
            email = user_name.Text;
            password = user_password.Text;

            if (!email.Contains('@'))
            {
                Snackbar.Make(cRootViewLogin, "Email not valid", Snackbar.LengthShort).Show();
                return;
            }
            else if (password.Length < 6)
            {
                Snackbar.Make(cRootViewLogin, "Password can't be less than 6 characters", Snackbar.LengthShort).Show();
                return;
            }

            //closing keyboard
            hdKey.CloseKeyboard();

            //Class instance
            TaskConfirmationListeners tasklistener = new TaskConfirmationListeners();
            tasklistener.success += Taskcomlistener_successLogin;
            tasklistener.failure += Taskcomlistener_failureLogin;
            mAuth.SignInWithEmailAndPassword(email, password)
              .AddOnSuccessListener(this, tasklistener)
              .AddOnFailureListener(this, tasklistener);
        }

        private void Taskcomlistener_successLogin(object sender, EventArgs e)
        {
            Intent main = new Intent(this, typeof(MainActivity));
            main.PutExtra("Email", user_name.Text.ToString());
            StartActivity(main);
        }

        private void Taskcomlistener_failureLogin(object sender, EventArgs e)
        {
            Snackbar.Make(cRootViewLogin, "Login Failed!", Snackbar.LengthShort).Show();
            return;
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
                mAuth = FirebaseAuth.Instance;
            }
            else
            {
                mAuth = FirebaseAuth.Instance;
            }
        }

    }
}