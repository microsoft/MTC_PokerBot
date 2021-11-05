using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Threading;

namespace HoldemFunctions
{
    public static class SleepySleepy
    {
        [FunctionName("SleepySleepy")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {

            string ms_string = req.Query["ms"];
            if (string.IsNullOrWhiteSpace(ms_string)) ms_string = "500";
            if(int.TryParse(ms_string, out int ms) == false)
            {
                return new BadRequestObjectResult($"Unable to parse ms value {ms_string}");
            }

            Thread.Sleep(ms);

            log.LogInformation($"Slept for {ms} miliseconds");

            return new OkObjectResult(ms);
        }
    }
}
