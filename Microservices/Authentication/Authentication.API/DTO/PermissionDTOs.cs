// YourNamespace/DTOs/PermissionDTOs.cs

namespace YourNamespace.DTOs
{
    public class ModuleDto
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
    }

    public class FeatureDto
    {
        public string ModuleId { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
    }

    public class PermissionTypeDto
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public int BitPosition { get; set; }
    }

    public class PermissionFlagDto
    {
        public string PermissionTypeId { get; set; }
        public bool IsGranted { get; set; }
    }
}