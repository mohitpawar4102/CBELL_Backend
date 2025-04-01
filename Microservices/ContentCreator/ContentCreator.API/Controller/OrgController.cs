using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using YourNamespace.Services;
using YourNamespace.DTOs;

namespace YourNamespace.Controllers
{
    [Route("api/organization")]
    [ApiController]
    public class OrganizationController : ControllerBase
    {
        private readonly OrganizationService _organizationService;

        public OrganizationController(OrganizationService organizationService)
        {
            _organizationService = organizationService;
        }

        [HttpPost("create_organization")]
        public Task<IActionResult> CreateOrganization([FromBody] OrganizationDTO organizationDto) => _organizationService.CreateOrganizationAsync(organizationDto);

        [HttpGet("get_all_organizations")]
        public Task<IActionResult> GetAllOrganizations() => _organizationService.GetAllOrganizationsAsync();

        [HttpGet("get_organization/{id}")]
        public Task<IActionResult> GetOrganizationById(string id) => _organizationService.GetOrganizationByIdAsync(id);

        [HttpPut("update/{id}")]
        public Task<IActionResult> UpdateOrganization(string id, [FromBody] OrganizationDTO organizationDto)=> _organizationService.UpdateOrganizationAsync(id, organizationDto);

        [HttpDelete("delete/{id}")]
        public Task<IActionResult> DeleteOrganization(string id) => _organizationService.SoftDeleteOrganizationAsync(id);

    }
}
