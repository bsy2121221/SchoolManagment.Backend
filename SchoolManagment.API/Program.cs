using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SchoolManagment.API.Authorization;
using SchoolManagment.Auth.Helpers;
using SchoolManagment.Auth.Services.Implementations;
using SchoolManagment.Auth.Services.Interfaces;
using SchoolManagment.DataAccess.Context;
using SchoolManagment.DataAccess.Repositories.Implementations;
using SchoolManagment.DataAccess.Repositories.Interfaces;
using SchoolManagment.Models.Common;
using System.Text;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container

// Database Context
builder.Services.AddSingleton<IDbContext, DapperContext>();

// JWT Configuration
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured");
var issuer = jwtSettings["Issuer"] ?? "SchoolManagementAPI";
var audience = jwtSettings["Audience"] ?? "SchoolManagementClient";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Constants.AuthPolicies.SuperAdminOnly,
        policy => policy.RequireRole(Constants.Roles.SuperAdmin));

    // SuperAdmin included deliberately. A platform administrator who has switched into
    // a school with POST /api/Schools/{id}/switch carries that school's SchoolId and is
    // granted every module in the seeded grid; excluding them by role name here would
    // make the grid lie -- the UI would offer buttons that always 403. "AdminOnly" means
    // "the school's administrator, or the platform owner acting as one".
    options.AddPolicy(Constants.AuthPolicies.AdminOnly,
        policy => policy.RequireRole(Constants.Roles.Admin, Constants.Roles.SuperAdmin));

    options.AddPolicy(Constants.AuthPolicies.TeacherOnly,
        policy => policy.RequireRole(Constants.Roles.Teacher));

    options.AddPolicy(Constants.AuthPolicies.StudentOnly,
        policy => policy.RequireRole(Constants.Roles.Student));

    // SuperAdmin for the same reason as AdminOnly above. This one is used by nine
    // controllers, so the effect is broad: a switched platform administrator can now read
    // what a school administrator can read. That is the direction the permission grid
    // already pointed -- SuperAdmin holds every module -- and the role names were simply
    // out of step with it.
    options.AddPolicy(Constants.AuthPolicies.AdminOrTeacher,
        policy => policy.RequireRole(
            Constants.Roles.Admin,
            Constants.Roles.Teacher,
            Constants.Roles.SuperAdmin
        ));

    options.AddPolicy(Constants.AuthPolicies.AllSchoolUsers,
        policy => policy.RequireRole(
            Constants.Roles.Admin,
            Constants.Roles.Teacher,
            Constants.Roles.Student,
            Constants.Roles.Parent
        ));

    // One policy per (module, action) for [RequiresPermission]. The role-name
    // policies above stay: they are coarse route guards, and the permission grid
    // is the fine-grained check inside them.
    options.AddPermissionPolicies();
});

builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
  // NOT Microsoft.OpenApi.Models

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Paste JWT token from login"
    });

    // v10: delegate + OpenApiSecuritySchemeReference (not OpenApiReference)
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

// HttpContext for TenantContext
builder.Services.AddHttpContextAccessor();

// Register Helpers
builder.Services.AddScoped<IJwtHelper, JwtHelper>();
builder.Services.AddScoped<ITenantContext, TenantContext>();

// Register Repositories
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<ISchoolRepository, SchoolRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<IClassRepository, ClassRepository>();
builder.Services.AddScoped<ISubjectRepository, SubjectRepository>();
builder.Services.AddScoped<ITeacherRepository, TeacherRepository>();
builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
builder.Services.AddScoped<IExaminationRepository, ExaminationRepository>();
builder.Services.AddScoped<IResultRepository, ResultRepository>();
builder.Services.AddScoped<IParentRepository, ParentRepository>();
builder.Services.AddScoped<IScheduleRepository, ScheduleRepository>();
builder.Services.AddScoped<IFeeRepository, FeeRepository>();
builder.Services.AddScoped<ISettingRepository, SettingRepository>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();

// Register Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISchoolService, SchoolService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<IClassService, ClassService>();
builder.Services.AddScoped<ISubjectService, SubjectService>();
builder.Services.AddScoped<ITeacherService, TeacherService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IExaminationService, ExaminationService>();
builder.Services.AddScoped<IResultService, ResultService>();
builder.Services.AddScoped<IParentService, ParentService>();
builder.Services.AddScoped<IScheduleService, ScheduleService>();
builder.Services.AddScoped<IFeeService, FeeService>();
builder.Services.AddScoped<ISettingService, SettingService>();

// Last, because it composes the services above rather than a repository of its own.
builder.Services.AddScoped<IDashboardService, DashboardService>();

// CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });

    // For production, use specific origins
    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins("https://yourdomain.com", "https://api.yourdomain.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddControllers();

// [ApiController] rejects an invalid model before the action body runs, and by
// default answers with RFC 9110 ProblemDetails. That bypassed every
// "if (!ModelState.IsValid)" block in every controller -- they were unreachable --
// and meant a validation failure arrived in a different shape from every other
// error the API returns, with the field names under "errors" as an object rather
// than the ApiResponse envelope's list of { field, message }.
//
// Rewriting the factory puts validation failures back in the envelope the
// controllers already declare through [ProducesResponseType(typeof(ApiResponse),
// 400)], so a client has one error shape to parse and can map each message onto
// the input that produced it.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .SelectMany(entry => entry.Value!.Errors.Select(
                error => new ErrorDetail(entry.Key, error.ErrorMessage)))
            .ToList();

        return new BadRequestObjectResult(
            ApiResponse.FailureResult("Validation failed", errors));
    };
});


// Swagger/OpenAPI Configuration
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    //app.MapOpenApi();
    //app.UseSwaggerUI(options =>
    //{
    //    options.SwaggerEndpoint("/openapi/v1.json", "My API v1");
    //});
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "My API v1");
    });
    app.UseCors("AllowAll");
}
else
{
    app.UseCors("Production");
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
