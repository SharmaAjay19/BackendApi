using Microsoft.WindowsAzure.Storage.Table;

namespace BackendApi.Models{
    public class UserDataEntity: TableEntity {
        public UserDataEntity(string username, string rowid){
            this.PartitionKey = username;
            this.RowKey = rowid;
        }

        public UserDataEntity(){}
        public string DataCol1 {get; set;}
        public string DataCol2 {get; set;}
    }

    public class UserProfileEntity: TableEntity {
        public UserProfileEntity(string username){
            this.PartitionKey = "UserProfile";
            this.RowKey = username;
        }

        public UserProfileEntity(){}
        public string Password {get; set;}
        public string LastLogin {get; set;}
    }
}