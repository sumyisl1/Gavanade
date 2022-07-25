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

            // request which table to run via query
            string table = req.Query["table"];
            string write = req.Query["write"];

            string result = "";
            string responseMessage = "";
            string searchString = "";
            string price = "";
            string query = "";

            // credentials for accessing sql database
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
            builder.DataSource = "gavanade-db-server.database.windows.net";
            builder.UserID = "gavanade";
            builder.Password = "cdL6YEvc5uzZsmw3eyShiiLpb85UGpvn";
            builder.InitialCatalog = "gavanade-db";

            int index = 0;
            // create list with all national and state prices
            List<string> prices = new List<string>();

            int tableNum = 0;
            // pastPrices table = 1
            if (table == "pastPrices")
            {
                tableNum = 2;
            }
            // contactInformation table = 2
            else if (table == "contactInformation")
            {
                tableNum = 4;
            }
            // concerns table = 2
            else if (table == "concerns")
            {
                tableNum = 6;
            }

            if (write == "write")
            {
                tableNum++;
            }
            // if unknown table was given, ERROR CHECKING
            if (tableNum == 0)
            {
                // TODO error checking
            }
            else
            {
                switch (tableNum)
                {
                    // if reading from pastPrices
                    case 2:
                        using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                        {
                            connection.Open();

                            // query which state to recieve gasprice from database
                            string state = req.Query["state"];
                            state = state.Replace(" ", "_");
                            string statePr = "";
                            string nationalPr = "";
                            string prevDate = "";

                            // make query to read from sql database
                            query = $"SELECT [dateUpdated], [US], [{state}] FROM dbo.pastPrices";
                            using (SqlCommand command = new SqlCommand(query, connection))
                            {
                                log.LogInformation("1");
                                // execute sql query
                                SqlDataReader dataReader = command.ExecuteReader();
                                // read sql query response
                                dataReader.Read();
                                log.LogInformation("2");
                                statePr += dataReader.GetValue(2);
                                nationalPr += dataReader.GetValue(1);
                                prevDate += dataReader.GetValue(0);

                                log.LogInformation("3");
                                // if state or national price ends in a zero, add those trailing zeros
                                while (statePr.Length < 4)
                                {
                                    statePr += "0";
                                }
                                while (nationalPr.Length < 4)
                                {
                                    nationalPr += "0";
                                }


                                log.LogInformation("4");
                                // change prevDate format from "7/24/2022 12:00:01 AM" to "7/24/2022"
                                prevDate = prevDate.Substring(0, prevDate.IndexOf(" "));

                                // set result to {state price}`{national price}`{prevDate}
                                result += statePr + "`" + nationalPr + "`" + prevDate;
                                log.LogInformation("5");
                            }
                            connection.Close();
                        }

                        log.LogInformation(result);
                        return new OkObjectResult(result);

                    // if writing to pastPrices
                    case 3:
                        log.LogInformation("Processing past prices.");

                        // webscrape state averages page from AAA
                        var response = await client.GetAsync(
                            "https://gasprices.aaa.com/state-gas-price-averages/"
                        );
                        responseMessage = await response.Content.ReadAsStringAsync();

                        // search for the national average in string returned from AAA
                        searchString = "Today’s AAA National Average $";
                        index = responseMessage.IndexOf(searchString) + searchString.Length;

                        // if searchstring not found, return an error
                        if (index <= 0)
                        {
                            log.LogInformation("Failed to find search string");
                        }
                        // if searchstring found, add national price to price string
                        else
                        {
                            price = responseMessage.Substring(index);
                            price = price.Substring(0, price.IndexOf(" ") - 1);
                            // add national price to prices list
                            prices.Add(price);
                        }

                        // loop through AAA response and find each state average
                        for (int i = 0; i < 51; i++)
                        {
                            // search for the state average in string returned from AAA
                            searchString = "<td class=\"regular\" style=\"display: table-cell;\">$";
                            index = responseMessage.IndexOf(searchString) + searchString.Length;

                            // if searchstring not found, return an error
                            if (index <= 0)
                            {
                                log.LogInformation("Failed to find search string");
                                continue;
                            }
                            price = responseMessage.Substring(index);
                            price = price.Substring(0, price.IndexOf(" ") - 1);
                            // add each price to the prices list
                            prices.Add(price);
                            responseMessage = responseMessage.Substring(index);
                        }

                        log.LogInformation($"{prices.Count}");
                        using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                        {
                            connection.Open();

                            // delete the previous prices from database
                            query = "DELETE FROM [dbo].[pastPrices]";
                            using (SqlCommand command = new SqlCommand(query, connection))
                            {
                                command.ExecuteNonQuery();
                            }

                            // insert new prices into the database and the date it was added
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
                    // if writing to contactInformation
                    case 5:
                        using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                        {
                            // sql query for inserting contact info
                            query = $"INSERT INTO [dbo].[contactInformation] VALUES (@firstName, @lastName, @email)";
                            using (SqlCommand command = new SqlCommand(query, connection))
                            {
                                // assign parameters from query into variables
                                string firstName = req.Query["firstName"];
                                string lastName = req.Query["lastName"];
                                string email = req.Query["email"];

                                // safely add variable values into the sql query
                                command.Parameters.AddWithValue("@firstName", firstName);
                                command.Parameters.AddWithValue("@lastName", lastName);
                                command.Parameters.AddWithValue("@email", email);

                                // execute sql query
                                connection.Open();
                                command.ExecuteNonQuery();
                                connection.Close();
                            }
                        }
                        break;
                    // if reading from concerns
                    case 6:
                        log.LogInformation("Processing concerns.");
                        break;
                    // if writing to concerns
                    case 7:
                        using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                        {
                            // sql query for inserting concerns
                            query = $"INSERT INTO [dbo].[concerns] VALUES (@email, @msg)";
                            using (SqlCommand command = new SqlCommand(query, connection))
                            {
                                // assign parameters from query into variables
                                string email = req.Query["email"];
                                string msg = req.Query["msg"];

                                // safely add variable values into the sql query
                                command.Parameters.AddWithValue("@email", email);
                                command.Parameters.AddWithValue("@msg", msg);

                                // execute sql query
                                connection.Open();
                                command.ExecuteNonQuery();
                                connection.Close();
                            }
                        }
                        break;
                    // if unknown table was given return an error
                    default:
                        log.LogInformation("error");
                        break;
                }

            }
            return new OkObjectResult(result);
        }
    }
}
