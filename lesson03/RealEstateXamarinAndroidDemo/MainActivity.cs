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
        private static Uri _serviceUri;
        private static HttpClient _httpClient;

        protected async override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            ConfigureMainLayout();

            // Initialize the http client
            _serviceUri = new Uri("https://" + SearchServiceName + ".search.windows.net");
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("api-key", AzureSearchApiKey);

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

