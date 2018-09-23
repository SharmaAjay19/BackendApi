using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using BackendApi.Models;

namespace BackendApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MainController : ControllerBase
    {
        CloudStorageAccount storageAccount;
        CloudTableClient tableClient;

        public MainController(){
            storageAccount = new CloudStorageAccount(
        new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials(
            "ajsharmstorage", "XY2JE3zk3zUV1wRzMoWemOJ1SUbIJoMFJG8yiX79clU9//2VS0Bxg7G6e/Hx4XOWWORqKSfk/H64Okai8sTftg=="), true);
            tableClient = storageAccount.CreateCloudTableClient();
        }


        // GET api/values/5
        [HttpGet("/FetchUserData/{username}")]
        public IActionResult FetchUserData(string username)
        {
            var result = getAllEntitiesInPartition<UserDataEntity>("UserData", username).GetAwaiter().GetResult();
            return new ObjectResult(result);
        }

        // POST api/values
        [HttpPost("/UserLogin")]
        [ActionName("/UserLogin")]
        public IActionResult UserLogin([FromBody] dynamic login_body)
        {
            UserProfileEntity userProfile = null;
            try{
                userProfile = getEntityById<UserProfileEntity>("UserProfiles", "UserProfile", login_body.username.ToString()).GetAwaiter().GetResult();
            }
            catch{
                userProfile = null;
            }
            if (userProfile == null)
                return new NoContentResult();
            if (userProfile.Password == login_body.password.ToString()){
                return new ObjectResult(JsonConvert.DeserializeObject<ReturnUserProfileEntity>(JsonConvert.SerializeObject(userProfile)));
            }
            return BadRequest("Incorrect Password");
        }

        [HttpPost("/UserRegister")]
        [ActionName("/UserRegister")]
        public async Task<IActionResult> UserRegister([FromBody] dynamic register_body)
        {
            UserProfileEntity userProfile = new UserProfileEntity(register_body.username.ToString());
            userProfile.Password = register_body.password.ToString();
            userProfile.LastLogin = DateTime.Now.ToString("yyyyMMddHHmmss");
            try{
                await addEntityToTable("UserProfiles", userProfile);
                return new OkObjectResult("Registered Successfully");
            }
            catch{
                return BadRequest("Username is taken");
            }
        }

        [HttpPost("/AddUserData")]
        [ActionName("/AddUserData")]
        public IActionResult AddUserData([FromBody] dynamic user_data_body)
        {
            UserDataEntity userData = new UserDataEntity(user_data_body.username.ToString(), Guid.NewGuid().ToString());
            userData.DataCol1 = user_data_body.DataCol1.ToString();
            userData.DataCol2 = user_data_body.DataCol2.ToString();
            var result = addEntityToTable("UserData", userData).GetAwaiter().GetResult();
            return new ObjectResult(result.Result);
        }

        private async Task<List<TableEntity>> getAllEntitiesInPartition<T>(string tablename, string partitionkey)
        where T : TableEntity, new()
        {
            List<TableEntity> results = new List<TableEntity>();
            TableQuery<T> query = new TableQuery<T>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionkey));
            // Print the fields for each customer.
            TableContinuationToken token = null;
            CloudTable cloudTable = tableClient.GetTableReference(tablename);
            do
            {
                TableQuerySegment<T> resultSegment = await cloudTable.ExecuteQuerySegmentedAsync(query, token);
                token = resultSegment.ContinuationToken;

                foreach (TableEntity entity in resultSegment.Results)
                {
                    results.Add(entity);
                }
            } while (token != null);
            return results;
        }

        private async Task<TableResult> addEntityToTable(string tablename, TableEntity entity){
            TableOperation insertOperation = TableOperation.Insert(entity);
            CloudTable cloudTable = tableClient.GetTableReference(tablename);
            // Execute the insert operation.
            return await cloudTable.ExecuteAsync(insertOperation);
        }

        private async Task<TableEntity> getEntityById<T>(string tablename, string partitionkey, string id)
        where T : TableEntity, new()
        {
            TableOperation retrieveOperation = TableOperation.Retrieve<T>(partitionkey, id);
            // Execute the retrieve operation.
            CloudTable cloudTable = tableClient.GetTableReference(tablename);
            TableResult retrievedResult = await cloudTable.ExecuteAsync(retrieveOperation);
            // Print the phone number of the result.
            if (retrievedResult.Result != null)
                return (T)retrievedResult.Result;
            else
                return null;
        }
    }
}
