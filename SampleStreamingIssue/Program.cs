using System.Net.Http.Headers;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace SampleStreamingIssue;

public static class Program
{
    private const string EndpointUrl = "";
    private const string EndpointApiKey = "";
    private const string DeploymentName = "";
    
    private static readonly HttpClient Client = GenerateClient();

    public static async Task Main(string[] args)
    {
        do
        {
            Console.WriteLine("Enter your message");
            var userInput = Console.ReadLine() ?? "";
            var payload = GeneratePayload(userInput);
            await Run(payload);
        } while (true);
    }

    private static StringContent GeneratePayload(string userInput)
    {
        var request = new EndpointRequest
        {
            chat_input = userInput,
            chat_history = """chat_history":[]"""
        };

        var serializedRequest = JsonSerializer.Serialize(request);
        var content = new StringContent(serializedRequest);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        content.Headers.Add("azureml-model-deployment", DeploymentName);
        return content;
    }
    
    private static HttpClient GenerateClient()
    {
        var handler = new HttpClientHandler
        {
            ClientCertificateOptions = ClientCertificateOption.Manual,
        };

        return new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(180),
            BaseAddress = new Uri(EndpointUrl),
            DefaultRequestHeaders =
            {
                Authorization = new AuthenticationHeaderValue("Bearer", EndpointApiKey),
                Accept = { new MediaTypeWithQualityHeaderValue("text/event-stream") }
            }
        };
    }
    
    private static async Task Run(HttpContent httpContent)
    {
        var requestResponse = await Client.PostAsync("", httpContent);

        var responseStream = await requestResponse.Content.ReadAsStreamAsync();
        using var streamReader = new StreamReader(responseStream);

        while (!streamReader.EndOfStream)
        {
            var response = await streamReader.ReadLineAsync();
            if (response is null) continue;

            var formattedJson = response.Substring(response.IndexOf(':') + 1);
            if (formattedJson == string.Empty) continue;

            var endpointResponse = JsonConvert.DeserializeObject<EndpointResponse>(formattedJson);
            if (endpointResponse == null) continue;

            Console.Write(endpointResponse.ChatOutput);
        }

        Console.WriteLine("\n ******** END OF CONTENT ********");
    }
}

public class EndpointRequest
{
    public required string chat_input { get; set; }
    public required string chat_history { get; set; }
}

public class EndpointResponse
{
    [JsonProperty(PropertyName = "chat_output")]
    public string? ChatOutput { get; set; }

    [JsonProperty(PropertyName = "context")]
    public string? Context { get; set; }
}