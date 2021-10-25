namespace GuruOps.ES.LinqToDsl.DAL
{
    public interface IElasticSearchSettings
    {
        public string EndpointUris { get; set; }
        public ConnectionPoolType ConnectionPoolType { get; set; }
        public int RequestTimeout { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public int RetryCount { get; set; }
        public int PageSize { get; set; }
    }

    public enum ConnectionPoolType
    {
        Single = 0,
        Static = 1
    }
}