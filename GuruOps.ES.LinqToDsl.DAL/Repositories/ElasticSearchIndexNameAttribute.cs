using System;

namespace GuruOps.ES.LinqToDsl.DAL.Repositories
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ElasticSearchIndexNameAttribute : Attribute
    {
        public string IndexName { get;}
        
        public ElasticSearchIndexNameAttribute(string indexName)
        {
            if (string.IsNullOrWhiteSpace(indexName))
            {
                throw new Exception("IndexName can't be empty");
            }

            IndexName = indexName;
        }   
    }
}