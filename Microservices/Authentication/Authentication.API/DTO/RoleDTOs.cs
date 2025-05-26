// YourNamespace/DTOs/RoleDTOs.cs

using System.Collections.Generic;

namespace YourNamespace.DTOs
{
    public class RoleCreateDto
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public List<PermissionDto> Permissions { get; set; }
    }

    public class PermissionDto
    {
        public string ModuleId { get; set; }
        public string FeatureId { get; set; }
        public List<PermissionFlagDto> PermissionFlags { get; set; }
    }
    
    public class RoleUpdateDto : RoleCreateDto
    {
        public string Id { get; set; }
    }
}