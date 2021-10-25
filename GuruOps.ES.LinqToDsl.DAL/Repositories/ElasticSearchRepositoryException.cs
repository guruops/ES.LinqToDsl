using System;

namespace GuruOps.ES.LinqToDsl.DAL.Repositories
{
    [Serializable]
    public class ElasticSearchRepositoryException : Exception
    {
        public ElasticSearchRepositoryException(string message, Exception innerException, string requestBody) : base(message, innerException)
        {
            RequestBody = requestBody;
        }

        public ElasticSearchRepositoryException(Exception innerException, string requestBody) : base(innerException?.Message, innerException)
        {
            RequestBody = requestBody;
        }
        public string RequestBody { get; }
    }
}
