namespace SchoolManagment.Models.DTOs.Parents
{
    public class ParentDTO
    {
        public int Id { get; set; }
        public string? Occupation { get; set; }
        public decimal? AnnualIncome { get; set; }
        public bool IsActive { get; set; }

        // User details
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }

        // Children info (for list views)
        public string? ChildrenNames { get; set; }
        public int ChildrenCount { get; set; }

        /// <summary>
        /// Father, Mother or Guardian -- and only populated by
        /// <c>GET /api/Parents/student/{studentId}</c>, because the relationship belongs to
        /// the link row rather than to the parent: the same person can be a Father to one
        /// child and a Guardian to another. Null on every other response.
        ///
        /// It exists because the previous query returned it through
        /// <see cref="ChildrenNames"/> (<c>sp.Relationship AS ChildrenNames</c>), so a
        /// caller reading the children of a student's parent got the word "Father".
        /// </summary>
        public string? Relationship { get; set; }

        public int SchoolId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
