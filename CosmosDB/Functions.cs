using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Cosmos;
using System.Linq;
using Microsoft.Azure.Cosmos.Linq;

namespace CosmosDB
{
    public class Functions
    {
        public class UserDto
        {
            [JsonProperty(PropertyName = "id")]
            public string Id { get; set; }

            public string Name { get; set; }
        }

        private readonly CosmosClient _cosmosClient;

        public Functions(CosmosClient cosmosClient)
        {
            _cosmosClient = cosmosClient;
        }

        [FunctionName("AddUser")]
        public async Task<IActionResult> AddUser(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation("AddUser HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            string key = new Random().Next(1, 1000000).ToString();

            if (string.IsNullOrWhiteSpace(name))
            {
                name = key;
            }

            Database db = _cosmosClient.GetDatabase("db");
            Container container = db.GetContainer("users");

            await container.CreateItemAsync(new UserDto { Id = key, Name = name });

            return new OkObjectResult($"Added {name}");
        }

        [FunctionName("GetUsersCount")]
        public async Task<IActionResult> GetUsersCount(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation("GetUsersCount HTTP trigger function processed a request.");

            Database db = _cosmosClient.GetDatabase("db");
            Container container = db.GetContainer("users");

            IQueryable<UserDto> q = container.GetItemLinqQueryable<UserDto>(false);

            var iterator = q.ToFeedIterator();
            int totalCount = 0;

            while (iterator.HasMoreResults)
            {
                var result = await iterator.ReadNextAsync();
                totalCount += result.Count();
            }

            return new OkObjectResult($"Users count {totalCount}");
        }
    }
}
