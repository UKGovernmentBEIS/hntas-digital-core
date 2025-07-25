namespace HNTAS.Core.Api.Configuration
{
    public class DbSettings
    {
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
        public string UsersCollectionName { get; set; }
        public string OrgCountersCollectionName { get; set; }
    }
}
