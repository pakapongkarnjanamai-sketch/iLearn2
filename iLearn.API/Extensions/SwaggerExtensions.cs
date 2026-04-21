namespace iLearn.API.Extensions
{
    /// <summary>
    /// Swagger / OpenAPI composition for the API host. Only enabled in Development.
    /// </summary>
    public static class SwaggerExtensions
    {
        public static IServiceCollection AddApiSwagger(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new() { Title = "iLearn API", Version = "v1" });
            });

            return services;
        }

        public static WebApplication UseApiSwagger(this WebApplication app)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "iLearn API V1");
            });

            return app;
        }
    }
}
