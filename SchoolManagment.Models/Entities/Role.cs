namespace SchoolManagment.Models.Entities
{
    /// <summary>
    /// dbo.Roles. Global, not per school: one set of roles for the whole
    /// platform.
    ///
    /// Id is NOT an identity column. Ids 1..5 are a contract shared with the
    /// database (CK_Users_SchoolScope names RoleId 1 by number, because a CHECK
    /// constraint cannot join to another table) -- see Constants.RoleIds.
    /// Custom roles are allocated from 100 upward, enforced by CK_Roles_Id.
    /// </summary>
    public class Role
    {
        public int Id { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string RoleCode { get; set; } = string.Empty;
        public string? Description { get; set; }

        /// <summary>The five seeded roles. They cannot be renamed or deleted.</summary>
        public bool IsSystemRole { get; set; }

        public bool IsActive { get; set; } = true;

        public int? CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        /// <summary>Filled by sp_GetRoleById's second result set.</summary>
        public List<RolePermission> Permissions { get; set; } = new();
    }
}
