using Android.App;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using Com.Androidquery;
using Com.Androidquery.Callback;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RealEstateXamarinAndroidDemo
{
    public class LoadListings : BaseAdapter
    {
        Activity _activity;
        List<ListListing> _listings;

        public LoadListings(Activity activity, List<ListListing> listings)
        {
            _activity = activity;
            _listings = listings;
        }

        public override int Count
        {
            get { return _listings.Count; }
        }

        public override Java.Lang.Object GetItem(int position)
        {
            return position;
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override Android.Views.View GetView(int position, Android.Views.View convertView, Android.Views.ViewGroup parent)
        {
            if (convertView == null)
            {
                convertView = _activity.LayoutInflater.Inflate(Resource.Layout.list_item, parent, false);
            }

            ListListing listing = _listings[position];

            AQuery aq = new AQuery(convertView);

            TextView txtProductName = convertView.FindViewById<TextView>(Resource.Id.txtProductName);
            txtProductName.Text = listing.MainText;

            TextView txtSubText = convertView.FindViewById<TextView>(Resource.Id.txtSubText);
            txtSubText.Text = listing.SubText;


            Bitmap imgLoading = aq.GetCachedImage(Resource.Drawable.img_loading);

            if (aq.ShouldDelay(position, convertView, parent, listing.ImageUrl))
            {
                ((AQuery)aq.Id(Resource.Id.imgProduct)).Image(imgLoading, 0.75f);
            }
            else
            {
                ((AQuery)aq.Id(Resource.Id.imgProduct)).Image(listing.ImageUrl, true, true, 0, 0, imgLoading, 0, 0.75f);
            }

            return convertView;
        }
    }
}
