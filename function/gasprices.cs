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

            if (!string.IsNullOrEmpty(latitude_str))
            {
                log.LogInformation("latitude passed");
                Double.TryParse(latitude_str, out latitude);
            }
            if (!string.IsNullOrEmpty(longitude_str))
            {
                log.LogInformation("longitude passed");
                Double.TryParse(longitude_str, out longitude);
            }

            if (latitude != 0 & longitude != 0)
            {
                client.DefaultRequestHeaders.Add("x-ms-client-id", "46f031d2-20fb-4ca3-b4f3-217ceeaa9d1a");
                var response = await client.GetAsync(
                    $"https://atlas.microsoft.com/search/address/reverse/json?api-version=1.0&query={latitude},{longitude}&subscription-key=QtkUpdIsoJvs1Di_m2zLOIe_lPCTQEdGUHnZZCTIQuU"
                );
                string responseMessage = await response.Content.ReadAsStringAsync();
                string searchstring = "\"postalCode\":\"";
                int index = responseMessage.IndexOf(searchstring) + searchstring.Length;
                zipcode_str = responseMessage.Substring(index);
                zipcode_str = zipcode_str.Substring(0, zipcode_str.IndexOf("\""));
            }

            if (!string.IsNullOrEmpty(zipcode_str))
            {
                log.LogInformation("zipcode passed");
                Int32.TryParse(zipcode_str, out zipcode);
            }

            if (zipcode != 0)
            {
                var response = await client.GetAsync(
                    $"https://www.gasbuddy.com/home?search={zipcode}"
                );
                string responseMessage = await response.Content.ReadAsStringAsync();

                string searchstring = "<span class=\"text__lg___1S7OO text__bold___1C6Z_ text__left___1iOw3 PriceTrends-module__priceHeader___fB9X9\">";

                int index = responseMessage.IndexOf(searchstring) + searchstring.Length;
                string price = responseMessage.Substring(index);
                index = price.IndexOf(searchstring) + searchstring.Length;
                price = price.Substring(index);
                price = price.Substring(0, price.IndexOf("<"));
                result = price;
            }

            return new OkObjectResult(result);
        }
    }
}
