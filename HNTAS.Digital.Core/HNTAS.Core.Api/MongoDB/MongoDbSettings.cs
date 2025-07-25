namespace HNTAS.Core.Api.MongoDB
{
    public class MongoDbSettings
    {
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
        public string UsersCollectionName { get; set; }
        public string OrgCountersCollectionName { get; set; }
    }
}
