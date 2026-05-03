using SDI.Back.API.Configuration;

namespace SDI.Back.API.Messaging;

public static class KafkaServiceCollectionExtensions
{
    public static IServiceCollection AddKafkaMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<KafkaOptions>(configuration.GetSection(KafkaOptions.SectionName));
        services.AddSingleton<IKafkaEventPublisher, KafkaEventPublisher>();

        return services;
    }
}
