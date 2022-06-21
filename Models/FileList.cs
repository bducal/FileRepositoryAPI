using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace FileRepositoryAPI.Models;

public class FileList {

    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("FileName")]
    [JsonPropertyName("FileName")]
    public string FileName { get; set; } = null!;

    [BsonElement("FileTags")]
    [JsonPropertyName("FileTags")]
    public List<string> FileTags { get; set; } = null!;

    [BsonElement("FileOwner")]
    [JsonPropertyName("FileOwner")]
    public string FileOwner { get; set; } = null!;
}