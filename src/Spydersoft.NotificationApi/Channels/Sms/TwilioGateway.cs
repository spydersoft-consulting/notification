using Microsoft.Extensions.Options;
using Twilio.Clients;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Spydersoft.NotificationApi.Channels.Sms;

public sealed class TwilioGateway(IOptions<TwilioOptions> options) : ITwilioGateway
{
    private readonly TwilioOptions _options = options.Value;
    private readonly TwilioRestClient _client = new(options.Value.AccountSid, options.Value.AuthToken);

    public async Task<TwilioSendResult> SendAsync(string toPhoneNumber, string body, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = await MessageResource.CreateAsync(
                to: new PhoneNumber(toPhoneNumber),
                from: new PhoneNumber(_options.FromNumber),
                body: body,
                client: _client);

            var failed = message.Status == MessageResource.StatusEnum.Failed
                || message.Status == MessageResource.StatusEnum.Undelivered;
            return failed
                ? new TwilioSendResult(false, message.Sid, message.ErrorMessage ?? message.Status.ToString())
                : new TwilioSendResult(true, message.Sid, null);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            return new TwilioSendResult(false, null, ex.Message);
        }
    }
}
