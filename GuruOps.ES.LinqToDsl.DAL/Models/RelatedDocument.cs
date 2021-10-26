using System.Text.Json.Serialization;

namespace GuruOps.ES.LinqToDsl.DAL.Models
{
    public class RelatedDocument
    {
        [JsonPropertyName("documentType")]
        public string DocumentType { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        public RelatedDocument(string documentType, string id)
        {
            DocumentType = documentType;
            Id = id;
        }
    }
}