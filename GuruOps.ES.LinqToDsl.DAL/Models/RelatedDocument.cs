using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GuruOps.ES.LinqToDsl.DAL.Models
{
    public class RelatedDocument
    {
        [JsonPropertyName("documentType")]
        public DocumentType DocumentType { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        public RelatedDocument(DocumentType documentType, string id)
        {
            DocumentType = documentType;
            Id = id;
        }
    }

    public enum DocumentType
    {
        [Display(Name = "Note", Description = "Note")]
        Note = 0
    }
}