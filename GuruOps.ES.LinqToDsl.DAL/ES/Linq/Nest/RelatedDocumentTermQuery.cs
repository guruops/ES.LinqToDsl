using GuruOps.ES.LinqToDsl.DAL.Models;
using Nest;
using System.Linq.Expressions;

namespace GuruOps.ES.LinqToDsl.DAL.ES.Linq.Nest
{
    public static class RelatedDocumentTermQuery
    {
        public static QueryBase Create(Expression source, ConstantExpression relatedDocumentConstnExpression)
        {
            if (relatedDocumentConstnExpression.Value == null)
            {
                return !new ExistsQuery { Field = source };
            }
            var left = new TermQuery
            {
                Field = RelatedDocumentsFieldFactory.Create((MemberExpression)source, "documentType"),
                Value = Expression.Constant(((RelatedDocument)(relatedDocumentConstnExpression.Value)).DocumentType).Value
            };
            var right = new TermQuery
            {
                Field = RelatedDocumentsFieldFactory.Create((MemberExpression)source, "id"),
                Value = Expression.Constant(((RelatedDocument)(relatedDocumentConstnExpression.Value)).Id).Value
            };
            return left && right;
        }
    }
}
