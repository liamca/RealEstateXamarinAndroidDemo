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
using System.Net.Http;

namespace RealEstateXamarinAndroidDemo
{

    public class Search
    {
        private static readonly Uri _serviceUri;
        private static HttpClient _httpClient;
        private static string SearchServiceName = "realestate";
        private static string SearchServiceApiKey = "96BE40C474500CE1B8FCAB22EB5C6A93";

        static Search()
        {
            try
            {
                // Initialize the httpClient that will be reused as needed
                _serviceUri = new Uri("https://" + SearchServiceName + ".search.windows.net");
                _httpClient = new HttpClient();
                _httpClient.DefaultRequestHeaders.Add("api-key", SearchServiceApiKey);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public dynamic ExecQuery(string url)
        {
            Uri uri = new Uri(url);
            HttpResponseMessage response = AzureSearchHelper.SendSearchRequest(_httpClient, HttpMethod.Get, uri);
            AzureSearchHelper.EnsureSuccessfulSearchResponse(response);

            return AzureSearchHelper.DeserializeJson<dynamic>(response.Content.ReadAsStringAsync().Result);
        }

        private string EscapeODataString(string s)
        {
            return Uri.EscapeDataString(s).Replace("\'", "\'\'");
        }
    }
}