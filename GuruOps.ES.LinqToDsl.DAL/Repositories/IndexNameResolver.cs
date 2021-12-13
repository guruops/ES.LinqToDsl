using System;

namespace GuruOps.ES.LinqToDsl.DAL.Repositories
{
    public class IndexNameResolver : IIndexNameResolver
    {
        public string GetIndexName(string indexName)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (string.IsNullOrEmpty(environment))
            {
                return indexName;
            }

            return $"{environment.ToLower()}_{indexName}";
        }
    }
}