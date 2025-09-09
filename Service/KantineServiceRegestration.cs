using ProActive2508.Service;
namespace ProActive2508.Service
{
    static class KantineServiceRegestration
    {
        public static IServiceCollection AddKantineServices(this IServiceCollection services)
        {
            services.AddScoped<IKantineWeekService, KantineWeekService>();
            return services;
        }
    }
}
