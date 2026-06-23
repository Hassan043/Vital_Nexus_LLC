using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using VitalNexus.Api.Configuration;
using VitalNexus.Infrastructure.Accounts;
using VitalNexus.Infrastructure.Configuration;
using VitalNexus.Infrastructure.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var entraOptions = builder.Configuration.BindEntraExternalIdOptions();
entraOptions.EnsureEntraExternalIdConfiguredForEnvironment(builder.Environment);

if (entraOptions.IsConfigured)
{
    builder.Services.AddEntraExternalIdAuthentication(entraOptions);
    builder.Services.AddVitalNexusCors(entraOptions);
    builder.Services.AddExternalIdentityAccessor();
    builder.Services.AddAccountsUserMapping();
    builder.Services.AddClinicContextResolution(builder.Configuration);
}

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "VitalNexus API", Version = "v1" });

    if (entraOptions.IsConfigured)
    {
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "Entra External ID access token. Scope: access_as_user",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = JwtBearerDefaults.AuthenticationScheme,
            BearerFormat = "JWT",
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer",
                    },
                },
                Array.Empty<string>()
            },
        });
    }
});

builder.Services.AddApplicationInsightsTelemetry();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (entraOptions.IsConfigured && entraOptions.AllowedOrigins.Length > 0)
{
    app.UseCors("Spa");
}

app.UseHttpsRedirection();

if (entraOptions.IsConfigured)
{
    app.UseAuthentication();
}

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
