namespace VeterinaryApi.Infrastructure.Payments;

public sealed  class ChargilyOptions
{
    public string ApiSecretKey { get; set; } = null!;
    public bool IsLiveMode { get; set; }
    public string WebhookUrl { get; set; } = null!;
    public string SuccessUrl { get; set; } = null!;
    public string FailureUrl { get; set; } = null!;
}
