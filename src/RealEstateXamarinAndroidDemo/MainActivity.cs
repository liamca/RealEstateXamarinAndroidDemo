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
using System.Net.Http;
using Plugin.Geolocator;

namespace RealEstateXamarinAndroidDemo
{
    [Activity(Label = "RealEstateXamarinAndroidDemo", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        // Below is a query key as well as a sample search service that you are free to use for demo or learning purposes
        public static string AzureSearchApiKey = "F1F01D9405EF2EE6188891C7298D7390";
        public static string SearchServiceName = "realestate";
        public static string AzureSuggestUrl = "/indexes/listings/docs/suggest?suggesterName=sg&$top=15&search=";
        public static string AzureLookupUrl = "/indexes/listings/docs/";
        public static string AzureSearchUrl = "/indexes/listings/docs?search=";
        private static Uri _serviceUri;
        private static HttpClient _httpClient;

        public static int filterMinBeds;
        public static int filterMaxBeds;
        public static int filterMinBaths;
        public static int filterMaxBaths;
        public static bool filterShowForSale;
        public static bool filterShowPending;
        public static bool filterShowSold;

        public static List<ListListing> ListingsForList;
        public static string StoredSearchQuery;

        public static int maxPage;
        public static int currentPage;

        protected async override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            ConfigureMainLayout();

            // Initialize the http client
            _serviceUri = new Uri("https://" + SearchServiceName + ".search.windows.net");
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("api-key", AzureSearchApiKey);

            // Reset the filters
            filterMinBeds = 0;
            filterMaxBeds = 6;
            filterMinBaths = 0;
            filterMaxBaths = 6;
            filterShowForSale = true;
            filterShowPending = true;
            filterShowSold = true;

            //Perform a test suggest to load HTTP client
            await ExecSuggest("seattle");
        }

        private async void ConfigureMainLayout()
        {
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);
            FindViewById<Button>(Resource.Id.buttonSearch).Click += SearchButton_Click;

            AutoCompleteTextView SearchText = FindViewById<AutoCompleteTextView>(Resource.Id.autoCompleteSearchTextView);
            SearchText.TextChanged += SearchText_TextChanged;
            SearchText.AfterTextChanged += SearchText_AfterTextChanged;


        }

        private void SearchButton_Click(object sender, EventArgs e)
        {
            // Execute the search using the provided search query
            AutoCompleteTextView SearchText = FindViewById<AutoCompleteTextView>(Resource.Id.autoCompleteSearchTextView);
            ExecListingsSearch(SearchText.Text);
        }

        private async Task<int> ExecListingsSearch(string q = null, bool countOnly = false)
        {
            // Execute a search using the supplied text and apply the results to a new listings list page
            string url = _serviceUri + AzureSearchUrl + "*";

            if (q != "")
                url = _serviceUri + AzureSearchUrl + q;

            // Append the count request
            url += "&$count=true";
            url += "&$top=10";

            // Set the page
            url += "&$skip=" + currentPage * 10;

            // Append the filter
            url += "&$filter=beds ge " + filterMinBeds + " and beds le " + filterMaxBeds +
                " and baths ge " + filterMinBaths + " and baths le " + filterMaxBaths;

            // Determing the status filter
            string statusFilter = string.Empty;
            if ((filterShowForSale) && (filterShowPending) && (filterShowSold))
                statusFilter = " and (status eq 'active' or status eq 'pending' or status eq 'sold')";
            else if (!(filterShowForSale) && (filterShowPending) && (filterShowSold))
                statusFilter = " and (status eq 'pending' or status eq 'sold')";
            else if ((filterShowForSale) && !(filterShowPending) && (filterShowSold))
                statusFilter = " and (status eq 'active' or status eq 'sold')";
            else if ((filterShowForSale) && (filterShowPending) && !(filterShowSold))
                statusFilter = " and (status eq 'active' or status eq 'pending')";
            else if ((filterShowForSale) && !(filterShowPending) && !(filterShowSold))
                statusFilter = " and (status eq 'active')";
            else if (!(filterShowForSale) && !(filterShowPending) && (filterShowSold))
                statusFilter = " and (status eq 'sold')";
            else if (!(filterShowForSale) && (filterShowPending) && !(filterShowSold))
                statusFilter = " and (status eq 'pending')";
            else
                statusFilter = " and status eq 'noresults'";
            url += statusFilter;

            // Get the users location
            var locator = CrossGeolocator.Current;
            locator.DesiredAccuracy = 100; //100 is new default
            var position = await locator.GetPositionAsync(timeoutMilliseconds: 10000);
            double lat = position.Latitude;
            double lon = position.Longitude;
            url += "&scoringProfile=geoScoring&scoringParameter=currentLocation:" + lon.ToString() + "," + lat.ToString();

            Uri uri = new Uri(url);
            HttpResponseMessage response = AzureSearchHelper.SendSearchRequest(_httpClient, HttpMethod.Get, uri);
            AzureSearchHelper.EnsureSuccessfulSearchResponse(response);
            dynamic result = AzureSearchHelper.DeserializeJson<dynamic>(response.Content.ReadAsStringAsync().Result);

            JObject jsonObj = JObject.Parse(result.ToString());

            if (countOnly == false)
            {
                ListingsForList = new List<ListListing>(); ;

                foreach (var item in jsonObj["value"])
                {
                    ListListing ll = new ListListing();
                    ll.ImageUrl = item["thumbnail"].ToString();
                    string price = "$" + Convert.ToInt32(item["price"].ToString()).ToString("#,##0");
                    ll.MainText = price + " Beds: " + item["beds"].ToString() +
                        " Baths: " + item["baths"].ToString() +
                        " Sq Ft.: " + Convert.ToInt32(item["sqft"].ToString()).ToString("#,##0");

                    ll.Id = item["listingId"].ToString();
                    // build up the address
                    string address = string.Empty;
                    if (item["number"] != null)
                        address += item["number"].ToString() + " ";
                    if (item["street"] != null)
                        address += item["street"].ToString() + " ";
                    if (item["city"] != null)
                        address += item["city"].ToString() + " ";
                    if (item["region"] != null)
                        address += item["region"].ToString() + " ";

                    ll.SubText = address;
                    ListingsForList.Add(ll);
                }

                // Default to the listings list for search results
                ConfigureListingsLayout();
            }

            int docCount = Convert.ToInt32(jsonObj["@odata.count"].ToString());
            // Update the seek bar with the paging
            maxPage = Convert.ToInt32(docCount / 10) + 1;

            if (countOnly == false)
            {
                SeekBar seekBar = FindViewById<SeekBar>(Resource.Id.seekBarPaging);
                if (maxPage > 20)
                    seekBar.Max = 19;   // Base 0
                else
                    seekBar.Max = maxPage;
            }

            return Convert.ToInt32(docCount.ToString());
        }


        private void SearchText_AfterTextChanged(object sender, Android.Text.AfterTextChangedEventArgs e)
        {
            AutoCompleteTextView SearchText = FindViewById<AutoCompleteTextView>(Resource.Id.autoCompleteSearchTextView);
            if (!SearchText.IsPopupShowing && SearchText.Text.Length > 0)
            {
                SearchText.ShowDropDown();
            }
        }

        private async void SearchText_TextChanged(object sender, Android.Text.TextChangedEventArgs e)
        {
            // As the text changes call Azure Search /suggest to handle typeahead
            try
            {
                // Handle type ahead search by executing /suggest API call to Azure Search
                string q = e.Text.ToString();
                if (q.Length > 2)
                {
                    List<string> execSuggest = await ExecSuggest(q);
                    var autoCompleteOptions = execSuggest.ToArray();
                    ArrayAdapter autoCompleteAdapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleDropDownItem1Line, autoCompleteOptions);
                    AutoCompleteTextView SearchText = FindViewById<AutoCompleteTextView>(Resource.Id.autoCompleteSearchTextView);
                    SearchText.Adapter = autoCompleteAdapter;
                }
            }
            catch
            {
            }
        }


        
        private void ConfigureListingsLayout()
        {
            SetContentView(Resource.Layout.Listings);
            FindViewById<Button>(Resource.Id.btnListBack).Click += ButtonBack_Click;
            FindViewById<Button>(Resource.Id.btnListFilter).Click += ButtonFilter_Click;

            ListView lvListings = FindViewById<ListView>(Resource.Id.lvListings);
            lvListings.Adapter = new LoadListings(this, ListingsForList);
            lvListings.ItemClick += LvListings_ItemClick;

            SeekBar seekBar = FindViewById<SeekBar>(Resource.Id.seekBarPaging);
            seekBar.StopTrackingTouch += SeekBar_StopTrackingTouch;
            seekBar.ProgressChanged += SeekBar_ProgressChanged;
            seekBar.Progress = currentPage;

        }

        private void LvListings_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            // User clicked on an item so grab id an pass to the detail page
            ListListing ll = ListingsForList[Convert.ToInt32(e.Id)];
            ConfigureDetailsLayout(ll.Id);
        }

        private void ConfigureDetailsLayout(string listingId)
        {
            SetContentView(Resource.Layout.Details);

            // Do an Azure Search doc lookup using the specified id
            string url = _serviceUri + AzureLookupUrl + listingId;

            Uri uri = new Uri(url);
            HttpResponseMessage response = AzureSearchHelper.SendSearchRequest(_httpClient, HttpMethod.Get, uri);
            AzureSearchHelper.EnsureSuccessfulSearchResponse(response);
            dynamic result = AzureSearchHelper.DeserializeJson<dynamic>(response.Content.ReadAsStringAsync().Result);

            JObject json = JObject.Parse(result.ToString());

            if (json != null)
            {
                TextView txtPrice = FindViewById<TextView>(Resource.Id.textDetailsPrice);
                txtPrice.Text = "$" + Convert.ToInt32(json["price"].ToString()).ToString("#,##0"); ;

                TextView txtAddress = FindViewById<TextView>(Resource.Id.textDetailsAddress);
                string address = string.Empty;
                if (json["number"] != null)
                    address += json["number"].ToString() + " ";
                if (json["street"] != null)
                    address += json["street"].ToString() + " ";
                if (json["city"] != null)
                    address += json["city"].ToString() + " ";
                if (json["region"] != null)
                    address += json["region"].ToString().ToUpper() + " ";
                txtAddress.Text = address;

                ImageView imgListing = FindViewById<ImageView>(Resource.Id.imageDetailListing);
                var imageBitmap = GetImageBitmapFromUrl(json["thumbnail"].ToString());
                imgListing.SetImageBitmap(imageBitmap);

                TextView txtDetails = FindViewById<TextView>(Resource.Id.textDetailsDetails);
                txtDetails.Text = " Beds: " + json["beds"].ToString() +
                    " Baths: " + json["baths"].ToString() +
                    " Sq Ft.: " + Convert.ToInt32(json["sqft"].ToString()).ToString("#,##0");

                TextView txtStatus = FindViewById<TextView>(Resource.Id.textDetailsStatus);
                txtStatus.Text = "Status: " + json["status"].ToString();

                TextView txtDescription = FindViewById<TextView>(Resource.Id.textDetailsDescription);
                txtDescription.Text = json["description"].ToString();

                TextView txtListingId = FindViewById<TextView>(Resource.Id.textDetailsListingId);
                txtListingId.Text = "Listing ID: " + json["listingId"].ToString();


            }

            Button buttonBackToList = FindViewById<Button>(Resource.Id.btnMainToList);
            buttonBackToList.Click += ButtonBackToList_Click;


        }

        private Bitmap GetImageBitmapFromUrl(string url)
        {
            Bitmap imageBitmap = null;

            using (var webClient = new WebClient())
            {
                var imageBytes = webClient.DownloadData(url);
                if (imageBytes != null && imageBytes.Length > 0)
                {
                    imageBitmap = BitmapFactory.DecodeByteArray(imageBytes, 0, imageBytes.Length);
                }
            }

            return imageBitmap;
        }

        private void ButtonBackToList_Click(object sender, EventArgs e)
        {
            // Go back and refresh the listings
            ExecListingsSearch();
        }


        private void SeekBar_ProgressChanged(object sender, SeekBar.ProgressChangedEventArgs e)
        {
            TextView seekBarText = FindViewById<TextView>(Resource.Id.textPaging);
            seekBarText.Text = "Page " + (Convert.ToInt32(e.SeekBar.Progress) + 1).ToString();
        }

        private void SeekBar_StopTrackingTouch(object sender, SeekBar.StopTrackingTouchEventArgs e)
        {
            currentPage = e.SeekBar.Progress;
            ExecListingsSearch();
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

            FindViewById<EditText>(Resource.Id.filterBedsMin).Text = filterMinBeds.ToString();
            FindViewById<EditText>(Resource.Id.filterBedsMax).Text = filterMaxBeds.ToString();
            FindViewById<EditText>(Resource.Id.filterBathsMin).Text = filterMinBaths.ToString();
            FindViewById<EditText>(Resource.Id.filterBedsMax).Text = filterMaxBaths.ToString();
            FindViewById<CheckBox>(Resource.Id.filterForSale).Checked = filterShowForSale;
            FindViewById<CheckBox>(Resource.Id.filterPending).Checked = filterShowPending;
            FindViewById<CheckBox>(Resource.Id.filterSold).Checked = filterShowSold;

            // Handle updates
            FindViewById<EditText>(Resource.Id.filterBedsMin).TextChanged += FilterActivity_TextChanged;
            FindViewById<EditText>(Resource.Id.filterBedsMax).TextChanged += FilterActivity_TextChanged;
            FindViewById<EditText>(Resource.Id.filterBathsMin).TextChanged += FilterActivity_TextChanged;
            FindViewById<EditText>(Resource.Id.filterBedsMax).TextChanged += FilterActivity_TextChanged;
            FindViewById<CheckBox>(Resource.Id.filterForSale).CheckedChange += FilterActivity_CheckedChange;
            FindViewById<CheckBox>(Resource.Id.filterPending).CheckedChange += FilterActivity_CheckedChange;
            FindViewById<CheckBox>(Resource.Id.filterSold).CheckedChange += FilterActivity_CheckedChange;


            Button filterApply = FindViewById<Button>(Resource.Id.filterApply);
            filterApply.Click += FilterApply_Click;
        }

        private void FilterActivity_TextChanged(object sender, Android.Text.TextChangedEventArgs e)
        {
            // Store updated filter changes 
            try
            {
                filterMinBeds = Convert.ToInt32(FindViewById<EditText>(Resource.Id.filterBedsMin).Text);
                filterMaxBeds = Convert.ToInt32(FindViewById<EditText>(Resource.Id.filterBedsMax).Text);
                filterMinBaths = Convert.ToInt32(FindViewById<EditText>(Resource.Id.filterBathsMin).Text);
                filterMaxBaths = Convert.ToInt32(FindViewById<EditText>(Resource.Id.filterBathsMax).Text);
                FindViewById<TextView>(Resource.Id.filterTotalListings).Text = "Total Listings: " + ExecListingsSearch(countOnly: true);
            }
            catch
            {
                // likely they hit delete and it is empty
            }
        }

        private void FilterActivity_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            // Store updated filter changes 
            filterShowForSale = FindViewById<CheckBox>(Resource.Id.filterForSale).Checked;
            filterShowPending = FindViewById<CheckBox>(Resource.Id.filterPending).Checked;
            filterShowSold = FindViewById<CheckBox>(Resource.Id.filterSold).Checked;
            FindViewById<TextView>(Resource.Id.filterTotalListings).Text = "Total Listings: " + ExecListingsSearch(countOnly: true);
        }

        private void FilterApply_Click(object sender, EventArgs e)
        {
            // Go back to listsings
            ConfigureListingsLayout();
        }

        private async Task<List<string>> ExecSuggest(string q)
        {
            // Execute /suggest API call to Azure Search and parse results
            string url = _serviceUri + AzureSuggestUrl + q;

            Uri uri = new Uri(url);
            HttpResponseMessage response = AzureSearchHelper.SendSearchRequest(_httpClient, HttpMethod.Get, uri);
            AzureSearchHelper.EnsureSuccessfulSearchResponse(response);
            dynamic result = AzureSearchHelper.DeserializeJson<dynamic>(response.Content.ReadAsStringAsync().Result);

            JObject jsonObj = JObject.Parse(result.ToString());
            List<string> Suggestions = new List<string>();
            // Iterate through the results to get the suggestions
            foreach (var item in jsonObj["value"])
            {
                Suggestions.Add(item["@search.text"].ToString());
            }
            // Limit to unique suggestions and sort
            return Suggestions.Distinct().OrderBy(x => x).ToList();
        }

    }
}

