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

            // variables for parsing request
            string result = "";
            string zipcodeStr = req.Query["zipcode"];
            string latitudeStr = req.Query["latitude"];
            string longitudeStr = req.Query["longitude"];
            int zipcode = 0;
            double latitude = 0;
            double longitude = 0;

            // strings for compiling into a result
            string city = "", state = "", stateAbbr = "";
            string areaPrice = "", statePrice = "", nationalPrice = "";

            // variables for internal use
            int index = 0;
            string searchString = "";
            string responseMessage = "";
            string numResults = "";
            string countryCode = "";

            // if the latitude is not null or empty try to parse
            if (!string.IsNullOrEmpty(latitudeStr))
            {
                log.LogInformation("latitude passed");
                Double.TryParse(latitudeStr, out latitude);
            }
            // if the longitude is not null or empty try to parse
            if (!string.IsNullOrEmpty(longitudeStr))
            {
                log.LogInformation("longitude passed");
                Double.TryParse(longitudeStr, out longitude);
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
                searchString = "\"countryCode\":\"";
                index = responseMessage.IndexOf(searchString) + searchString.Length;
                countryCode = responseMessage.Substring(index);
                countryCode = countryCode.Substring(0, countryCode.IndexOf("\""));

                // parse country code
                if (countryCode != "US")
                {
                    return new OkObjectResult("Error: outside US");
                }

                // search for zipcode in the response from api
                searchString = "\"postalCode\":\"";
                index = responseMessage.IndexOf(searchString) + searchString.Length;
                zipcodeStr = responseMessage.Substring(index);
                zipcodeStr = zipcodeStr.Substring(0, zipcodeStr.IndexOf("\""));
            }
            else
            {
                // add leading zeros to zipcode_str
                while (zipcodeStr.Length < 5)
                {
                    zipcodeStr = "0" + zipcodeStr;
                }

                // if no coordinates are given, query api with given zipcode instead
                client.DefaultRequestHeaders.Add("x-ms-client-id", "46f031d2-20fb-4ca3-b4f3-217ceeaa9d1a");
                var response = await client.GetAsync(
                    $"https://atlas.microsoft.com/search/address/json?api-version=1.0&query={zipcodeStr}&countrySet=US&subscription-key=QtkUpdIsoJvs1Di_m2zLOIe_lPCTQEdGUHnZZCTIQuU"
                );
                responseMessage = await response.Content.ReadAsStringAsync();

                // search for zipcode in the response from api
                searchString = "\"numResults\":";
                index = responseMessage.IndexOf(searchString) + searchString.Length;
                numResults = responseMessage.Substring(index);
                numResults = numResults.Substring(0, numResults.IndexOf(","));
                if (Int32.Parse(numResults) < 1)
                {
                    return new OkObjectResult("Error: invalid zipcode");
                }
            }

            // if the zipcode is not null or empty try to parse
            if (!string.IsNullOrEmpty(zipcodeStr))
            {
                log.LogInformation("zipcode passed");
                Int32.TryParse(zipcodeStr, out zipcode);
            }
            // if zipcode is still 0 return an error
            if (zipcode == 0)
            {
                return new OkObjectResult("Error: invalid query");
            }

            // search for state abbreviation in the response from api
            searchString = "\"countrySubdivision\":\"";
            index = responseMessage.IndexOf(searchString) + searchString.Length;
            stateAbbr = responseMessage.Substring(index);
            stateAbbr = stateAbbr.Substring(0, stateAbbr.IndexOf("\""));

            // search for state in the response from api
            searchString = "\"countrySubdivisionName\":\"";
            index = responseMessage.IndexOf(searchString) + searchString.Length;
            state = responseMessage.Substring(index);
            state = state.Substring(0, state.IndexOf("\""));

            // search for city in response from api
            searchString = "\"municipality\":\"";
            index = responseMessage.IndexOf(searchString) + searchString.Length;
            city = responseMessage.Substring(index);
            city = city.Substring(0, city.IndexOf("\""));

            // replace ", " with "/" in city string
            while (city.Contains(", "))
            {
                log.LogInformation(city);
                city = city.Substring(0, city.IndexOf(", ")) + "/" + city.Substring(city.IndexOf(", ") + 2);
            }

            // if given a zipcode, get prices from gasbuddy
            if (zipcode != 0)
            {
                // webscrape the prices from gasbuddy
                var response = await client.GetAsync(
                    $"https://www.gasbuddy.com/home?search={zipcodeStr}"
                );
                responseMessage = await response.Content.ReadAsStringAsync();

                // search for the average price in string returned from gasbuddy
                searchString = "<span class=\"text__lg___1S7OO text__bold___1C6Z_ text__left___1iOw3 PriceTrends-module__priceHeader___fB9X9\">";

                index = responseMessage.IndexOf(searchString) + searchString.Length;
                string price = responseMessage.Substring(index);
                index = price.IndexOf(searchString) + searchString.Length;
                price = price.Substring(index);
                price = price.Substring(0, price.IndexOf("<"));
                areaPrice = price;
            }

            // use state abbreviation to webscrape AAA for national and state averages
            if (!string.IsNullOrEmpty(stateAbbr))
            {
                // webscrape the average prices from AAA
                var response = await client.GetAsync(
                    $"https://gasprices.aaa.com/?state={stateAbbr}"
                );
                responseMessage = await response.Content.ReadAsStringAsync();

                // search for the national average in string returned from AAA
                searchString = "AAA National Average ";
                index = responseMessage.IndexOf(searchString) + searchString.Length;
                nationalPrice = responseMessage.Substring(index);
                nationalPrice = nationalPrice.Substring(0, nationalPrice.IndexOf(" ") - 1);

                // search for the state average in string returned from AAA
                searchString = $"AAA {state} Avg. ";
                index = responseMessage.IndexOf(searchString) + searchString.Length;
                statePrice = responseMessage.Substring(index);
                statePrice = statePrice.Substring(0, statePrice.IndexOf(" ") - 1);
            }

            // update result with zipcode, city, state and all prices including the prices of the most recent sunday
            var databaseResponse = await client.GetAsync(
                $"https://gavanade-function-windows.azurewebsites.net/api/database?table=pastPrices&state={state}"
            );
            responseMessage = await databaseResponse.Content.ReadAsStringAsync();
            if (responseMessage.Length < 1)
            {
                responseMessage = "``";
            }

            result = $"{zipcodeStr}`{city}`{state}`{areaPrice}`{statePrice}`{nationalPrice}`{responseMessage}";
            return new OkObjectResult(result);
        }
    }
}
