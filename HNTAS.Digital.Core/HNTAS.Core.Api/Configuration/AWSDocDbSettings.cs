namespace HNTAS.Core.Api.Configuration
{
    public class AWSDocDbSettings
    {
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
        public string UsersCollectionName { get; set; }
        public string CountersCollectionName { get; set; }
        public string HeatNetworksCollectionName { get; set; }
    }
}
