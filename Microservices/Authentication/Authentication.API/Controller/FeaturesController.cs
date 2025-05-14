using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using YourNamespace.DTOs;
using YourNamespace.Services;
// using YourApiMicroservice.Auth; // Add this for AuthGuard if needed

namespace YourNamespace.Controllers
{
    [ApiController]
    [Route("api/features")]
    public class FeaturesController : ControllerBase
    {
        private readonly FeatureService _featureService;

        public FeaturesController(FeatureService featureService)
        {
            _featureService = featureService;
        }

        [HttpGet]
        // [AuthGuard("Administration", "Features", "Read")] // Add this if needed
        public Task<IActionResult> GetFeatures([FromQuery] string moduleId = null) => 
            _featureService.GetFeaturesAsync(moduleId);

        [HttpPost]
        // [AuthGuard("Administration", "Features", "Create")] // Add this if needed
        public Task<IActionResult> CreateFeature([FromBody] FeatureDto featureDto) => 
            _featureService.CreateFeatureAsync(featureDto);

        [HttpGet("{id}")]
        // [AuthGuard("Administration", "Features", "Read")] // Add this if needed
        public Task<IActionResult> GetFeature(string id) => 
            _featureService.GetFeatureByIdAsync(id);

        [HttpPut("{id}")]
        // [AuthGuard("Administration", "Features", "Update")] // Add this if needed
        public Task<IActionResult> UpdateFeature(string id, [FromBody] FeatureDto featureDto) => 
            _featureService.UpdateFeatureAsync(id, featureDto);

        [HttpDelete("{id}")]
        // [AuthGuard("Administration", "Features", "Delete")] // Add this if needed
        public Task<IActionResult> DeleteFeature(string id) => 
            _featureService.DeleteFeatureAsync(id);
    }
}