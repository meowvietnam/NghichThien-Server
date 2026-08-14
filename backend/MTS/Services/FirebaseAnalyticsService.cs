
namespace MTS.Services;
public class GaEvent
{
    public string name { get; set; } = default!;
    public Dictionary<string, object> @params { get; set; } = default!;
}
public class FirebaseAnalyticsService
{
    private readonly HttpClient _httpClient;
    private const string MeasurementId = "1:5274935319:android:09828abe076a4e73ecbc52";
    private const string ApiSecret = "4ygKXf0LQ-CgozHzaGMrUQ";
    public static FirebaseAnalyticsService Instance { get; private set; } = new (new HttpClient());
    public FirebaseAnalyticsService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    public async Task<string> LogEventAsync(string clientId, string eventName, Dictionary<string, object> parameters)
    {
        var url = $"https://www.google-analytics.com/mp/collect" +
                  $"?measurement_id={MeasurementId}&api_secret={ApiSecret}";

        var payload = new
        {
            client_id = clientId,
            events = new[]
            {
                new GaEvent { name = eventName, @params = parameters }
            }
        };

        var response = await _httpClient.PostAsJsonAsync(url, payload);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}