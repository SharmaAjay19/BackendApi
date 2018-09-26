using Microsoft.WindowsAzure.Storage.Table;

namespace BackendApi.Models{
    public class UserDataEntity: TableEntity {
        public UserDataEntity(string username, string rowid){
            this.PartitionKey = username;
            this.RowKey = rowid;
        }

        public UserDataEntity(){}
        public string areaName {get; set;}
        public string id {get; set;}
        public string polygon {get; set;}
        public string username {get; set;}
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

    public class ReturnUserProfileEntity: TableEntity {
        public string LastLogin {get; set;}
    }
}