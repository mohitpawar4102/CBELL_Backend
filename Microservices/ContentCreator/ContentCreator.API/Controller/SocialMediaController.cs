// using Microsoft.AspNetCore.Mvc;
// using System.Threading.Tasks;
// using ContentCreator.API.Services;
// using ContentCreator.API.DTO;
// using ContentCreator.API.Auth;
// using YourApiMicroservice.Auth;

// namespace ContentCreator.API.Controllers
// {
//     [Route("api/social-media")]
//     [ApiController]
//     public class SocialMediaController : ControllerBase
//     {
//         private readonly SocialMediaService _socialMediaService;

//         public SocialMediaController(SocialMediaService socialMediaService)
//         {
//             _socialMediaService = socialMediaService;
//         }

//         [HttpPost("post")]
//         // [AuthGuard("Social Media", "Content Management", "Create")]
//         public async Task<IActionResult> PostContent([FromBody] SocialMediaPostDTO postDto)
//         {
//             if (string.IsNullOrEmpty(postDto.Platform))
//             {
//                 return BadRequest("Platform must be specified");
//             }

//             if (postDto.MediaUrls == null || postDto.MediaUrls.Count == 0)
//             {
//                 return BadRequest("At least one media URL must be provided");
//             }

//             switch (postDto.Platform.ToLower())
//             {
//                 case "instagram":
//                     return await _socialMediaService.PostToInstagramAsync(postDto);
//                 default:
//                     return BadRequest($"Unsupported platform: {postDto.Platform}");
//             }
//         }
//     }
// } 