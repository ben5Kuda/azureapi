using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using Host.Logger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLog;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerUI;
using UserManagement.Context;

namespace Host
{
  public class Startup
  {
    public Startup(IConfiguration configuration)
    {
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {

      services.AddDbContext<SsoDbContext>(options => options.UseSqlServer(Configuration.GetConnectionString("SsoDbContext")));
      services.AddSingleton(Configuration.GetSection("ElasticSearch").Get<ElasticSettings>());

      JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

      services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
           .AddJwtBearer(options =>
           {
             // base-address of your identityserver
             options.Authority = "https://localhost:5021";
             // name of the API resource
             options.Audience = "https://localhost:5021/resources";
             //options.SaveToken = true;

           });

     
      services.AddSwaggerGen(c =>
      {
        c.SwaggerDoc("v1", new Info { Title = "apicore", Version = "v1" });

        // Define the OAuth2.0 scheme that's in use (i.e. Implicit Flow)
        c.AddSecurityDefinition("oauth2", new OAuth2Scheme
        {
          Type = "oauth2",
          Flow = "implicit",
          AuthorizationUrl = "https://localhost:5021/connect/authorize",

          Scopes = new Dictionary<string, string>
          {
             { "access", "Access identity" },
             { "offline_access", "Access api operations" },
          }
        });


        c.AddSecurityRequirement(new Dictionary<string, IEnumerable<string>>
        {
            { "oauth2", new string[] { } }
        });

      });
      services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IHostingEnvironment env)
    {
      app.UseMiddleware<ResponseLogMiddleware>();

      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
      }
      else
      {
        app.UseHsts();
      }

      GlobalDiagnosticsContext.Set("connectionString", "Server=DVTL597LKC2;Initial Catalog=Logging;Integrated Security=True");

      app.UseHttpsRedirection();
      app.UseAuthentication();

      app.UseMvcWithDefaultRoute();

      app.UseSwagger();

      app.UseSwaggerUI(c =>
      {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "apicore");
        c.DocExpansion(DocExpansion.None);
        c.EnableFilter();
        c.DisplayRequestDuration();
        c.ShowExtensions();

        c.OAuthClientId("mvc");
        c.OAuthClientSecret("secret");
      });
    }
  }
}
