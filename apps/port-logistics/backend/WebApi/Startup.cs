using Microsoft.EntityFrameworkCore;
using DataModel.Repository;
using Domain.IRepository;
using Application.Services;

namespace Domain
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		protected virtual void ConfigureServices(IServiceCollection services)
		{
			services.AddControllers();

			services
				.AddEndpointsApiExplorer()
				.AddSwaggerGen()
				.AddDbContext<AbsanteeContext>(options =>
				{
					options.UseSqlServer(Configuration["ConnectionString"]);
				});

			// Register services and repositories
			services.AddScoped<IPhysicalResourceRepository, PhysicalResourceRepository>();
			services.AddScoped<PhysicalResourceService>();
		}

		public virtual void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseSwagger();
				app.UseSwaggerUI();
			}

			app.UseHttpsRedirection();
			app.UseRouting();
			app.UseAuthorization();
			
			// Serve arquivos estáticos da pasta wwwroot
			app.UseDefaultFiles();
			app.UseStaticFiles();
			
			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllers();
				
				// Fallback para SPA
				endpoints.MapFallbackToFile("index.html");
			});
		}
	}
}