using Amazon.Lambda.Core;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ProductSearch;

public class Function
{
    private static readonly HttpClient _httpClient = new HttpClient();

    public Function()
    {
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("ShopFlow-Backend/1.0");
    }

    public async Task<ProductResult?> FunctionHandler(Input input, ILambdaContext context)
    {
        string? barcode = input.Barcode; // Mapping from AppSync arguments
        if (string.IsNullOrEmpty(barcode))
        {
            context.Logger.LogLine("Error: Barcode is null or empty.");
            return null;
        }

        context.Logger.LogLine($"Search Request: {barcode}");

        try
        {
            string url = $"https://world.openfoodfacts.org/api/v0/product/{barcode}.json";
            string json = await _httpClient.GetStringAsync(url);
            
            // Log Payload (truncated if too massive, but usually fine)
            context.Logger.LogLine($"OpenFoodFacts Response: {json}");

            var response = JsonConvert.DeserializeObject<OpenFoodFactsResponse>(json);
            
            if (response != null && response.Status == 1 && response.Product != null)
            {
                var result = new ProductResult
                {
                    Name = response.Product.ProductName,
                    Category = "Groceries", // Mapping logic could be improved here
                    ImageUrl = response.Product.ImageUrl
                };
                 return result;
            }
            
             context.Logger.LogLine($"Product not found for barcode: {barcode}");
             return null;

        }
        catch (Exception ex)
        {
            context.Logger.LogLine($"Exception during search: {ex.Message}");
            context.Logger.LogLine(ex.StackTrace);
            return null;
        }
    }
}

public class Input 
{
    [JsonPropertyName("barcode")]
    public string? Barcode { get; set; }
}

public class ProductResult
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("category")]
    public string Category { get; set; }

    [JsonPropertyName("imageUrl")]
    public string ImageUrl { get; set; }
}

// Internal classes for JSON Deserialization
public class OpenFoodFactsResponse
{
    public int Status { get; set; }
    public ProductData Product { get; set; }
}

public class ProductData
{
    public string ProductName { get; set; }
    public string ImageUrl { get; set; }
}
