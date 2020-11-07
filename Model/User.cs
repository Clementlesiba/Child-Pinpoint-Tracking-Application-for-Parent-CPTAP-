using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace PinPointTrackAp.Model
{
    class User
    {
        public string Email { get; set; }
        public double Lat { get; set; }
        public double Long { get; set; }
        public double rD { get; set; }
    }
}