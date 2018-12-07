using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace SampleReact
{
	public class Startup
	{
		private IConfiguration Configuration;
		private IHostingEnvironment Environment;

		public Startup(IConfiguration configuration, IHostingEnvironment env)
		{
			this.Configuration = configuration;
			this.Environment = env;
		}

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices( IServiceCollection services )
		{
#if DEBUG
			IdentityModelEventSource.ShowPII = true;
#endif
			services.AddOptions();

			services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));

			var settings = Configuration.GetSection("AppSettings").Get<AppSettings>();

			services.AddTransient<IHttpContextAccessor, HttpContextAccessor>();

			// internal - always
			services.AddSingleton<ILogManager, LogManager>();
			services.AddSingleton<IDataManager, DataManager>();
			services.AddSingleton<IRestService, RestServiceLocal>();
			services.AddSingleton<ITokenService, TokenServiceLocal>();
			services.AddSingleton<ICredentialsService, CredentialsServiceLocal>();

			// currently all code is local (monolithic), to extract out each unit of functionality into queues/functions or https/functions (microservices), do this..
			if (settings.ServiceLocation.IPasswordService == ServiceType.Local)
			{
				services.AddSingleton<IPasswordService, PasswordServiceLocal>();
			}

			if (settings.ServiceLocation.ICalculatorService == ServiceType.Local)
			{
				services.AddTransient<IActionProcessor, ActionProcessorLocal>();
			}
			else
			{
				services.AddTransient<IActionProcessor, ActionProcessorLocal>();
			}

			// 3rd party providers
			services.AddSingleton<IEmailProvider, EmailProvider>();
			services.AddSingleton<ISlackProvider, SlackProvider>();
			//services.AddScoped<ApiExceptionFilter>();

#if !DEBUG
			services.AddApplicationInsightsTelemetry(Configuration);
#endif

			services.AddAuthentication(options =>
			{
				// Identity made Cookie authentication the default.
				// However, we want JWT Bearer Auth to be the default.
				options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
				options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
			})
			//default
			.AddJwtBearer(options =>
			{
				#region Default Jwt Options

				// Configure JWT Bearer Auth to expect our security key
				options.TokenValidationParameters =
					new TokenValidationParameters
					{
						LifetimeValidator = (before, expires, token, param) =>
						{
							return expires > DateTime.UtcNow;
						},
						ValidateAudience = false,
						ValidateIssuer = false,
						ValidateActor = false,
						ValidateLifetime = true,
						IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.ASCII.GetBytes(settings.JWTSecretKeys["Site"].Key))
					};

				// We have to hook the OnMessageReceived event in order to
				// allow the JWT authentication handler to read the access
				// token from the query string when a WebSocket or 
				// Server-Sent Events request comes in.
				options.Events = new JwtBearerEvents
				{
					OnMessageReceived = context =>
					{

						// If the request is for our hub...
						var path = context.HttpContext.Request.Path;
						if (path.StartsWithSegments("/sitehub"))
						{
							var accessToken = context.Request.Query["access_token"];
							// Read the token out of the query string
							if (!string.IsNullOrEmpty(accessToken)) context.Token = accessToken;
						}
						return Task.CompletedTask;
					}
				};

				#endregion
			})
			.AddJwtBearer("callbacks", options =>
			{
				#region Scheme 'callbacks' Options

				options.RequireHttpsMetadata = false;
				//options.Audience = "http://local.tinyfeet.co:52524/";
				//options.Authority = "http://local.tinyfeet.co:52524/";
				options.IncludeErrorDetails = true; ;
				options.TokenValidationParameters = new TokenValidationParameters
				{
					//ValidateIssuer = true,
					ValidIssuer = "https://my.tinyfeet.co/",
					//ValidateAudience = true,
					ValidAudience = "https://my.tinyfeet.co/",
					ValidateLifetime = true,
					IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.ASCII.GetBytes(settings.JWTSecretKeys["Callback"].Key)),
					//ValidateIssuerSigningKey = true,
					LifetimeValidator = (before, expires, token, param) => { return expires > DateTime.UtcNow; }
				};

				options.Events = new JwtBearerEvents
				{
					OnTokenValidated = context =>
					{
						// Add the access_token as a claim, as we may actually need it
						var accessToken = context.SecurityToken as JwtSecurityToken;
						if (accessToken != null)
						{
							//ClaimsIdentity identity = context.Ticket.Principal.Identity as ClaimsIdentity;
							//if (identity != null)
							//{
							//	identity.AddClaim(new Claim("access_token", accessToken.RawData));
							//}
						}

						return Task.CompletedTask;
					},
					OnAuthenticationFailed = context =>
					{
						return Task.CompletedTask;
					},
					OnChallenge = context =>
					{
						return Task.CompletedTask;
					},
					OnMessageReceived = context =>
					{
						// If the request is for our hub...
						var path = context.HttpContext.Request.Path;
						if (path.StartsWithSegments("/callback"))
						{
							//var creds = AuthenticationHeaderValue.Parse( context.HttpContext.Request.Headers[HeaderNames.Authorization] );

							//JwtSecurityTokenHandler hand = new JwtSecurityTokenHandler();
							//var x = hand.ReadJwtToken( creds.Parameter);
							//context.Token = x.ToString();
							//if( creds != null ) context.Token = creds.Parameter;
						}
						return Task.CompletedTask;
					}
				};

				#endregion
			});

			services
				.AddMvc(config =>
				{
				})
				.SetCompatibilityVersion(CompatibilityVersion.Version_2_1)
				.AddJsonOptions(setupAction =>
				{
					setupAction.SerializerSettings.Converters = new System.Collections.Generic.List<JsonConverter>(){
						new Newtonsoft.Json.Converters.IsoDateTimeConverter(),
						new Newtonsoft.Json.Converters.StringEnumConverter()
					};
					setupAction.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
					setupAction.SerializerSettings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
					setupAction.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
					setupAction.SerializerSettings.DateFormatString = "o";
					setupAction.SerializerSettings.DefaultValueHandling = DefaultValueHandling.Include;
					setupAction.SerializerSettings.Formatting = Formatting.Indented;
					setupAction.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
				});

			// In production, the React files will be served from this directory
			services.AddSpaStaticFiles( configuration =>
			 {
				 configuration.RootPath = "ClientApp/build";
			 } );

			services.AddResponseCompression();


			services.AddCors(options => options.AddPolicy("CorsPolicy",
				builder =>
				{
					builder.WithOrigins(
						"http://localhost")
							.AllowAnyHeader()
							.AllowAnyMethod()
							.AllowCredentials();
				})
			);

			services.AddMySignalR(settings);
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure( 
			IApplicationBuilder app,
			IHostingEnvironment env,
			IApplicationLifetime applicationLifetime,
			IOptions<AppSettings> settings,
			ILogManager logger,
			IDataManager data,
			IActionProcessor actions
			)
		{

			Console.WriteLine($"Environment: {settings.Value.Environment}");

			ServicePointManager.DefaultConnectionLimit = 10;
			ServicePointManager.UseNagleAlgorithm = false;

			if ( env.IsDevelopment() )
			{
				app.UseDeveloperExceptionPage();
			}
			else
			{
				app.UseExceptionHandler( "/Error" );
				app.UseHsts();
			}

			app.UseStaticFiles();
			app.UseSpaStaticFiles();
			app.UseAuthentication();
			app.UseResponseCompression();

			app.Use(async (context, next) =>
			{
				await next();
				if (context.Response.StatusCode == 404 && !Path.HasExtension(context.Request.Path.Value))
				{
					context.Request.Path = "/index.html";
					context.Response.StatusCode = 200;
					await next();
				}
			})
			.UseCookiePolicy()
			.UseCors("CorsPolicy")
			.UseDefaultFiles(new DefaultFilesOptions { DefaultFileNames = new System.Collections.Generic.List<string> { "index.html" } })
			.UseMySignalR(settings)
			.UseMvc( routes =>
			 {
				 routes.MapRoute(
					 name: "default",
					 template: "{controller}/{action=Index}/{id?}" );
			 } );

			//app.UseSpa( spa =>
			// {
			//	 spa.Options.SourcePath = "ClientApp";

			//	 if( env.IsDevelopment() )
			//	 {
			//		 spa.UseReactDevelopmentServer( npmScript: "start" );
			//	 }
			// } );

			var rs = data.LoadSystemDataAsync().GetAwaiter().GetResult();
			if (rs.Code == ResponseCode.Ok)
			{
				Console.WriteLine("Metadata loaded!");
			}
			else
			{
				EmailProvider ep = new EmailProvider(settings, data);
				ep.SendEmergencyEmail("TinyFeett App Startup Error - Failure to load data", rs.Message);
			}

		}
	}
}
