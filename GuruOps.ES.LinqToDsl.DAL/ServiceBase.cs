using GuruOps.ES.LinqToDsl.DAL.Exceptions;
using GuruOps.ES.LinqToDsl.DAL.Repositories;
using GuruOps.ES.LinqToDsl.Models;
using GuruOps.ES.LinqToDsl.Models.QueryModels;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace GuruOps.ES.LinqToDsl.DAL
{
    public abstract class ServiceBase<T> : IServiceBase<T> where T : Document
    {
        private readonly IRepository<T> _repository;
        private readonly IElasticSearchUserContext _elasticSearchUserContext;

        private readonly ILogger<ServiceBase<T>> _logger;
        private readonly IElasticSearchSettings _elasticSearchSettings;


        protected ServiceBase(
            IRepository<T> repository,
            IElasticSearchUserContext elasticSearchUserContext,
            ILogger<ServiceBase<T>> logger,
            IElasticSearchSettings elasticSearchSettings)
        {
            _repository = repository;
            _elasticSearchUserContext = elasticSearchUserContext;
            _logger = logger;
            _elasticSearchSettings = elasticSearchSettings;
        }

        private int MaxRetry = 3;

        public virtual async Task<Result<T>> CountAsync(
            Expression<Func<T, bool>> predicate = null,
            bool includeDeleted = false,
            string searchText = null,
            bool system = false)
        {
            var result = new Result<T> { Count = null };

            try
            {
                result.Count = await _repository.CountAsync(predicate, includeDeleted, searchText);
            }
            catch (Exception exp)
            {
                _logger.LogError(exp, exp.Message);
            }
            return result;
        }

        public virtual async Task<Result<T>> CreateAsync(T document)
        {
            // Set CreatedBy/ModifiedBy
            SetCreatedModifiedBy(document);

            var result = (await _repository.AddAsync(document).ConfigureAwait(false)).FromResult<T>();

            return result;
        }

        public virtual async Task<ResultList<T>> BulkCreateAsync(List<T> documents, int batchCount = 100)
        {
            var results = new ResultList<T>
            {
                Output = new List<T>()
            };

            _logger.LogInformation($"ServiceBase.BulkCreateAsync. Total:{documents.Count}");

            if (batchCount == 0)
                results.Exception = new BadRequestException($"Invalid batchCount:{batchCount}.");
            else
            {
                var partitionKeys = documents
                ?.Select(c => c.DocumentType)
                .Distinct();

                if (partitionKeys?.Count() > 1)
                    results.Exception = new BadRequestException("Documents with different partition keys is not allowed.");
                else if (partitionKeys?.Any() == true)
                {
                    // Set CreatedBy/ModifiedBy
                    SetCreatedModifiedBy(documents);
                    
                    int skip = 0, retry = 0;
                    do
                    {
                        int iteration = (skip / batchCount) + 1;

                        var items = documents.Skip(skip).Take(batchCount);
                        if (!items.Any())
                            break;

                        try
                        {
                            _logger.LogInformation($"ServiceBase.BulkCreateAsync by batch. Iteration:{iteration}, Count:{items.Count()}, Retry:{retry}");
                            var result = (await _repository.BulkCreateAsync(items.ToList())
                                            .ConfigureAwait(false))
                                            .FromResult<T>();

                            results.RequestCharge += result.RequestCharge;
                            if (!results.IsError)
                            {
                                skip += batchCount;
                                retry = 0;
                                results.Output.AddRange(result.Output?.ToList() ?? new List<T>());
                            }
                            else
                            {
                                _logger.LogError($"_repository.BulkCreateAsync result error. Iteration:{iteration}, Count:{items.Count()}, Retry:{retry}");
                                retry++;
                                if (retry == MaxRetry)
                                {
                                    retry = 0;
                                    skip += batchCount;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"_repository.BulkCreateAsync unhandled error. Iteration:{iteration}, Count:{items.Count()}, Retry:{retry}, Message:{ex.Message}", ex);
                            
                            retry++;
                            if (retry == MaxRetry)
                            {
                                retry = 0;
                                skip += batchCount;
                            }
                        }
                    } while (true);
                }
            }

            return results;
        }

        public virtual async Task<Result<T>> UpsertAsync(T document)
        {
            // Set who modified the document
            var currentDocument = (await GetAsync(document.Id))?.Output;

            SetCreatedModifiedBy(document, currentDocument == null);

            var result = (await _repository.UpsertAsync(document, currentDocument, this as IDisableAudit != null).ConfigureAwait(false)).FromResult<T>();

            return result;
        }

        public virtual async Task<ResultList<T>> BulkUpsertAsync(
            List<T> documents,
            List<T> currentDocuments = null,
            int batchCount = 100)
        {
            var results = new ResultList<T>
            {
                Output = new List<T>()
            };

            _logger.LogInformation($"ServiceBase.BulkUpsertAsync. Total:{documents.Count}");

            if (batchCount == 0)
                results.Exception = new BadRequestException($"Invalid batchCount:{batchCount}.");
            else
            {
                var partitionKeys = documents
                ?.Select(c => c.DocumentType)
                .Distinct();

                if (partitionKeys?.Count() > 1)
                    results.Exception = new BadRequestException("Documents with different partition keys is not allowed.");
                else if (partitionKeys?.Any() == true)
                {
                    currentDocuments = await GetOriginalDocuments(documents, currentDocuments);

                    // Set CreatedBy/ModifiedBy
                    SetCreatedModifiedBy(documents, currentDocuments);

                    int skip = 0, retry = 0;
                    do
                    {
                        int iteration = (skip / batchCount) + 1;

                        var items = documents.Skip(skip).Take(batchCount);
                        if (!items.Any())
                            break;

                        try
                        {
                            _logger.LogInformation($"ServiceBase.BulkUpsertAsync by batch. Iteration:{iteration}, Count:{items.Count()}, Retry:{retry}");
                            var result = (await _repository.BulkUpsertAsync(items.ToList())
                                            .ConfigureAwait(false))
                                            .FromResult<T>();

                            results.RequestCharge += result.RequestCharge;
                            if (!results.IsError)
                            {
                                skip += batchCount;
                                retry = 0;
                                results.Output.AddRange(result.Output?.ToList() ?? new List<T>());
                            }
                            else
                            {
                                _logger.LogError($"_repository.BulkUpsertAsync result error. Iteration:{iteration}, Count:{items.Count()}, Retry:{retry}");

                                retry++;
                                if (retry == MaxRetry)
                                {
                                    retry = 0;
                                    skip += batchCount;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"_repository.BulkUpsertAsync unhandled error. Iteration:{iteration}, Count:{items.Count()}, Retry:{retry}, Message:{ex.Message}");

                            retry++;
                            if (retry == MaxRetry)
                            {
                                retry = 0;
                                skip += batchCount;
                            }
                        }
                    } while (true);
                }
            }
            return results;
        }

        // TODO
        [Obsolete("Use BulkUpsertAsync")]
        public virtual async Task<ResultList<T>> UpsertAllAsync(
            List<T> documents,
            List<T> currentDocuments = null,
            int batchCount = 10)
        {
            var results = new ResultList<T>
            {
                Output = new List<T>()
            };

            if (documents?.Any() == true)
            {
                int retry = 0;
                var pending = new List<T>(documents);
                var disableAudit = this as IDisableAudit != null;
                while (true)
                {
                    _logger.LogInformation($"ServiceBase.UpdateAllAsync. Total:{documents.Count}, Pending:{pending.Count}, Retry:{retry}");

                    var tasks = new List<Task<Result>>();
                    var updateResults = new List<Result<T>>();
                    foreach (var document in pending)
                    {
                        var currentDocument = currentDocuments?.FirstOrDefault(c => c.Id == document.Id);
                        tasks.Add(_repository.UpsertAsync(document, currentDocument, disableAudit));
                        if (tasks.Count == batchCount)
                        {
                            updateResults.AddRange((await Task.WhenAll(tasks.ToArray()).ConfigureAwait(false))
                                .Select(c => c.FromResult<T>()));

                            tasks.Clear();
                        }
                    }

                    if (tasks.Any())
                    {
                        updateResults.AddRange((await Task.WhenAll(tasks.ToArray()).ConfigureAwait(false))
                            .Select(c => c.FromResult<T>()));
                    }

                    results.Output = updateResults
                        .Where(c => !c.IsError && c.Output != null)
                        .Select(c => c.Output)
                        .ToList();

                    // determine pending to update
                    pending = (from doc1 in documents
                               join doc2 in results.Output on doc1.Id equals doc2.Id into docs
                               where docs.Count() == 0
                               select doc1).ToList();

                    // report documents that were not updated
                    results.Exception = null;
                    if (pending.Any())
                        results.Exception = new Exception($"Following document(s) with Id:[{string.Join(",", pending)}] were not updated successfully");

                    if (!pending.Any() || retry++ == MaxRetry)
                        break;
                }
            }

            return results;
        }

        public virtual async Task<Result<T>> CloneAsync(T document)
        {
            // should be implemented by specific service
            await Task.Yield();
            throw new NotImplementedException();
        }

        public virtual async Task<Result<T>> DeleteAsync(T document)
        {
            // Set CreatedBy/ModifiedBy
            SetCreatedModifiedBy(document, false);
            
            Result<T> result = (await _repository.DeleteAsync(document, this as IDisableAudit != null).ConfigureAwait(false)).FromResult<T>();

            return result;
        }

        // TODO
        [Obsolete("Use BulkDeleteAsync")]
        public virtual async Task<ResultList<T>> DeleteAllAsync(List<T> documents,
            int batchCount = 10)
        {
            var results = new ResultList<T>
            {
                Output = new List<T>()
            };

            if (documents?.Any() == true)
            {
                int retry = 0;
                var pending = new List<T>(documents);
                while (true)
                {
                    _logger.LogInformation($"ServiceBase.DeleteAllAsync. Total:{documents.Count}, Retry:{retry}");

                    var tasks = new List<Task<Result>>();
                    var deleteResults = new List<Result<T>>();
                    foreach (var document in pending)
                    {
                        tasks.Add(_repository.DeleteAsync(document));
                        if (tasks.Count == batchCount)
                        {
                            deleteResults.AddRange((await Task.WhenAll(tasks.ToArray()).ConfigureAwait(false))
                                .Select(c => c.FromResult<T>()));

                            tasks.Clear();
                        }
                    }

                    if (tasks.Any())
                    {
                        deleteResults.AddRange((await Task.WhenAll(tasks.ToArray()).ConfigureAwait(false))
                            .Select(c => c.FromResult<T>()));
                    }

                    results.Output = deleteResults
                        .Where(c => !c.IsError && c.Output != null)
                        .Select(c => c.Output)
                        .ToList();

                    // determine pending to delete
                    pending = (from doc1 in documents
                               join doc2 in results.Output on doc1.Id equals doc2.Id into docs
                               where docs.Count() == 0
                               select doc1).ToList();

                    // report documents that were not deleted
                    results.Exception = null;
                    if (pending.Any())
                        results.Exception = new Exception($"Following document(s) with Id:[{string.Join(",", pending)}] were not deleted successfully");

                    if (!pending.Any() || retry++ == MaxRetry)
                        break;
                }
            }

            return results;
        }

        public virtual async Task<ResultList<T>> BulkDeleteAsync(List<T> documents, int batchCount = 100)
        {
            var results = new ResultList<T>
            {
                Output = new List<T>()
            };

            _logger.LogInformation($"ServiceBase.BulkDeleteAsync. Total:{documents.Count}");

            if (batchCount == 0)
                results.Exception = new BadRequestException($"Invalid batchCount:{batchCount}.");
            else
            {
                var partitionKeys = documents
                ?.Select(c => c.DocumentType)
                .Distinct();

                if (partitionKeys?.Count() > 1)
                    results.Exception = new BadRequestException("Documents with different partition keys is not allowed.");
                else if (partitionKeys?.Any() == true)
                {
                    // Set who deleted the documents
                    SetCreatedModifiedBy(documents, false);
                    
                    int skip = 0, retry = 0;
                    do
                    {
                        int iteration = (skip / batchCount) + 1;

                        var items = documents.Skip(skip).Take(batchCount);
                        if (!items.Any())
                            break;

                        try
                        {
                            _logger.LogInformation($"ServiceBase.BulkDeleteAsync by batch. Iteration:{iteration}, Count:{items.Count()}, Retry:{retry}");
                            var result = (await _repository.BulkDeleteAsync(items.ToList())
                                            .ConfigureAwait(false))
                                            .FromResult<T>();

                            results.RequestCharge += result.RequestCharge;
                            if (!results.IsError)
                            {
                                skip += batchCount;
                                retry = 0;
                                results.Output.AddRange(result.Output?.ToList() ?? new List<T>());
                            }
                            else
                            {
                                _logger.LogError($"_repository.BulkDeleteAsync result error. Iteration:{iteration}, Count:{items.Count()}, Retry:{retry}");

                                retry++;
                                if (retry == MaxRetry)
                                {
                                    retry = 0;
                                    skip += batchCount;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"_repository.BulkDeleteAsync unhandled error. Iteration:{iteration}, Count:{items.Count()}, Retry:{retry}, Message:{ex.Message}", ex);

                            retry++;
                            if (retry == MaxRetry)
                            {
                                retry = 0;
                                skip += batchCount;
                            }
                        }
                    } while (true);
                }
            }

            return results;
        }

        /// <summary>
        /// DO NO USE THIS METHOD EXCEPT UNDER EXTREME CONDITIONS
        /// Use GetWhere with a Predicate to perform queries and limit data results
        /// </summary>
        /// <param name="orderBy"></param>
        /// <param name="orderByDesc"></param>
        /// <param name="includeDeleted"></param>
        /// <param name="system">
        /// <para/>True - return only System level data
        /// <para/>False - return Firm level and System level (if supported)
        /// </param>
        /// <returns></returns>
        public virtual async Task<ResultList<T>> GetAllAsync(Expression<Func<T, object>> orderBy = null,
            Expression<Func<T, object>> orderByDesc = null,
            bool includeDeleted = false, bool system = false)
        {
            var result = (await _repository.GetAllAsync(orderBy, orderByDesc, includeDeleted).ConfigureAwait(false))
                .FromResult<T>();

            return result;
        }

        /// <summary>
        /// Get record by Id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="system">
        /// <para/>True - Get system level record by Id
        /// <para/>False - Get firm level record by Id
        /// </param>
        /// <returns></returns>
        public virtual async Task<Result<T>> GetAsync(string id, bool system = false)
        {
            var result = (await _repository.GetAsync(id).ConfigureAwait(false)).FromResult<T>();

            return result;
        }

        /// <summary>
        /// Get record by Name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="system">
        ///     True - Get system level record by name
        ///     False - Get firm level record by name
        /// </param>
        /// <returns></returns>
        public virtual async Task<Result<T>> GetByNameAsync(string name, bool system = false)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get record by External Id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="system">
        ///     True - Get system level record by External Id
        ///     False - Get firm level record by External Id
        /// </param>
        /// <returns></returns>
        public virtual async Task<Result<T>> GetByExternalIdAsync(string id, bool system = false)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get records by Ids
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="orderBy"></param>
        /// <param name="orderByDesc"></param>
        /// <param name="includeDeleted"></param>
        /// <param name="system">
        /// </param>
        /// <returns></returns>
        public virtual async Task<ResultList<T>> GetByIdsAsync(List<string> ids, Expression<Func<T, object>> orderBy = null, Expression<Func<T, object>> orderByDesc = null,
            bool includeDeleted = false, bool system = false)
        {
            ResultList<T> results = new ResultList<T>
            {
                Output = new List<T>()
            };

            ids = ids?.Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .ToList();
            if (ids?.Any() == true)
            {
                var idList = new List<string>();
                int skip = 0, take = 10;
                do
                {
                    idList = ids.Skip(skip).Take(take).ToList();
                    skip += take;
                    var result = await GetWhereAsync(c => idList.Contains(c.Id), orderBy, orderByDesc, includeDeleted, system);
                    if (result.Output != null)
                        results.Output.AddRange(result.Output);
                } while (skip < ids.Count);
            }

            return results;
        }

        /// <summary>
        /// Get all records filtered by predicate expression
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="orderBy"></param>
        /// <param name="orderByDesc"></param>
        /// <param name="includeDeleted"></param>
        /// <param name="system">
        /// <para/>True - return only System level data
        /// <para/>False - return Firm level and System level (if supported)
        /// </param>
        /// <returns></returns>
        public virtual async Task<ResultList<T>> GetAllWhereAsync(
            Expression<Func<T, bool>> predicate,
            Expression<Func<T, object>> orderBy = null,
            Expression<Func<T, object>> orderByDesc = null,
            bool includeDeleted = false,
            bool system = false)
        {
            var result = new ResultList<T>
            {
                Output = new List<T>()
            };

            try
            {
                var countResult = await CountAsync(predicate, includeDeleted: includeDeleted, system: system);
                int count = countResult?.Count ?? 0;
                if (count > 0)
                {
                    var pages = count / _elasticSearchSettings.PageSize;
                    foreach (var page in Enumerable.Range(1, pages + 1))
                    {
                        var pagination = new PaginationRequest<T>
                        {
                            ItemsPerPage = _elasticSearchSettings.PageSize,
                            PageNumber = page,
                            OrderBy = orderBy,
                            OrderByDesc = orderByDesc,
                            IncludeDeleted = includeDeleted,
                        };

                        var pageResult = await GetWithPagingAsync(predicate, pagination);
                        if (!pageResult.IsError && pageResult.Output?.Any() == true)
                            result.Output.AddRange(pageResult.Output);
                    }
                }
            }
            catch (Exception ex)
            {
                
            }

            return result;
        }

        /// <summary>
        /// Get records filtered by predicate expression
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="orderBy"></param>
        /// <param name="orderByDesc"></param>
        /// <param name="includeDeleted"></param>
        /// <param name="system">
        /// <para/>True - return only System level data
        /// <para/>False - return Firm level and System level (if supported)
        /// </param>
        /// <returns></returns>
        public virtual async Task<ResultList<T>> GetWhereAsync(
            Expression<Func<T, bool>> predicate,
            Expression<Func<T, object>> orderBy = null,
            Expression<Func<T, object>> orderByDesc = null,
            bool includeDeleted = false,
            bool system = false)
        {
            var result = (await _repository.GetWhereAsync(predicate, orderBy, orderByDesc, includeDeleted).ConfigureAwait(false)).FromResult<T>();
            return result;
        }

        public virtual async Task<ResultList<T>> GetWithPagingAsync(
            Expression<Func<T, bool>> predicate = null,
            Expression<Func<T, object>> orderBy = null,
            Expression<Func<T, object>> orderByDesc = null,
            int itemCount = 10,
            string continuationToken = "",
            bool includeDeleted = false,
            string searchText = null,
            bool system = false)
        {
            ResultList<T> result;

            result = (await _repository.GetWhereWithPagingAsync(
                predicate,
                orderBy,
                orderByDesc,
                itemCount,
                continuationToken,
                includeDeleted,
                searchText).ConfigureAwait(false)).FromResult<T>();

            return result;
        }

        public async Task<ResultList<T>> GetWithPagingAsync(IFilterQuery<T> query, PaginationRequest<T> request)
        {
            ResultList<T> result;
            try
            {
                var filterBy = query.GetQuery();
                result = await GetWithPagingAsync(filterBy, request);
            }
            catch (Exception e)
            {
                result = new ResultList<T> { Exception = e };
            }

            return result;
        }

        public async Task<ResultList<T>> GetWithPagingAsync(Expression<Func<T, bool>> filterBy, PaginationRequest<T> request)
        {
            if (request.PageNumber < 1)
            {
                return new ResultList<T>()
                {
                    Output = null,
                    ContinuationToken = null,
                    Exception = new Exception("Incorrect page number " + request.PageNumber)
                };
            }

            ResultList<T> result = null;
            string continuationToken = "";

            // For page number 1 just make one call to repository
            if (request.PageNumber == 1)
            {
                ResultList query = await _repository.GetWhereWithPagingAsync(
                    filterBy,
                    request.OrderBy,
                    request.OrderByDesc,
                    request.ItemsPerPage,
                        continuationToken,
                    request.IncludeDeleted,
                    request.SearchText,
                    request.ItemsPerPage,
                    0).ConfigureAwait(false);

                result = (query).FromResult<T>();

                return result;
            }

            // In case we have page number > 1 we need to skip defined number of items and then get requested number of items
            int skip = request.ItemsPerPage * (request.PageNumber - 1);

            result = (await _repository.GetWhereWithPagingAsync(
                filterBy,
                request.OrderBy,
                request.OrderByDesc,
                skip,
                continuationToken,
                request.IncludeDeleted,
                request.SearchText,
                request.ItemsPerPage,
                skip).ConfigureAwait(false)).FromResult<T>();

            continuationToken = result.ContinuationToken;

            // If there is not items for the requested page - return error
            if ((result.Output == null || !result.Output.Any()) && string.IsNullOrEmpty(continuationToken))
            {
                return new ResultList<T>()
                {
                    Output = null,
                    ContinuationToken = null,
                    Exception = new NotFoundException("No content for page " + request.PageNumber)
                };
            }

            if (!string.IsNullOrEmpty(continuationToken))
            {
                result = (await _repository.GetWhereWithPagingAsync(
                    filterBy,
                    request.OrderBy,
                    request.OrderByDesc,
                    request.ItemsPerPage,
                    continuationToken,
                    request.IncludeDeleted,
                    request.SearchText).ConfigureAwait(false)).FromResult<T>();
            }

            return result;
        }

        public virtual async Task<ResultList<T>> UpdateOrderAsync(List<OrderItem> orderList)
        {
            ResultList<T> result = new ResultList<T>
            {
                Output = new List<T>()
            };

            try
            {
                List<string> ids = orderList?
                    .Where(item => !string.IsNullOrWhiteSpace(item.Id))
                    .Select(item => item.Id)?.ToList() ?? new List<string>();

                if (ids.Any())
                {
                    ResultList<T> documents = await GetByIdsAsync(ids);
                    if (documents?.Output?.Any() == true)
                    {
                        var updated = new List<T>();
                        foreach (T document in documents.Output)
                        {
                            OrderItem orderItem = orderList.FirstOrDefault(item => item.Id == document.Id);
                            if (orderItem != null)
                            {
                                if ((document as ISortable).SortOrder != orderItem.SortOrder)
                                {
                                    (document as ISortable).SortOrder = orderItem.SortOrder;
                                    updated.Add(document);
                                }
                                else
                                    result.Output.Add(document);
                            }
                        }

                        // Set who modified the documents
                        SetCreatedModifiedBy(updated, false);

                        var updateResult = await UpsertAllAsync(updated);
                        result.Output.AddRange(updateResult.Output);
                    }
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
            }

            return result;
        }

        

        #region Private methods

        private void SetCreatedModifiedBy(T document, bool isNew = true)
        {
            if (document is ICreatedModified doc && doc != null)
            {
                doc.ModifiedById = _elasticSearchUserContext.UserId;

                if (isNew || string.IsNullOrWhiteSpace(doc.CreatedById))
                    doc.CreatedById = _elasticSearchUserContext.UserId;
            }
        }

        private void SetCreatedModifiedBy(List<T> documents, bool isNew = true)
        {
            foreach (var document in documents ?? new List<T>())
                SetCreatedModifiedBy(document, isNew);
        }

        private void SetCreatedModifiedBy(List<T> documents, List<T> currentDocuments)
        {
            foreach (var document in documents ?? new List<T>())
            {
                var isNew = !currentDocuments.Any(c => c.Id == document.Id);
                SetCreatedModifiedBy(document, isNew);
            }
        }

        private async Task<List<T>> GetOriginalDocuments(List<T> newDocuments, List<T> currentDocuments)
        {
            currentDocuments = currentDocuments ?? new List<T>();
            newDocuments = newDocuments ?? new List<T>();
            var results = currentDocuments;

            var ids = newDocuments
                .Where(doc => !currentDocuments.Any(c => c.Id == doc.Id))
                .Select(doc => doc.Id);

            if (ids.Any())
            {
                var missing = (await GetByIdsAsync(ids.ToList()))
                    ?.Output ?? new List<T>();
                results.AddRange(missing);
            }

            return results;
        }
        #endregion Private methods
    }
}