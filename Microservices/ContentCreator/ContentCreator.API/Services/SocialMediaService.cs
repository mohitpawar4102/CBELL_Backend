// using System;
// using System.Net.Http;
// using System.Text;
// using System.Text.Json;
// using System.Threading.Tasks;
// using Microsoft.AspNetCore.Mvc;
// using ContentCreator.API.DTO;
// using Microsoft.Extensions.Configuration;
// using MongoDB.Driver;
// using MongoDB.Driver.GridFS;
// using MongoDB.Bson;
// using System.IO;
// using System.Net.Http.Headers;
// using Library.Models;
// using MongoDB.Bson.Serialization.Attributes;

// namespace ContentCreator.API.Services
// {
//     public class SocialMediaService
//     {
//         private readonly HttpClient _httpClient;
//         private readonly IConfiguration _configuration;
//         private readonly IMongoDatabase _database;
//         private readonly GridFSBucket _gridFs;
//         private readonly IMongoCollection<Document> _documents;
//         private readonly string _instagramApiBaseUrl = "https://graph.instagram.com/v12.0";
//         private readonly string _instagramAuthUrl = "https://api.instagram.com/oauth/authorize";
//         private readonly string _instagramTokenUrl = "https://api.instagram.com/oauth/access_token";

//         public SocialMediaService(HttpClient httpClient, IConfiguration configuration, IMongoDatabase database)
//         {
//             _httpClient = httpClient;
//             _configuration = configuration;
//             _database = database;
            
//             _gridFs = new GridFSBucket(database, new GridFSBucketOptions
//             {
//                 BucketName = "documents",
//                 ChunkSizeBytes = 255 * 1024
//             });
            
//             _documents = database.GetCollection<Document>("Documents");
//         }

//         public async Task<IActionResult> PostToInstagramAsync(SocialMediaPostDTO postDto)
//         {
//             try
//             {
             
//                 if (!ObjectId.TryParse(postDto.MediaDocumentIds[0], out var documentId))
//                 {
//                     return new BadRequestObjectResult("Invalid document ID format");
//                 }

//                 var document = await _documents.Find(d => d.Id == documentId && !d.IsDeleted).FirstOrDefaultAsync();
//                 if (document == null)
//                 {
//                     return new NotFoundObjectResult("Document not found or has been deleted");
//                 }

//                 string tempFilePath = Path.GetTempFileName();
//                 using (var stream = await _gridFs.OpenDownloadStreamAsync(document.FileId))
//                 using (var fileStream = new FileStream(tempFilePath, FileMode.Create))
//                 {
//                     await stream.CopyToAsync(fileStream);
//                 }

//                 using (var form = new MultipartFormDataContent())
//                 {
//                     form.Add(new StringContent(postDto.Caption), "caption");
//                     form.Add(new StringContent(document.ContentType), "media_type");
                    
//                     var fileContent = new StreamContent(File.OpenRead(tempFilePath));
//                     fileContent.Headers.ContentType = new MediaTypeHeaderValue(document.ContentType);
//                     form.Add(fileContent, "media", document.FileName);

//                     var response = await _httpClient.PostAsync($"{_instagramApiBaseUrl}/me/media?access_token={postDto.AccessToken}", form);
                    
//                     if (!response.IsSuccessStatusCode)
//                     {
//                         return new BadRequestObjectResult("Failed to upload media to Instagram");
//                     }

//                     var responseContent = await response.Content.ReadAsStringAsync();
//                     var containerId = JsonSerializer.Deserialize<JsonElement>(responseContent).GetProperty("id").GetString();

//                     var publishResponse = await _httpClient.PostAsync(
//                         $"{_instagramApiBaseUrl}/me/media?access_token={postDto.AccessToken}",
//                         new StringContent(JsonSerializer.Serialize(new { creation_id = containerId }), Encoding.UTF8, "application/json")
//                     );

//                     if (!publishResponse.IsSuccessStatusCode)
//                     {
//                         return new BadRequestObjectResult("Failed to publish media");
//                     }
//                 }

//                 // Clean up temporary file
//                 File.Delete(tempFilePath);

//                 return new OkObjectResult(new { message = "Content posted successfully" });
//             }
//             catch (Exception ex)
//             {
//                 return new BadRequestObjectResult($"Error posting to Instagram: {ex.Message}");
//             }
//         }

//         public string GetInstagramAuthUrl()
//         {
//             var clientId = _configuration["Instagram:ClientId"];
//             var redirectUri = _configuration["Instagram:RedirectUri"];
//             var scope = "user_profile,user_media";

//             return $"{_instagramAuthUrl}?client_id={clientId}&redirect_uri={redirectUri}&scope={scope}&response_type=code";
//         }

//         public async Task<IActionResult> ExchangeCodeForToken(string code)
//         {
//             try
//             {
//                 var clientId = _configuration["Instagram:ClientId"];
//                 var clientSecret = _configuration["Instagram:ClientSecret"];
//                 var redirectUri = _configuration["Instagram:RedirectUri"];

//                 var formData = new Dictionary<string, string>
//                 {
//                     { "client_id", clientId },
//                     { "client_secret", clientSecret },
//                     { "grant_type", "authorization_code" },
//                     { "redirect_uri", redirectUri },
//                     { "code", code }
//                 };

//                 var content = new FormUrlEncodedContent(formData);
//                 var response = await _httpClient.PostAsync(_instagramTokenUrl, content);

//                 if (!response.IsSuccessStatusCode)
//                 {
//                     return new BadRequestObjectResult("Failed to exchange code for token");
//                 }

//                 var responseContent = await response.Content.ReadAsStringAsync();
//                 var tokenData = JsonSerializer.Deserialize<JsonElement>(responseContent);

//                 // Store the token in your database
//                 var tokenCollection = _database.GetCollection<InstagramToken>("InstagramTokens");
//                 var token = new InstagramToken
//                 {
//                     AccessToken = tokenData.GetProperty("access_token").GetString(),
//                     UserId = tokenData.GetProperty("user_id").GetString(),
//                     ExpiresAt = DateTime.UtcNow.AddDays(60)
//                 };

//                 await tokenCollection.ReplaceOneAsync(
//                     t => t.UserId == token.UserId,
//                     token,
//                     new ReplaceOptions { IsUpsert = true }
//                 );

//                 return new OkObjectResult(new { message = "Token obtained and stored successfully" });
//             }
//             catch (Exception ex)
//             {
//                 return new BadRequestObjectResult($"Error exchanging code for token: {ex.Message}");
//             }
//         }
//     }

//     public class InstagramToken
//     {
//         [BsonId]
//         [BsonRepresentation(BsonType.ObjectId)]
//         public string Id { get; set; }
        
//         [BsonElement("userId")]
//         public string UserId { get; set; }
        
//         [BsonElement("accessToken")]
//         public string AccessToken { get; set; }
        
//         [BsonElement("expiresAt")]
//         public DateTime ExpiresAt { get; set; }
//     }
// } 