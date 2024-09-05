using Common.Abstract.Middleware;
using Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.IISIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using Repository.Portal;
using Serilog;
using Shared;
using Shared.Configurations;
using YourClassLibraryName;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
IConfiguration _configuration = new ConfigurationBuilder()
                            .AddJsonFile("appsettings.json")
                             .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
                             .Build();

#region logger

Serilog.Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Error()
.WriteTo.File($"{builder.Environment.WebRootPath}" + @"\\logs\\log.txt", rollingInterval: RollingInterval.Day)
.CreateLogger();

#endregion

AppSettings _settings = new();
builder.Configuration.GetSection("Settings").Bind(_settings, c => c.BindNonPublicProperties = true);
Static.Settings = _settings;

//builder.Services.AddCors();
builder.Services.AddCors(options =>
{
    options.AddPolicy(
      "CorsPolicy",
      builder => builder.WithOrigins(_settings.CorsUrl)
      .AllowAnyMethod()
      .AllowAnyHeader()
      .AllowCredentials());
});
builder.Services.AddPortalScope();
builder.Services.AddDbContext<AlphaCareContext>(item =>
 item.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), x =>
 {
    x.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
 }));
builder.Services.AddIdentity<AppUser, AppRole>()
      .AddEntityFrameworkStores<AlphaCareContext>()
      .AddDefaultTokenProviders();
builder.Services.Configure<IdentityOptions>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(2);
    options.Lockout.MaxFailedAccessAttempts = 3;
});
builder.Services.AddLocalization();
builder.Services.AddAuthentication(IISDefaults.AuthenticationScheme);
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

    options.SerializerSettings.ContractResolver = new DefaultContractResolver
    {
        NamingStrategy = new CamelCaseNamingStrategy(),

    };
    options.SerializerSettings.Formatting = Formatting.Indented;
}).AddJsonOptions(x =>
{
    x.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddLogging(configuration => configuration.ClearProviders());// Removes default console logger
builder.Services.AddRazorPages();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
#region Swagger
//builder.Services.AddSwaggerGen();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("portal-1.0", new OpenApiInfo { Title = "AlphaCare Portal API", Version = "1.0" });
    c.DocInclusionPredicate((documentName, apiDescription) =>
    {
        var apiVersionAttribute = apiDescription.ActionDescriptor.EndpointMetadata.FirstOrDefault(x => x.GetType().Equals(typeof(ApiVersionAttribute)))
            as ApiVersionAttribute;
        if (apiVersionAttribute != null)
        {
            // You can now use the `apiVersionAttribute` to get version information
            var version = apiVersionAttribute.Versions.FirstOrDefault();
            return ((ApiExplorerSettingsAttribute)apiDescription.ActionDescriptor.EndpointMetadata.First(x => x.GetType().Equals(typeof(ApiExplorerSettingsAttribute)))).GroupName + $"-{version.ToString()}" == documentName;
        }
        return ((ApiExplorerSettingsAttribute)apiDescription.ActionDescriptor.EndpointMetadata.First(x => x.GetType().Equals(typeof(ApiExplorerSettingsAttribute)))).GroupName == documentName;
    });

    c.AddSecurityDefinition("OAuth2", new OpenApiSecurityScheme
    {
        Description = "OAuth2",
        Name = "auth",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement() {
        {
            new OpenApiSecurityScheme
            {
            Reference = new OpenApiReference
                {
                Type = ReferenceType.SecurityScheme,
                Id = "OAuth2"
                },
                Scheme = "oauth2",
                Name = "auth",
                In = ParameterLocation.Header,

            },
            new List<string>()
            }
        });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = @"JWT Authorization header using the Bearer scheme. \r\n\r\n 
                      Enter 'Bearer' [space] and then your token in the text input below.
                      \r\n\r\nExample: 'Bearer 12345abcdef'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement() {
        {
            new OpenApiSecurityScheme
            {
            Reference = new OpenApiReference
                {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
                },
                Scheme = "Bearer",
                Name = "Authorization",
                In = ParameterLocation.Header,

            },
            new List<string>()
            }
        });

});
#endregion

//var app = builder.Build();

//// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}
builder.Services.AddHttpContextAccessor();
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseCors(builder =>
    {
        builder.AllowAnyHeader();
        builder.AllowAnyMethod();
        builder.WithOrigins(_settings.CorsUrl);
        builder.AllowCredentials();
    });
}
app.UseSwagger().UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/portal-1.0/swagger.json", "AlphaCare Portal API");
});
app.UseMiddleware<OAuth>();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();
app.Run();
