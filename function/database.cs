using System;
using System.Net.Http;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Data.SqlClient;

namespace gavanade.function
{
    public static class database
    {
        private static readonly HttpClient client = new HttpClient();
        [FunctionName("database")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Processing request for database.");

            string table = req.Query["table"];
            string result = "";
            string responseMessage = "";
            string searchString = "";
            string price = "";

            string query = "";

            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
            builder.DataSource = "gavanade-db-server.database.windows.net";
            builder.UserID = "gavanade";
            builder.Password = "cdL6YEvc5uzZsmw3eyShiiLpb85UGpvn";
            builder.InitialCatalog = "gavanade-db";

            int index = 0;
            List<string> prices = new List<string>();

            int tableNum = 0;
            if (table == "pastPrices")
            {
                tableNum = 1;
            }
            else if (table == "contactInformation")
            {
                tableNum = 2;
            }
            else if (table == "concerns")
            {
                tableNum = 3;
            }


            if (tableNum == 0)
            {
                // TODO error checking
            }
            else
            {
                switch (tableNum)
                {
                    case 1:
                        log.LogInformation("Processing past prices.");

                        var response = await client.GetAsync(
                            "https://gasprices.aaa.com/state-gas-price-averages/"
                        );
                        responseMessage = await response.Content.ReadAsStringAsync();

                        searchString = "Today’s AAA National Average $";
                        index = responseMessage.IndexOf(searchString) + searchString.Length;
                        if (index <= 0)
                        {
                            log.LogInformation("Failed to find search string");
                        }
                        else
                        {
                            price = responseMessage.Substring(index);
                            price = price.Substring(0, price.IndexOf(" ") - 1);
                            prices.Add(price);
                        }

                        // search for the average price in string returned from AAA
                        for (int i = 0; i < 51; i++)
                        {
                            searchString = "<td class=\"regular\" style=\"display: table-cell;\">$";
                            index = responseMessage.IndexOf(searchString) + searchString.Length;

                            if (index <= 0)
                            {
                                log.LogInformation("Failed to find search string");
                                continue;
                            }
                            price = responseMessage.Substring(index);
                            price = price.Substring(0, price.IndexOf(" ") - 1);
                            prices.Add(price);
                            responseMessage = responseMessage.Substring(index);
                        }

                        log.LogInformation($"{prices.Count}");
                        using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                        {
                            connection.Open();

                            query = "DELETE FROM [dbo].[pastPrices]";
                            using (SqlCommand command = new SqlCommand(query, connection))
                            {
                                command.ExecuteNonQuery();
                            }

                            query = $"INSERT INTO [dbo].[pastPrices] VALUES ('{DateTime.Now}'";
                            foreach (string p in prices)
                            {
                                query += $",{p}";
                            }
                            query += ");";
                            log.LogInformation(query);

                            using (SqlCommand command = new SqlCommand(query, connection))
                            {
                                command.ExecuteNonQuery();
                            }
                            connection.Close();
                        }


                        break;
                    case 2:
                        log.LogInformation("Processing contact information.");
                        break;
                    case 3:
                        log.LogInformation("Processing concerns.");
                        break;
                    default:
                        log.LogInformation("error");
                        break;
                }

            }
            return new OkObjectResult(result);
        }
    }
}
