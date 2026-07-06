
using Infrastructure.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddSingleton<IJwtTokenGenerator>(sp =>
{
    var options = sp.GetRequiredService<IOptions<JwtOptions>>().Value;
    return new JwtTokenGenerator(options);
});

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
    ?? throw new Exception("Jwt section is missing");

var key = Encoding.UTF8.GetBytes(jwtOptions.Secret);
var issuer = jwtOptions.Issuer;
var audience = jwtOptions.Audience;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        NameClaimType = "sub",
        RoleClaimType = "role"
    };
});

builder.Services.AddApplication(builder.Configuration);
//builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Logging.AddConsole();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

app.Run();
