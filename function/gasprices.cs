using System;
using System.Net.Http;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace gavanade.function
{
    public static class gasprices
    {
        private static readonly HttpClient client = new HttpClient();
        [FunctionName("gasprices")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Processing request for gas prices.");

            string result = "Error: unable to process request";
            string zipcode_str = req.Query["zipcode"];
            string latitude_str = req.Query["latitude"];
            string longitude_str = req.Query["longitude"];
            int zipcode = 0;
            double latitude = 0;
            double longitude = 0;

            // strings for compiling into a result
            string city = "", state = "", state_abbr = "";
            string areaprice = "", stateprice = "", nationalprice = "";

            int index = 0;
            string searchstring = "", responseMessage = "";

            // if the latitude is not null or empty try to parse
            if (!string.IsNullOrEmpty(latitude_str))
            {
                log.LogInformation("latitude passed");
                Double.TryParse(latitude_str, out latitude);
            }
            // if the longitude is not null or empty try to parse
            if (!string.IsNullOrEmpty(longitude_str))
            {
                log.LogInformation("longitude passed");
                Double.TryParse(longitude_str, out longitude);
            }

            // if given coordinates, convert them to zipcode
            if (latitude != 0 & longitude != 0)
            {
                // use api to get zipcode from coordiantes
                client.DefaultRequestHeaders.Add("x-ms-client-id", "46f031d2-20fb-4ca3-b4f3-217ceeaa9d1a");
                var response = await client.GetAsync(
                    $"https://atlas.microsoft.com/search/address/reverse/json?api-version=1.0&query={latitude},{longitude}&subscription-key=QtkUpdIsoJvs1Di_m2zLOIe_lPCTQEdGUHnZZCTIQuU"
                );
                responseMessage = await response.Content.ReadAsStringAsync();

                // search for zipcode in the response from api
                searchstring = "\"postalCode\":\"";
                index = responseMessage.IndexOf(searchstring) + searchstring.Length;
                zipcode_str = responseMessage.Substring(index);
                zipcode_str = zipcode_str.Substring(0, zipcode_str.IndexOf("\""));
            }
            else
            {
                // if no coordinates are given, query api with given zipcode instead
                client.DefaultRequestHeaders.Add("x-ms-client-id", "46f031d2-20fb-4ca3-b4f3-217ceeaa9d1a");
                var response = await client.GetAsync(
                    $"https://atlas.microsoft.com/search/address/json?api-version=1.0&query={zipcode_str}&countrySet=US&subscription-key=QtkUpdIsoJvs1Di_m2zLOIe_lPCTQEdGUHnZZCTIQuU"
                );
                responseMessage = await response.Content.ReadAsStringAsync();
            }

            // if the zipcode is not null or empty try to parse
            if (!string.IsNullOrEmpty(zipcode_str))
            {
                log.LogInformation("zipcode passed");
                Int32.TryParse(zipcode_str, out zipcode);
            }
            // if zipcode is still 0 return an error
            if (zipcode == 0)
            {
                return new OkObjectResult("Error with the query,0,0,0,0,0");
            }

            // search for state abbreviation in the response from api
            searchstring = "\"countrySubdivision\":\"";
            index = responseMessage.IndexOf(searchstring) + searchstring.Length;
            state_abbr = responseMessage.Substring(index);
            state_abbr = state_abbr.Substring(0, state_abbr.IndexOf("\""));

            // search for state in the response from api
            searchstring = "\"countrySubdivisionName\":\"";
            index = responseMessage.IndexOf(searchstring) + searchstring.Length;
            state = responseMessage.Substring(index);
            state = state.Substring(0, state.IndexOf("\""));

            // search for city in response from api
            searchstring = "\"municipality\":\"";
            index = responseMessage.IndexOf(searchstring) + searchstring.Length;
            city = responseMessage.Substring(index);
            city = city.Substring(0, city.IndexOf("\""));

            // if given a zipcode, get prices from gasbuddy
            if (zipcode != 0)
            {
                // webscrape the prices from gasbuddy
                var response = await client.GetAsync(
                    $"https://www.gasbuddy.com/home?search={zipcode}"
                );
                responseMessage = await response.Content.ReadAsStringAsync();

                // search for the average price in string returned from gasbuddy
                searchstring = "<span class=\"text__lg___1S7OO text__bold___1C6Z_ text__left___1iOw3 PriceTrends-module__priceHeader___fB9X9\">";

                index = responseMessage.IndexOf(searchstring) + searchstring.Length;
                string price = responseMessage.Substring(index);
                index = price.IndexOf(searchstring) + searchstring.Length;
                price = price.Substring(index);
                price = price.Substring(0, price.IndexOf("<"));
                areaprice = price;
            }

            // use state abbreviation to webscrape AAA for national and state averages
            if (!string.IsNullOrEmpty(state_abbr))
            {
                // webscrape the average prices from AAA
                var response = await client.GetAsync(
                    $"https://gasprices.aaa.com/?state={state_abbr}"
                );
                responseMessage = await response.Content.ReadAsStringAsync();

                // search for the national average in string returned from AAA
                searchstring = "AAA National Average ";
                index = responseMessage.IndexOf(searchstring) + searchstring.Length;
                nationalprice = responseMessage.Substring(index);
                nationalprice = nationalprice.Substring(0, nationalprice.IndexOf(" "));

                // search for the state average in string returned from AAA
                searchstring = $"AAA {state} Avg. ";
                index = responseMessage.IndexOf(searchstring) + searchstring.Length;
                stateprice = responseMessage.Substring(index);
                stateprice = stateprice.Substring(0, stateprice.IndexOf(" "));
            }


            // update result with zipcode, city, state and all prices
            result = $"{zipcode},{city},{state},{areaprice},{stateprice},{nationalprice}";
            return new OkObjectResult(result);
        }
    }
}
