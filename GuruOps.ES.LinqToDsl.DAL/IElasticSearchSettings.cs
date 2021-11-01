namespace GuruOps.ES.LinqToDsl.DAL
{
    public interface IElasticSearchSettings
    {
        string EndpointUris { get; set; }
        ConnectionPoolType ConnectionPoolType { get; set; }
        int RequestTimeout { get; set; }
        string User { get; set; }
        string Password { get; set; }
        int RetryCount { get; set; }
        int PageSize { get; set; }
    }

    public enum ConnectionPoolType
    {
        Single = 0,
        Static = 1
    }
}