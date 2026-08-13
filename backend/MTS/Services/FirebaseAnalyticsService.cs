
namespace MTS.Services;

public class FirebaseAnalyticsService
{
    private readonly HttpClient _httpClient;
    private const string MeasurementId = "G-XXXXXXX";
    private const string ApiSecret = "YOUR_API_SECRET";
    public static FirebaseAnalyticsService Instance { get; private set; } = new (new HttpClient());
    public FirebaseAnalyticsService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    public async Task LogEventAsync(string clientId, string eventName, Dictionary<string, object> parameters)
    {
        var url = $"https://www.google-analytics.com/mp/collect" +
                  $"?measurement_id={MeasurementId}&api_secret={ApiSecret}";

        var payload = new
        {
            client_id = clientId,
            events = new[]
            {
                new
                {
                    name = eventName,
                    parameters = parameters
                }
            }
        };

        var response = await _httpClient.PostAsJsonAsync(url, payload);
        response.EnsureSuccessStatusCode();
    }
}