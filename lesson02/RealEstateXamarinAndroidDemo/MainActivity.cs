using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using Android.Graphics;
using System.Net;

namespace RealEstateXamarinAndroidDemo
{
    [Activity(Label = "RealEstateXamarinAndroidDemo", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            ConfigureMainLayout();
        }

        private async void ConfigureMainLayout()
        {
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);
            FindViewById<Button>(Resource.Id.buttonSearch).Click += SearchButton_Click;

        }

        private void SearchButton_Click(object sender, EventArgs e)
        {
            ConfigureListingsLayout();
        }

        private void ConfigureListingsLayout()
        {
            SetContentView(Resource.Layout.Listings);
            FindViewById<Button>(Resource.Id.btnListBack).Click += ButtonBack_Click;
            FindViewById<Button>(Resource.Id.btnListFilter).Click += ButtonFilter_Click;
        }

        private void ButtonBack_Click(object sender, EventArgs e)
        {
            // Go back to main layout
            ConfigureMainLayout();
        }

        private void ButtonFilter_Click(object sender, EventArgs e)
        {
            // Go to filter page
            ConfigureFilterLayout();
        }

        private void ConfigureFilterLayout()
        {
            SetContentView(Resource.Layout.Filter);

            Button filterApply = FindViewById<Button>(Resource.Id.filterApply);
            filterApply.Click += FilterApply_Click;
        }

        private void FilterApply_Click(object sender, EventArgs e)
        {
            // Go back to listsings
            ConfigureListingsLayout();
        }



    }
}

