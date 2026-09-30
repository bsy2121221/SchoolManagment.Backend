namespace SchoolManagment.Models.Entities
{
    /// <summary>
    /// dbo.Addresses. A person may hold one address per AddressType
    /// (UQ_Addresses_Person_Type) and at most one flagged IsPrimary
    /// (UX_Addresses_Person_Primary, a filtered unique index).
    ///
    /// AddressType is constrained to Constants.AddressTypes.
    /// </summary>
    public class Address
    {
        public int Id { get; set; }

        /// <summary>NULL only for the platform SuperAdmin's rows.</summary>
        public int? SchoolId { get; set; }

        public int PersonId { get; set; }

        public string AddressType { get; set; } = "Permanent";

        public string AddressLine1 { get; set; } = string.Empty;
        public string? AddressLine2 { get; set; }
        public string? Landmark { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? PostalCode { get; set; }

        public bool IsPrimary { get; set; } = true;
        public bool IsActive { get; set; } = true;

        public int? CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
