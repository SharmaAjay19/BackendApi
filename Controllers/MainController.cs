using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.SqlClient;
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
        SqlConnection sqlConnection;
        public MainController(){
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
            builder.DataSource = "__DataSource__"; 
            builder.UserID = "__UserID__";            
            builder.Password = "__Password__";     
            builder.InitialCatalog = "areamarket";
            sqlConnection = new SqlConnection(builder.ConnectionString);
            storageAccount = new CloudStorageAccount(
        new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials(
            "ajsharmstorage", "__StorageAccountKey__"), true);
            tableClient = storageAccount.CreateCloudTableClient();
        }


        // GET api/values/5
        [HttpGet("/FetchUserData/{username}")]
        public IActionResult FetchUserData(string username)
        {
            var result = getAllEntitiesInPartition<UserDataEntity>("UserData", username).GetAwaiter().GetResult();
            return new ObjectResult(result);
        }

        [HttpGet("/FetchUserDataSql/{username}")]
        public IActionResult FetchUserDataSql(string username)
        {
            var result = getEntitiesByUserFromSql(username);
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
                return NotFound("User not found");
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
        public IActionResult AddUserData([FromBody] UserDataEntity userData)
        {
            userData.PartitionKey = userData.username;
            userData.RowKey = userData.id;
            var result = addEntityToTable("UserData", userData).GetAwaiter().GetResult();
            return new ObjectResult(result.Result);
        }

        [HttpPost("/AddUserDataSql")]
        [ActionName("/AddUserDataSql")]
        public IActionResult AddUserDataSql([FromBody] UserDataEntity userData)
        {
            addEntityToSql(userData);
            return new OkObjectResult("Saved successfully");
        }

        [HttpGet("/DeleteUserDataSql/{id}")]
        [ActionName("/DeleteUserDataSql")]
        public IActionResult DeleteUserDataSql(string id)
        {
            deleteEntityFromSql(id);
            return new OkObjectResult("Deleted successfully");
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

        private void addEntityToSql(UserDataEntity entity){
            sqlConnection.Open();       
            String query = "EXEC dbo.AddAreaToMarket '" + entity.id + "', '" + entity.username + "', '" + entity.areaName + "', '" + entity.polygon + "'";
            SqlCommand command = new SqlCommand(query, sqlConnection);
            command.ExecuteNonQueryAsync();                 
        }

        private void deleteEntityFromSql(string id){
            sqlConnection.Open();
            String query = "EXEC dbo.DeleteArea '" + id + "'";
            SqlCommand command = new SqlCommand(query, sqlConnection);
            command.ExecuteNonQueryAsync();
        }

        private object getEntitiesByUserFromSql(string username){
            sqlConnection.Open();
            String query = "EXEC dbo.GetAreasByUser '" + username + "'";
            SqlCommand command = new SqlCommand(query, sqlConnection);
            SqlDataReader reader = command.ExecuteReader();
            String data = "";
            while (reader.Read())
                data = reader.GetString(0);
            return JsonConvert.DeserializeObject(data);
        }
    }
}
