



using FluentValidation;
using JasperFx.CodeGeneration.Frames;
using Marten;
using Microsoft.AspNetCore.Http.Json;
using SoftwareCenter.Api.Shared;
using SoftwareCenter.Api.Vendors;
using SoftwareCenter.Api.Vendors.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpContextAccessor(); 

// built-in (which means you can change this) hierarchial configuration.
// app.settings.json
// look at ASPNETCORE_ENVIRONMENT variable, and then for an appsettings.DEVELOPMENT.json 
// Your "secrets" in Visual Studio
// In an environment variable for that particular value (ConnectionStrings__software)
// in the command line arguments when you run the API
builder.Services.AddAuthentication().AddJwtBearer();  // AuthN - verifying who someone is.
builder.Services.AddAuthorizationBuilder().AddPolicy("SoftwareCenterManager", pol =>
{
    pol.RequireRole("SoftwareCenter");
    pol.RequireRole("Manager");
   
});
var connectionString = builder.Configuration.GetConnectionString("software") ?? throw new ChaosException("No connection string! Don't start me");
Console.WriteLine("Using this connection string: " + connectionString);
builder.Services.AddMarten(options =>
{
    options.Connection(connectionString);
}).UseLightweightSessions(); // not worth explaining, Marten specific, unless you are interested ask me later.


// Add services to the container.

// global (other than controllers) for minimal APIs, or for instances of the JSON Serializer like you'll see later.
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    // this is useful for errors with validation
    // enums
    options.SerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
  options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// scoped means a new instance of this thing per http request.
builder.Services.AddScoped<IValidator<CommercialVendorCreateModel>, CommercialVendorCreateModelValidator>();
builder.Services.AddScoped<IProvideIdentity, ProvidesIdentityFromJwt>();

// 
var techApiUrl = builder.Configuration.GetConnectionString("techs-api") ?? throw new ChaosException("No Tech API Url ");

// this is a "named, typed http client"
builder.Services.AddHttpClient<TechApiHttp>(client =>
{
    client.BaseAddress = new Uri(techApiUrl);
});

// "Eager" service creation - 
//var techApi = new TechApiHttp();
//builder.Services.AddSingleton<ILookupTechsFromTechApi>(_ => techApi);

// This is saying "in the future, if some code asks for the ILookup thing, just give them
// the thing I already registered above (TechsApiHttp), because it has the base url and all that jazz.
// Provider factory
builder.Services.AddScoped<ILookupTechsFromTechApi>(sp =>
{
    return sp.GetRequiredService<TechApiHttp>();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // we are goig to add, during development, a route called /openapi/v1.json 
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Software Center");
    });
}
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// if someone does a GET  and the ID is a guid, then create that thing and call, otherwise don't

app.Run();

public partial class Program { }