using System.Collections.Generic;
using Nest;

namespace GuruOps.ES.LinqToDsl.DAL.ES.Helpers
{
    internal static class AggregationHelpers
    {
        public static AggregationContainer GetAggregation(int itemCount, int skip, QueryBase query, FieldSort fieldSort, bool isDocumentSourceNeeded)
        {
            var aggregation = GetRemovingDuplicatesAggregation(itemCount, skip, fieldSort, isDocumentSourceNeeded);
            return new AggregationContainer()
            {
                Filter = new FilterAggregation("filter")
                {
                    Filter = query
                },
                Aggregations = aggregation
            };
        }

        private static TermsAggregation GetRemovingDuplicatesAggregation(int itemCount, int skip, FieldSort fieldSort, bool isDocumentSourceNeeded)
        {
            var topHitsAggregation = new TopHitsAggregation("duplicateDocuments")
            {
                Size = 1,
                Sort = new List<ISort>()
                            {
                                new FieldSort()
                                    {Field = new Field("_index"), Order = SortOrder.Descending}
                            }
            };

            if (!isDocumentSourceNeeded)
            {
                topHitsAggregation.Source = new SourceFilter
                {
                    Includes = new[] { "_id" }
                };
            }

            return new TermsAggregation("duplicateCount")
            {
                Field = new Field("_id"),
                Size = int.MaxValue,
                ShardSize = int.MaxValue,
                Aggregations =topHitsAggregation && GetMaxAggregation(fieldSort) && GetPagedAggregation(itemCount, skip, fieldSort)
            };
        }

        private static MaxAggregation GetMaxAggregation(FieldSort sortField)
        {
            if (sortField == null) return null;
            return new MaxAggregation("order", sortField.Field);
        }

        private static BucketSortAggregation GetPagedAggregation(int itemCount, int skip, ISort sortField)
        {
            if (sortField == null) return null;
            return new BucketSortAggregation("result_bucket_sort")
            {
                Sort = new List<ISort>
                {
                    new FieldSort { Field = "order", Order = sortField.Order}
                },
                From = skip,
                Size = itemCount == 0 ? 500 : itemCount
            };
        }
    }
}
