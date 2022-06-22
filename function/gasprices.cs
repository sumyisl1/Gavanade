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

            int zipcode = Int32.Parse(req.Query["zipcode"]);

            client.DefaultRequestHeaders.Add("x-ms-client-id", "46f031d2-20fb-4ca3-b4f3-217ceeaa9d1a");
            var response = await client.GetAsync(
                $"https://atlas.microsoft.com/search/address/json?api-version=1.0&query={zipcode}&countrySet=US&subscription-key=QtkUpdIsoJvs1Di_m2zLOIe_lPCTQEdGUHnZZCTIQuU"
            );
            string responseMessage = await response.Content.ReadAsStringAsync();

            List<string> states = new List<string>();
            List<double> prices = new List<double>();

            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            path = path.Substring(0, path.IndexOf("bin"));
            using (var reader = new StreamReader(path + "state-gas-price-averages.csv"))
            {
                reader.ReadLine();
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    string[] values = line.Split(',');

                    states.Add(values[0]);
                    prices.Add(Double.Parse(values[1]));
                }
            }

            string csn = "countrySubdivisionName";
            int index = responseMessage.IndexOf(csn);
            string state;
            double price = -1;
            if (index > 0)
            {
                state = responseMessage.Substring(index + csn.Length + 3);
                state = state.Substring(0, state.IndexOf("\""));
                if (states.Contains(state))
                {
                    price = prices[states.IndexOf(state)];
                }
                responseMessage = $"{price}";
            }
            else
            {
                responseMessage = "Error: no state found for zip code";
            }
            return new OkObjectResult(responseMessage);
        }
    }
}
