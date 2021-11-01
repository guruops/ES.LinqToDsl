using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Elasticsearch.Net;
using GuruOps.ES.LinqToDsl.DAL.ES.Helpers;
using GuruOps.ES.LinqToDsl.DAL.ES.Linq.Nest;
using GuruOps.ES.LinqToDsl.DAL.ES.Linq.Visitors;
using GuruOps.ES.LinqToDsl.Models;
using GuruOps.ES.LinqToDsl.Models.Attributes;
using GuruOps.ES.LinqToDsl.Models.Exceptions;
using GuruOps.ES.LinqToDsl.Models.QueryModels;
using Microsoft.Extensions.Logging;
using Nest;
using QueryBase = Nest.QueryBase;
using Result = GuruOps.ES.LinqToDsl.Models.QueryModels.Result;

namespace GuruOps.ES.LinqToDsl.DAL.Repositories
{
    public class ElasticSearchRepository<T> : IRepository<T> where T : DocumentBase
    {
        private readonly IElasticSearchSettings _elasticSearchSettingsSettings;
        private readonly ILogger<ElasticSearchRepository<T>> _logger;
        private readonly int _defaultPageSize = 2000;
        private readonly Time _noScrollLifetime = null;
        private readonly ElasticClient _elasticClient;

        public ElasticSearchRepository(IElasticClientFactory elasticClientFactory,
            IElasticSearchSettings elasticSearchSettingsSettings,
            ILogger<ElasticSearchRepository<T>> logger)
        {
            _elasticClient = elasticClientFactory.Create<T>();
            _elasticSearchSettingsSettings = elasticSearchSettingsSettings;
            _logger = logger;
            _defaultPageSize = elasticSearchSettingsSettings.PageSize;
        }

        public async Task<Result> UpsertAsync(T newDocument, T currentDocument, bool disableAudit = false)
        {
            var request = new UpdateRequest<T, T>(newDocument.Id)
            {
                Doc = newDocument,
                DocAsUpsert = true,
                Refresh = Refresh.WaitFor,
                
            };

            newDocument.Modified = DateTime.UtcNow;
            var response = await _elasticClient.UpdateAsync(request);
            
            Result result = new Result
            {
                Output = newDocument,
                Exception = response.OriginalException
            };
            return result;
        }

        public async Task<Result> AddAsync(T document)
        {
            IIndexRequest<T> DoNotUpdateWithSameId(IndexDescriptor<T> i) => i.OpType(OpType.Create).Refresh(Refresh.WaitFor);
            var response = await _elasticClient.IndexAsync(document, DoNotUpdateWithSameId)
                .ConfigureAwait(false);
            var result = new Result();
            if (response.IsValid)
            {
                result.Output = document;
            }
            else
            {
                result.Exception = IsHttpConflict(response.OriginalException)
                    ? new DocumentConflictException()
                    : response.OriginalException;
            }
            return result;
        }

        public Task<ResultList> GetAllAsync(
            Expression<Func<T, object>> orderBy = null,
            Expression<Func<T, object>> orderByDesc = null,
            bool includeDeleted = false)
        {
            QueryBase query = null;
            var fieldSort = CreateSortField(orderBy, orderByDesc);
            var result = SearchAsync(query, fieldSort, _defaultPageSize, _noScrollLifetime, includeDeleted);
            return result;
        }

        public async Task<Result> GetAsync(string id)
        {
            var documentPath = DocumentPath<T>.Id(id);
            var response = await _elasticClient.GetAsync(documentPath)
                .ConfigureAwait(false);
            var result = new Result { Output = response.Source };
            if (!IsHttpNotFound(response))
            {
                result.Exception = response.OriginalException;
            }
            return result;
        }

        public Task<ResultList> GetWhereAsync(
            Expression<Func<T, bool>> predicate,
            Expression<Func<T, object>> orderBy = null,
            Expression<Func<T, object>> orderByDesc = null,
            bool includeDeleted = false)
        {
            QueryBase query = CreateFilterQuery(predicate);
            FieldSort fieldSort = CreateSortField(orderBy, orderByDesc);
            var result = SearchAsync(query, fieldSort, _defaultPageSize, _noScrollLifetime, includeDeleted);
            return result;
        }

        public async Task<ResultList> GetWhereWithPagingAsync(
            Expression<Func<T, bool>> predicate = null,
            Expression<Func<T, object>> orderBy = null,
            Expression<Func<T, object>> orderByDesc = null,
            int itemCount = 10,
            string continuationToken = "",
            bool includeDeleted = false,
            string searchText = null,
            int pageTake = 0,
            int pageSkip = 0)
        {
            var isCompositeRequest =
                _elasticClient.ConnectionSettings.DefaultIndices.FirstOrDefault().Value.Split(',').Length > 1;

            QueryBase filterQuery = CreateFilterQuery(predicate);
            QueryBase searchQuery = CreateSearchQuery(searchText);
            FieldSort fieldSort = CreateSortField(orderBy, orderByDesc);


            var query = filterQuery && searchQuery && CreateDeletedQuery(includeDeleted);
            var result = new ResultList();
            var request = new SearchRequest( typeof(T));
            if (isCompositeRequest)
            {
                var agg = AggregationHelpers.GetAggregation(pageTake, pageSkip, query, fieldSort, isDocumentSourceNeeded: true);
                request.Aggregations = new AggregationDictionary {{"duplicateCount", agg}};
                request.Size = 0;
            }
            else
            {
                request.Query = query;
                request.Size = pageTake == 0 ? _defaultPageSize : pageTake;
                request.From = pageSkip;
                if (fieldSort != null)
                {
                    request.Sort = new List<ISort>
                    {
                        fieldSort
                    };
                }
            }

            ISearchResponse<T> response = null;

            var triesCount = _elasticSearchSettingsSettings.RetryCount;
            while (triesCount > 0)
            {
                try
                {
                    response = await _elasticClient
                        .SearchAsync<T>(request)
                        .ConfigureAwait(false);

                    if (!response.IsValid)
                    {
                        result.Exception = new ElasticSearchRepositoryException(response.OriginalException, _elasticClient.RequestResponseSerializer.SerializeToString(request));
                    }

                    result = new ResultList
                    {
                        Output = isCompositeRequest ? GetDocumentsFromAggregation(response)?.ToList() : response.Documents.ToList<object>(),
                        ContinuationToken = response.ScrollId,
                        Exception = result.Exception
                    };                    
                    break;
                }
                catch (Exception e)
                {
                    triesCount--;
                }
            }

            if (triesCount == 0)
                throw new ElasticSearchRepositoryException($"ES cluster is not available(index: { _elasticClient.ConnectionSettings.DefaultIndices.FirstOrDefault().Value})",
                    response?.OriginalException, _elasticClient.RequestResponseSerializer.SerializeToString(request));


            return result;

        }
        
        public Task<Result> DeleteAsync(T document, bool disableAudit = false)
        {
            // TODO: check caller if currentDocument can be pass
            // to save unnecessary call to retrieve currentDocument
            document.Deleted = true;
            var result = UpsertAsync(document, null, disableAudit);
            return result;
        }

        public async Task<int> CountAsync(
            Expression<Func<T, bool>> predicate = null,
           bool includeDeleted = false,
            string searchText = null)
        {
            var isCompositeRequest =
                _elasticClient.ConnectionSettings.DefaultIndices.FirstOrDefault().Value.Split(',').Length > 1;

            QueryBase includeDeletedQuery = CreateDeletedQuery(includeDeleted);
            QueryBase filterQuery = CreateFilterQuery(predicate);
            QueryBase searchQuery = CreateSearchQuery(searchText);
            QueryBase query = includeDeletedQuery && filterQuery && searchQuery;            
            if (!isCompositeRequest)
            {
                //TODO!!!!!!!!!!!!!!!!!
                var countRequest = new CountRequest(typeof(T)) { Query = query };
                CountResponse response = await _elasticClient.CountAsync(countRequest)
                    .ConfigureAwait(false);

                if (!response.IsValid)
                {
                    throw new ElasticSearchRepositoryException(response.OriginalException, _elasticClient.RequestResponseSerializer.SerializeToString(countRequest));
                }
                return (int)response.Count;
            }
            else
            {
                var request = new SearchRequest(typeof(T));
                var agg = AggregationHelpers.GetAggregation(int.MaxValue, 0, query, null, isDocumentSourceNeeded: false);
                request.Aggregations = new AggregationDictionary { { "duplicateCount", agg } };
                request.Size = 0;

                ISearchResponse<T> response = await _elasticClient
                        .SearchAsync<T>(request)
                        .ConfigureAwait(false);

                if (!response.IsValid)
                {
                    //Initially bad design.
                    //For a reason here should be throw an exception if request fails. Ideally method's result should contain Exception and stick with class design
                    throw new ElasticSearchRepositoryException(response.OriginalException, _elasticClient.RequestResponseSerializer.SerializeToString(request));
                }

                return GetCountFromAggregation(response);
            }
        }

        public async Task<ResultList> BulkCreateAsync(List<T> documents)
        {
            _logger.LogDebug($"ESRepository.BulkCreateAsync() - Start");
            ResultList result = new ResultList();

            try
            {
                _logger.LogDebug($"ESRepository.BulkCreateAsync() - Call ES.IndexManyAsync() method");
                var response = await _elasticClient.IndexManyAsync(documents)
                        .ConfigureAwait(false);

                _logger.LogDebug($"ESRepository.BulkCreateAsync() - Call ES.IndexManyAsync completed. Response Valid:{response.IsValid}");
                if (response.IsValid)
                {
                    result.Output = documents.ToList<dynamic>();
                }
                else
                {
                    result.Exception = IsHttpConflict(response.OriginalException)
                        ? new DocumentConflictException()
                        : response.OriginalException;

                    _logger.LogError($"ESRepository.BulkCreateAsync() - Response Message:{result.Exception?.Message}", result.Exception);
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                _logger.LogError($"Error in ESRepository.BulkCreateAsync(). Message:{ex.Message}", ex);
            }

            _logger.LogDebug($"ESRepository.BulkCreateAsync() - End");
            return result;
        }

        public async Task<ResultList> BulkUpsertAsync(List<T> documents)
        {
            _logger.LogDebug($"ESRepository.BulkUpsertAsync() - Start");
            ResultList result = new ResultList();

            try
            {
                _logger.LogDebug($"ESRepository.BulkUpsertAsync() - Create BulkDescriptor");
                var descriptor = new BulkDescriptor();
                foreach (var doc in documents)
                    descriptor.Index<T>(i => i
                        .Document(doc));

                _logger.LogDebug($"ESRepository.BulkUpsertAsync() - Call ES.BulkAsync() method");
                var response = await _elasticClient.BulkAsync(descriptor)
                        .ConfigureAwait(false);

                _logger.LogDebug($"ESRepository.BulkUpsertAsync() - Call ES.BulkAsync completed. Response Valid:{response.IsValid}");
                if (response.IsValid)
                {
                    result.Output = documents.ToList<dynamic>();
                }
                else
                {
                    result.Exception = IsHttpConflict(response.OriginalException)
                        ? new DocumentConflictException()
                        : response.OriginalException;

                    _logger.LogError($"ESRepository.BulkUpsertAsync() - Response Message:{result.Exception?.Message}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                _logger.LogError($"Error in ESRepository.BulkUpsertAsync(). Message:{ex.Message}", ex);
            }

            _logger.LogDebug($"ESRepository.BulkUpsertAsync() - End");
            return result;
        }

        public async Task<ResultList> BulkDeleteAsync(List<T> documents)
        {
            _logger.LogDebug($"ESRepository.BulkDeleteAsync() - Start");
            ResultList result = new ResultList();

            try
            {
                foreach (var document in documents)
                    document.Deleted = true;

                result = await BulkUpsertAsync(documents);
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                _logger.LogError($"Error in ESRepository.BulkDeleteAsync(). Error:{ex.Message}", ex);
            }

            _logger.LogDebug($"ESRepository.BulkDeleteAsync() - End");
            return result;
        }

        #region Private Methods
        private async Task<ResultList> SearchAsync(
            QueryBase query,
            FieldSort fieldSort,
            int? itemCount,
            Time scrollLifetime,
            bool includeDeleted)
        {
            QueryBase includeDeletedQuery = CreateDeletedQuery(includeDeleted);
            QueryBase searchQuery = query && includeDeletedQuery;
            var request = new SearchRequest(typeof(T))
            {
                Query = searchQuery,
                Size = itemCount,
                Scroll = scrollLifetime
            };
            if (fieldSort != null)
            {
                request.Sort = new List<ISort>
                {
                    fieldSort
                };
            }
            
            ISearchResponse<T> response = await _elasticClient
                .SearchAsync<T>(request)
                .ConfigureAwait(false);
            _logger.LogDebug(response.DebugInformation);
            var result = new ResultList
            {
                Output = response.Documents.ToList<object>(),
                ContinuationToken = response.ScrollId
            };

            if (!response.IsValid)
            {
                result.Exception = new ElasticSearchRepositoryException(response.OriginalException, _elasticClient.RequestResponseSerializer.SerializeToString(request));
                _logger.LogError( response.OriginalException.Message, response.OriginalException);
            }
            return result;
        }

        private static IEnumerable<object> GetDocumentsFromAggregation(ISearchResponse<T> response)
        {
            return ((BucketAggregate)((SingleBucketAggregate)response.Aggregations["duplicateCount"])?.Values
                    .FirstOrDefault())?.Items
                .Select(i => ((TopHitsAggregate)((KeyedBucket<object>)i)["duplicateDocuments"]))
                .SelectMany(i => i.Documents<T>());
        }

        private static int GetCountFromAggregation(ISearchResponse<T> response)
        {
            var count = ((BucketAggregate)((SingleBucketAggregate)response.Aggregations["duplicateCount"])?.Values
                    .FirstOrDefault())?.Items
                .Select(i => ((TopHitsAggregate)((KeyedBucket<object>)i)["duplicateDocuments"])).Count();
            return count.Value;
        }

        private async Task<ResultList> ScrollAsync(string scrollId, Time lifetime)
        {
            var scrollRequest = new ScrollRequest(scrollId, lifetime);
            ISearchResponse<T> response = await _elasticClient
                .ScrollAsync<T>(scrollRequest)
                .ConfigureAwait(false);
            ResultList result = new ResultList
            {
                Output = response.Documents.ToList<object>(),
                ContinuationToken = response.ScrollId
            };

            if (!response.IsValid)
            {
                result.Exception = new ElasticSearchRepositoryException(response.OriginalException, _elasticClient.RequestResponseSerializer.SerializeToString(scrollRequest));
            }
            return result;
        }

        private static void GetFieldNames(Type currentType, string propertyName, List<string> fields)
        {
            foreach (PropertyInfo pi in currentType.GetProperties())
            {
                if (Attribute.IsDefined(pi, typeof(JsonPropertyNameAttribute)))
                {
                    var jsonPropertyName = string.Empty;
                    var attrs = pi.GetCustomAttributes(true);
                    foreach (object attr in attrs)
                    {
                        if (attr is JsonPropertyNameAttribute authAttr)
                        {
                            jsonPropertyName = authAttr.Name;
                        }
                    }

                    if (Attribute.IsDefined(pi, typeof(FullTextSearchPropertyAttribute)))
                    {
                        fields.Add(string.IsNullOrEmpty(propertyName) ? jsonPropertyName : $"{propertyName}.{jsonPropertyName}");
                    }
                    else
                    {
                        GetFieldNames(pi.PropertyType, $"{propertyName}{jsonPropertyName}", fields);
                    }

                    if (pi.PropertyType.GetInterface(nameof(ICollection)) != null && pi.PropertyType.IsGenericType)
                    {
                        var genericType = pi.PropertyType.GenericTypeArguments[0];
                        GetFieldNames(genericType, string.IsNullOrEmpty(propertyName) ? jsonPropertyName : $"{propertyName}.{jsonPropertyName}", fields);
                    }
                }
            }
        }

        private static QueryBase CreateSearchQuery(string searchText)
        {
            QueryBase result = null;
            if (!string.IsNullOrEmpty(searchText))
            {
                var isEmail = searchText.IsValidEmail();

                if (!isEmail)
                {
                    Regex pattern = new Regex("[-+@/;,\t\r ]|[\n]");
                    searchText = $"*{pattern.Replace(searchText, " ")}*";
                }

                result = new QueryStringQuery
                {
                    Query = searchText,
                    DefaultOperator = Operator.And,
                    Fields = GetSearchFields()
                };
            }
            return result;
        }

        private static Fields GetSearchFields()
        {
            var type = typeof(T);

            var fieldNames = new List<string>();

            GetFieldNames(type, string.Empty, fieldNames);

            Fields fields = null;
            if (fieldNames.Any())
            {
                var field = new Field(fieldNames[0]);
                var isSecondFieldAdded = false;
                foreach (var fieldName in fieldNames.Skip(1))
                {
                    fields = isSecondFieldAdded ? fields.And(fieldName) : field.And(fieldName);
                    isSecondFieldAdded = true;
                }
            }

            return fields;
        }

        private static FieldSort CreateSortField(
            Expression<Func<T, object>> orderBy = null,
            Expression<Func<T, object>> orderByDesc = null)
        {
            if (orderBy != null && orderByDesc != null)
            {
                throw new InvalidOperationException("OrderBy and OrderByDesc cannot be used together");
            }
            FieldSort result = null;
            if (orderBy != null)
            {
                result = new SortingTranslator()
                    .Translate(orderBy.Body);
                result.Order = SortOrder.Ascending;
            }
            if (orderByDesc != null)
            {
                result = new SortingTranslator()
                    .Translate(orderByDesc.Body);
                result.Order = SortOrder.Descending;
            }
            return result;
        }

        private static QueryBase CreateFilterQuery(Expression expression)
        {
            QueryBase result = null;
            if (expression != null)
            {
                Expression evaluatedExpression = PartialEvaluator.Evaluate(expression);
                Expression body = ((LambdaExpression)evaluatedExpression).Body;
                Expression optimizedExpression = new EnumerationExpressionVisitor().Visit(body);
                FilterTranslator filterTranslator = CreateFilterTranslator();
                result = filterTranslator.Translate(optimizedExpression);
            }
            return result;
        }

        private static bool IsHttpConflict(Exception originalException)
        {
            var result = originalException is ElasticsearchClientException exception &&
                         exception.Response.HttpStatusCode == (int)HttpStatusCode.Conflict;
            return result;
        }

        private static bool IsHttpNotFound(IResponse response)
        {
            var result = response.OriginalException is ElasticsearchClientException exception && exception.Response.HttpStatusCode == (int)HttpStatusCode.NotFound;
            return result;
        }

        private static FilterTranslator CreateFilterTranslator()
        {
            var equalityQueryTranslator = new EqualityQueryTranslator();
            var rangeQueryTranslator = new RangeQueryTranslator();
            var anyEqualityQueryTranslator = new AnyEqualityQueryTranslator();
            var result = new FilterTranslator(equalityQueryTranslator, rangeQueryTranslator, anyEqualityQueryTranslator);
            return result;
        }

        private static QueryBase CreateDeletedQuery(bool isDeleted)
        {
            if (isDeleted) return null;
            Expression<Func<T, bool>> predicate = t => t.Deleted;
            var constantExpression = Expression.Constant(false);
            var equalityQueryTranslator = new EqualityQueryTranslator();
            var result = equalityQueryTranslator.Create(predicate.Body, constantExpression);
            return result;
        }
        #endregion
    }
}
