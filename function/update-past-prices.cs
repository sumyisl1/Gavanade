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
    public static class update_past_prices
    {
        private static readonly HttpClient client = new HttpClient();
        [FunctionName("update_past_prices")]
        public static void Run([TimerTrigger("0 0 0 * * 0")] TimerInfo myTimer, ILogger log)
        {
            // if timer is late
            if (myTimer.IsPastDue)
            {
                log.LogInformation("Timer is running late!");
            }

            // url query to update the database prices using AAA
            client.GetAsync(
                "https://gavanade-function-windows.azurewebsites.net/api/database?table=pastPrices&write=write"
            );

            // log timer is on time
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
        }
    }
}
