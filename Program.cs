using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using CityInfo.API.Services;
using CityInfo.API;
using CityInfo.API.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Asp.Versioning;
using System.Reflection;
using Asp.Versioning.ApiExplorer;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.HttpOverrides;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console(restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information) // Console logs Information+
    .WriteTo.File("logs/cityinfo.txt", rollingInterval: RollingInterval.Day, 
    restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Error) // File logs Error+
    .CreateLogger();


var builder = WebApplication.CreateBuilder(args);

// Explicitly configure Kestrel (HTTPS Certificates)
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ConfigureHttpsDefaults(httpsOptions =>
    {
        // Configuration will be picked up from environment variables
    });
});


builder.Host.UseSerilog();

// Add services to the container.

builder.Services.AddControllers(options =>
{
    options.ReturnHttpNotAcceptable = true;
}).AddNewtonsoftJson() // This line requires the Newtonsoft.Json package
.AddXmlDataContractSerializerFormatters();

builder.Services.AddProblemDetails();

 
/*builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        ctx.ProblemDetails.Extensions.Add("additionalInfo", "Here is some additional information");
        ctx.HttpContext.Response.StatusCode = 400;  
        ctx.ProblemDetails.Extensions.Add("server", Environment.MachineName);
    };
});
*/
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

builder.Services.AddSingleton<FileExtensionContentTypeProvider>();
builder.Services.AddSingleton<CitiesDataStore>();

/*builder.Services.AddDbContext<CityInfoContext>(
    dbContextOptions => dbContextOptions.UseSqlite(
        builder.Configuration["ConnectionStrings:CityInfoDBConnectionString"]));*/

builder.Services.AddDbContext<CityInfoContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ICityInfoRepository, CityInfoRepository>();

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Authentication:Issuer"],
            ValidAudience = builder.Configuration["Authentication:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
               Convert.FromBase64String(builder.Configuration["Authentication:SecretForKey"]))
        };
    }
    );

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("MustBeFromSfax", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("city", "sfax");
    });
});

builder.Services.AddApiVersioning(setupAction =>
{
    setupAction.ReportApiVersions = true;
    setupAction.AssumeDefaultVersionWhenUnspecified = true;
    setupAction.DefaultApiVersion = new ApiVersion(1, 0);
}).AddMvc()
.AddApiExplorer(setupAction =>
{
    setupAction.SubstituteApiVersionInUrl = true;
});

//After registred a version api, we need to add the versioning to the swagger
builder.Services.AddEndpointsApiExplorer();

var apiVersionDescriptionProvider = builder.Services.BuildServiceProvider().GetRequiredService<IApiVersionDescriptionProvider>();
builder.Services.AddSwaggerGen(setupAction =>
{
    foreach (var description in
    apiVersionDescriptionProvider.ApiVersionDescriptions)
    {
        //versioning api
        setupAction.SwaggerDoc(
            $"{description.GroupName}",
            new()
            {
                Title = "City Info API",
                Version = description.ApiVersion.ToString(),
                Description = "Through this API you can access cities and their points of interest."
            });
    }

    var xmlCommentsFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlCommentsFullPath = Path.Combine(AppContext.BaseDirectory, xmlCommentsFile);

    //include xml comments
    setupAction.IncludeXmlComments(xmlCommentsFullPath);

    //add authorization to swagger
    setupAction.AddSecurityDefinition("CityInfoApiBearerAuth", new()
    {
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        Description = "Input a valid token to access this API"
    });

    //define the default security scheme : bearer
    setupAction.AddSecurityRequirement(new()
    {
        {
            new ()
            {
                Reference = new OpenApiReference {
                    Type = ReferenceType.SecurityScheme,
                    Id = "CityInfoApiBearerAuth" }
            },
            new List<string>()
        }
    });
});


#if DEBUG
builder.Services.AddTransient<IMailService, LocalMailService>();
#else
builder.Services.AddTransient<IMailService, CloudMailService>();
#endif



builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
    | ForwardedHeaders.XForwardedProto;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI(setupAction =>
    {
        //versioning api
        var descriptions = app.DescribeApiVersions();
        foreach (var description in descriptions)
        {
            setupAction.SwaggerEndpoint(
                $"/swagger/{description.GroupName}/swagger.json",
                description.GroupName.ToUpperInvariant());
        }
    });
//}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"error\": \"An unexpected error occurred.\"}");
        });
    });
}

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CityInfoContext>();
    dbContext.Database.Migrate();
}


app.UseForwardedHeaders();

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.Run();
