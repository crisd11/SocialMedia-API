using AutoMapper;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SocialMedia.Core.Business;
using SocialMedia.Core.CustomEntities;
using SocialMedia.Core.Interfaces;
using SocialMedia.Infrastructure.Data;
using SocialMedia.Infrastructure.Filters;
using SocialMedia.Infrastructure.Interfaces;
using SocialMedia.Infrastructure.Repositories;
using SocialMedia.Infrastructure.Services;
using System;

namespace SocialMedia.Api
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
            //para que lea los mapeos en toda la app
            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            services.AddControllers(options =>
            {
                options.Filters.Add<GlobalExceptionFilter>();
            }).AddNewtonsoftJson(options => //para usar esto hay que instalar newtonsoft en aspnetcore
            {
                //para evitar el loopeo infinito de crear una entidad y te quiera devolver sus propiedades
                //que adentro contienen otra entidad que a su vez tiene la 1er entidad y nunca se rompe el ciclo
                options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;

                //para que no muestre propiedades con valor nulo en las respuestas de las apis
                options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            })
            .ConfigureApiBehaviorOptions(options =>
            {
                //para invalidar los filtros del decorador [APICONTROLLER]
                //options.SuppressModelStateInvalidFilter = true;
            });

            //para inyectar en una clase los valores guardados en un json en appsettings
            services.Configure<PaginationOptions>(Configuration.GetSection("Pagination"));

            services.AddDbContext<SocialMediaContext>(options =>
            options.UseSqlServer(Configuration.GetConnectionString("SocialMedia"))
            );

            //dependancy injection (le decis al metodo que para X interfaz le vas a dar Y implementacion)
            services.AddTransient<IPostBusiness, PostBusiness>();
            services.AddScoped(typeof(IRepository<>), typeof(BaseRepository<>));
            services.AddTransient<IUnitOfWork, UnitOfWork>();
            services.AddSingleton<IUriService>(provider =>
            {
                var accesor = provider.GetRequiredService<IHttpContextAccessor>();
                var request = accesor.HttpContext.Request;
                //http o https         //url del host ej www.misapis.com
                var absoluteUri = string.Concat(request.Scheme, "://", request.Host.ToUriComponent());
                return new UriService(absoluteUri);
            });

            services.AddMvc(options =>
            {
                //para registrar un filtro que va ser usado de forma global
                options.Filters.Add<ValidationFilter>();
            }).AddFluentValidation(options =>  
            {
                //para registrar las validaciones que se hayan creado
                options.RegisterValidatorsFromAssemblies(AppDomain.CurrentDomain.GetAssemblies());
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
