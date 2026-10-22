namespace Spydersoft.NotificationApi.Channels.Sms;

public static class SmsChannelExtensions
{
    /// <summary>
    /// Registers the SMS sender. The real Twilio gateway is used only when Twilio credentials are
    /// configured; otherwise the in-memory fake keeps offline dev and tests working.
    /// </summary>
    public static IServiceCollection AddSmsChannel(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TwilioOptions>(configuration.GetSection(TwilioOptions.SectionName));

        var twilio = configuration.GetSection(TwilioOptions.SectionName).Get<TwilioOptions>();
        if (twilio?.IsConfigured == true)
        {
            services.AddSingleton<ITwilioGateway, TwilioGateway>();
        }
        else
        {
            services.AddSingleton<ITwilioGateway, FakeTwilioGateway>();
        }

        services.AddScoped<INotificationChannelSender, SmsChannelSender>();
        return services;
    }
}
