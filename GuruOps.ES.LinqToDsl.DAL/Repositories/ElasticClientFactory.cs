using System;
using System.Linq;
using Elasticsearch.Net;
using GuruOps.ES.LinqToDsl.Models;
using Nest;
using Nest.JsonNetSerializer;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace GuruOps.ES.LinqToDsl.DAL.Repositories
{
    public interface IElasticClientFactory
    {
        ElasticClient Create<T>() where T : DocumentBase;

        IElasticLowLevelClient CreateLowLevel(
            TimeSpan requestTimeout,
            CreateConnectionSettingsDelegate connectionSettingsFactoryMethod);
    }

    public class ElasticClientFactory : IElasticClientFactory
    {
        private readonly IElasticSearchSettings _settings;
        private readonly IIndexNameResolver _indexNameResolver;

        public ElasticClientFactory(IElasticSearchSettings settings, IIndexNameResolver indexNameResolver)
        {
            _settings = settings;
            _indexNameResolver = indexNameResolver;
        }
        public ElasticClient Create<T>() where T : DocumentBase
        {
            var connectionSettings = CreateConnectionSettings(
                _settings,
                TimeSpan.FromSeconds(_settings.RequestTimeout),
                (pool, sourceSerializer) => new ConnectionSettings(pool, sourceSerializer));

            var indexName = _indexNameResolver.GetIndexName(typeof(T).Name.ToLower());
            
            connectionSettings.BasicAuthentication(_settings.User, _settings.Password);
            connectionSettings.DisableDirectStreaming();

            connectionSettings.DefaultMappingFor<T>(i => i
                .IndexName(indexName));

            var result = new ElasticClient(connectionSettings);
            return result;
        }

        public IElasticLowLevelClient CreateLowLevel(
            TimeSpan requestTimeout,
            CreateConnectionSettingsDelegate connectionSettingsFactoryMethod)
        {
            var connectionSettings = CreateConnectionSettings(_settings, requestTimeout, connectionSettingsFactoryMethod);
            connectionSettings.BasicAuthentication(_settings.User, _settings.Password);
            var transport = new Transport<IConnectionConfigurationValues>(connectionSettings);
            var result = new ElasticLowLevelClient(transport);
            return result;
        }

        private ConnectionSettings CreateConnectionSettings(
            IElasticSearchSettings settings,
            TimeSpan requestTimeout,
            CreateConnectionSettingsDelegate factoryMethod)
        {
            var pool = GetEsConnectionPool(settings);
            var connectionSettings = factoryMethod(pool, JsonSerializer.Default)
                .RequestTimeout(requestTimeout)
                .EnableHttpCompression();
            return connectionSettings;
        }

        private IConnectionPool GetEsConnectionPool(IElasticSearchSettings settings)
        {
            IConnectionPool pool;
            switch (settings.ConnectionPoolType)
            {
                default:
                    pool = new SingleNodeConnectionPool(new Uri(settings.EndpointUris));
                    break;
                case ConnectionPoolType.Static:
                    var uris = settings
                        .EndpointUris
                        .Split(';')
                        .Select(uri => new Uri(uri));
                    pool = new StaticConnectionPool(uris);
                    break;
            }
            return pool;
        }

        private class JsonSerializer : ConnectionSettingsAwareSerializerBase
        {
            private JsonSerializer(
                IElasticsearchSerializer builtinSerializer,
                IConnectionSettingsValues connectionSettings)
                : base(builtinSerializer, connectionSettings)
            {
            }

            protected override JsonSerializerSettings CreateJsonSerializerSettings() =>
                new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    Formatting = Formatting.None,
                    NullValueHandling = NullValueHandling.Include,
                    DefaultValueHandling = DefaultValueHandling.Include,
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                };

            public static IElasticsearchSerializer Default(
                IElasticsearchSerializer builtin,
                IConnectionSettingsValues values) =>
                new JsonSerializer(
                    builtin,
                    values);
        }

    }

    public delegate ConnectionSettings CreateConnectionSettingsDelegate(
        IConnectionPool connectionPool,
        ConnectionSettings.SourceSerializerFactory sourceSerializer);
}
