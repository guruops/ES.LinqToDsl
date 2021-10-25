using System;

namespace GuruOps.ES.LinqToDsl.DAL.Repositories
{
    public class ContinuationTokenEncoder
    {
        private const char Delimiter = ';';
        private const string ElasticSearchPrefix = "es";
        private const string DocumentDbPrefix = "db";

        /// <exception cref="InvalidOperationException"></exception>
        public (ContinuationTokenType type, string token) Decode(string continuationToken)
        {
            (ContinuationTokenType type, string token) result;
            if (!string.IsNullOrEmpty(continuationToken))
            {
                var segments = continuationToken.Split(Delimiter);
                if (segments.Length == 2)
                {
                    switch (segments[0])
                    {
                        case DocumentDbPrefix:
                            result = (type: ContinuationTokenType.DocumentDb, token: segments[1]);
                            break;
                        case ElasticSearchPrefix:
                            result = (type: ContinuationTokenType.ElasticSearch, token: segments[1]);
                            break;
                        default:
                            throw new InvalidOperationException($"Invalid token - {continuationToken}.");
                    }
                }
                else
                {
                    //old documentDb continuationToken
                    result = segments.Length == 1
                        ? (type: ContinuationTokenType.DocumentDb, token: continuationToken)
                        : throw new InvalidOperationException($"Invalid token - {continuationToken}.");
                }
            }
            else
            {
                result = (type: ContinuationTokenType.None, token: continuationToken);
            }

            return result;
        }

        /// <exception cref="InvalidOperationException"></exception>
        public string Encode(ContinuationTokenType type, string sourceContinuationToken)
        {
            string result;
            if (!string.IsNullOrEmpty(sourceContinuationToken))
            {
                switch (type)
                {
                    case ContinuationTokenType.DocumentDb:
                        result = Concat(DocumentDbPrefix, sourceContinuationToken);
                        break;
                    case ContinuationTokenType.ElasticSearch:
                        result = Concat(ElasticSearchPrefix, sourceContinuationToken);
                        break;
                    default:
                        throw new InvalidOperationException($"TokenType - {type} is not support in this context.");
                }
            }
            else
            {
                result = sourceContinuationToken;
            }
            return result;
        }

        private static string Concat(string prefix, string sourceToken)
        {
            var result = $"{prefix}{Delimiter}{sourceToken}";
            return result;
        }
    }
}
