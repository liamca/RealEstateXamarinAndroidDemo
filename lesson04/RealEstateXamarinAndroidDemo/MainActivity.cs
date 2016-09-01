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

namespace RealEstateXamarinAndroidDemo
{
    [Activity(Label = "RealEstateXamarinAndroidDemo", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        // Below is a query key as well as a sample search service that you are free to use for demo or learning purposes
        public static string AzureSearchApiKey = "F1F01D9405EF2EE6188891C7298D7390";
        public static string SearchServiceName = "realestate";
        public static string AzureSuggestUrl = "/indexes/listings/docs/suggest?suggesterName=sg&$top=15&search=";
        public static string AzureSearchUrl = "/indexes/listings/docs?search=";
        private static Uri _serviceUri;
        private static HttpClient _httpClient;

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

            currentPage = 0;

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


        private void SearchButton_Click(object sender, EventArgs e)
        {
            AutoCompleteTextView SearchText = FindViewById<AutoCompleteTextView>(Resource.Id.autoCompleteSearchTextView);
            ExecListingsSearch(SearchText.Text);
        }

        private int ExecListingsSearch(string q = null, bool countOnly = false)
        {
            // Execute a search using the supplied text and apply the results to a new listings list page
            string url = _serviceUri + AzureSearchUrl + "*";
            if (q != "")
            {
                url = _serviceUri + AzureSearchUrl + q;
                StoredSearchQuery = q;
            }
            // Append the count request
            url += "&$count=true";
            url += "&$top=10";

            // Set the page
            url += "&$skip=" + currentPage*10;

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


        private void ConfigureListingsLayout()
        {
            SetContentView(Resource.Layout.Listings);
            FindViewById<Button>(Resource.Id.btnListBack).Click += ButtonBack_Click;
            FindViewById<Button>(Resource.Id.btnListFilter).Click += ButtonFilter_Click;

            ListView lvListings = FindViewById<ListView>(Resource.Id.lvListings);
            lvListings.Adapter = new LoadListings(this, ListingsForList);

            SeekBar seekBar = FindViewById<SeekBar>(Resource.Id.seekBarPaging);
            seekBar.StopTrackingTouch += SeekBar_StopTrackingTouch;
            seekBar.ProgressChanged += SeekBar_ProgressChanged;
            seekBar.Progress = currentPage;


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

            Button filterApply = FindViewById<Button>(Resource.Id.filterApply);
            filterApply.Click += FilterApply_Click;
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

