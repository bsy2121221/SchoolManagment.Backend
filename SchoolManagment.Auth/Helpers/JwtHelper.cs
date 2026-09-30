using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SchoolManagment.Models.Common;
using SchoolManagment.Models.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SchoolManagment.Auth.Helpers
{
    public interface IJwtHelper
    {
        /// <summary>
        /// <paramref name="permissions"/> is baked into the token as one "perm"
        /// claim per module, so authorising a request costs no database round
        /// trip. Pass null and the token carries no permissions at all, which
        /// denies everything the permission checks guard -- so the login and
        /// refresh paths must always supply the grid.
        /// </summary>
        string GenerateAccessToken(User user, School? school = null, IEnumerable<RolePermission>? permissions = null);

        string GenerateRefreshToken();
        ClaimsPrincipal? ValidateToken(string token);
        int? GetUserIdFromToken(string token);
        int? GetSchoolIdFromToken(string token);
        int? GetRoleIdFromToken(string token);
    }

    public class JwtHelper : IJwtHelper
    {
        private readonly IConfiguration _configuration;
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _expiryMinutes;

        public JwtHelper(IConfiguration configuration)
        {
            _configuration = configuration;
            _secretKey = _configuration["Jwt:SecretKey"] ?? throw new ArgumentNullException("Jwt:SecretKey is not configured");
            _issuer = _configuration["Jwt:Issuer"] ?? "SchoolManagementAPI";
            _audience = _configuration["Jwt:Audience"] ?? "SchoolManagementClient";
            _expiryMinutes = int.TryParse(_configuration["Jwt:ExpiryMinutes"], out int expiry) ? expiry : 60;
        }

        public string GenerateAccessToken(
            User user, School? school = null, IEnumerable<RolePermission>? permissions = null)
        {
            var claims = new List<Claim>
            {
                new Claim(Constants.JwtClaims.UserId, user.Id.ToString()),
                new Claim(Constants.JwtClaims.Username, user.Username),
                new Claim(Constants.JwtClaims.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(Constants.JwtClaims.Role, user.Role),

                // The role name stays, because [Authorize(Roles = "...")] matches
                // on it. The id is what the permission grid and the database key
                // on, and it survives a role being renamed.
                new Claim(Constants.JwtClaims.RoleId, user.RoleId.ToString()),

                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Normally the user's own school. A SuperAdmin has none, so when one is
            // passed a school explicitly -- switching into a tenant -- that school
            // is what scopes the token, and every school-scoped controller then
            // works unchanged. For everyone else the two agree, because the school
            // was looked up from user.SchoolId in the first place.
            var effectiveSchoolId = user.SchoolId ?? school?.Id;

            if (effectiveSchoolId.HasValue)
            {
                claims.Add(new Claim(Constants.JwtClaims.SchoolId, effectiveSchoolId.Value.ToString()));
            }

            if (!string.IsNullOrEmpty(school?.SchoolCode))
            {
                claims.Add(new Claim(Constants.JwtClaims.SchoolCode, school.SchoolCode));
            }

            if (permissions != null)
            {
                foreach (var permission in permissions)
                {
                    var packed = PackPermission(permission);
                    if (packed != null)
                        claims.Add(new Claim(Constants.JwtClaims.Permission, packed));
                }
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_expiryMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// "Users:VCED" -- module name, colon, one letter per granted flag.
        /// Returns null for a row that grants nothing, so an all-false row does
        /// not cost a claim; absence and "nothing granted" deny alike.
        /// </summary>
        private static string? PackPermission(RolePermission permission)
        {
            if (string.IsNullOrWhiteSpace(permission.ModuleName))
                return null;

            var flags = new StringBuilder(4);
            if (permission.CanView) flags.Append(Constants.PermissionClaim.View);
            if (permission.CanCreate) flags.Append(Constants.PermissionClaim.Create);
            if (permission.CanEdit) flags.Append(Constants.PermissionClaim.Edit);
            if (permission.CanDelete) flags.Append(Constants.PermissionClaim.Delete);

            if (flags.Length == 0)
                return null;

            return $"{permission.ModuleName}{Constants.PermissionClaim.Separator}{flags}";
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public ClaimsPrincipal? ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_secretKey);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _issuer,
                    ValidateAudience = true,
                    ValidAudience = _audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

                if (validatedToken is not JwtSecurityToken jwtToken ||
                    !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return null;
                }

                return principal;
            }
            catch
            {
                return null;
            }
        }

        public int? GetUserIdFromToken(string token) =>
            GetIntClaim(token, Constants.JwtClaims.UserId);

        public int? GetSchoolIdFromToken(string token) =>
            GetIntClaim(token, Constants.JwtClaims.SchoolId);

        public int? GetRoleIdFromToken(string token) =>
            GetIntClaim(token, Constants.JwtClaims.RoleId);

        private int? GetIntClaim(string token, string claimType)
        {
            var value = ValidateToken(token)?.FindFirst(claimType)?.Value;
            return int.TryParse(value, out int parsed) ? parsed : null;
        }
    }
}
