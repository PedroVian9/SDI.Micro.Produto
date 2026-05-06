using SDI.Back.API.Configuration;

namespace SDI.Back.API.Messaging;

public static class KafkaServiceCollectionExtensions
{
    public static IServiceCollection AddKafkaMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<KafkaOptions>(configuration.GetSection(KafkaOptions.SectionName));
        services.Configure<KafkaOptions>(options =>
        {
            var bootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS");
            if (!string.IsNullOrWhiteSpace(bootstrapServers))
            {
                options.BootstrapServers = bootstrapServers;
            }

            var enabled = Environment.GetEnvironmentVariable("KAFKA_ENABLED");
            if (bool.TryParse(enabled, out var parsedEnabled))
            {
                options.Enabled = parsedEnabled;
            }

            var failOnPublishError = Environment.GetEnvironmentVariable("KAFKA_FAIL_ON_PUBLISH_ERROR");
            if (bool.TryParse(failOnPublishError, out var parsedFailOnPublishError))
            {
                options.FailOnPublishError = parsedFailOnPublishError;
            }

            var allowedEventTypes = Environment.GetEnvironmentVariable("KAFKA_ALLOWED_EVENT_TYPES");
            if (!string.IsNullOrWhiteSpace(allowedEventTypes))
            {
                options.AllowedEventTypes = allowedEventTypes
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList();
            }
        });
        services.AddSingleton<IKafkaEventPublisher, KafkaEventPublisher>();

        return services;
    }
}
