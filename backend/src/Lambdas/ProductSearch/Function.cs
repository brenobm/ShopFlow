using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
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
    private static readonly IAmazonDynamoDB _dynamoDb = new AmazonDynamoDBClient();
    private readonly string _metricsTableName;

    public Function()
    {
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("ShopFlow-Backend/1.0");
        _metricsTableName = Environment.GetEnvironmentVariable("METRICS_TABLE_NAME") ?? "ShopFlow_BarcodeMetrics";
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
        
        string status = "Miss";
        string? productName = null;

        try
        {
            string url = $"https://world.openfoodfacts.org/api/v0/product/{barcode}.json";
            string json = await _httpClient.GetStringAsync(url);
            
            // Log Payload (truncated if too massive, but usually fine)
            context.Logger.LogLine($"OpenFoodFacts Response: {json}");

            var response = JsonConvert.DeserializeObject<OpenFoodFactsResponse>(json);
            
            if (response != null && response.Status == 1 && response.Product != null)
            {
                status = "Hit";
                productName = response.Product.ProductName;

                var result = new ProductResult
                {
                    Name = response.Product.ProductName,
                    Category = MapCategory(response.Product.CategoriesTags),
                    ImageUrl = response.Product.ImageUrl
                };
                
                 await LogMetricAsync(barcode, status, productName, context);
                 return result;
            }
            
             context.Logger.LogLine($"Product not found for barcode: {barcode}");
             await LogMetricAsync(barcode, status, null, context);
             return null;

        }
        catch (Exception ex)
        {
            context.Logger.LogLine($"Exception during search: {ex.Message}");
            context.Logger.LogLine(ex.StackTrace);
            await LogMetricAsync(barcode, "Error", null, context);
            return null;
        }
    }

    private static string MapCategory(List<string>? tags)
    {
        if (tags == null || tags.Count == 0)
            return "Pantry";

        var joined = string.Join(" ", tags).ToLowerInvariant();

        if (ContainsAny(joined, "dairy", "milk", "cheese", "yogurt", "egg", "butter", "cream"))
            return "Dairy & Eggs";
        if (ContainsAny(joined, "meat", "poultry", "beef", "pork", "chicken", "turkey", "fish", "seafood", "lamb"))
            return "Meat & Poultry";
        if (ContainsAny(joined, "beverage", "drink", "juice", "water", "soda", "coffee", "tea", "wine", "beer", "spirits"))
            return "Beverages";
        if (ContainsAny(joined, "bread", "bakery", "pastry", "cake", "biscuit", "cookie", "cracker"))
            return "Bakery";
        if (ContainsAny(joined, "frozen"))
            return "Frozen Foods";
        if (ContainsAny(joined, "fruit", "vegetable", "produce", "fresh", "salad", "herb"))
            return "Produce";
        if (ContainsAny(joined, "household", "cleaning", "detergent", "laundry", "dish"))
            return "Household";
        if (ContainsAny(joined, "personal-care", "cosmetic", "hygiene", "shampoo", "soap", "toiletry"))
            return "Personal Care";

        return "Pantry";
    }

    private static bool ContainsAny(string source, params string[] terms)
        => terms.Any(t => source.Contains(t));

    private async Task LogMetricAsync(string barcode, string status, string? productName, ILambdaContext context)
    {
        try 
        {
            var request = new PutItemRequest
            {
                TableName = _metricsTableName,
                Item = new Dictionary<string, AttributeValue>
                {
                    { "barcode", new AttributeValue { S = barcode } },
                    { "timestamp", new AttributeValue { S = DateTime.UtcNow.ToString("O") } },
                    { "status", new AttributeValue { S = status } },
                    { "updatedAt", new AttributeValue { S = DateTime.UtcNow.ToString("O") } }
                }
            };

            if (!string.IsNullOrEmpty(productName))
            {
                request.Item.Add("productName", new AttributeValue { S = productName });
            }

            await _dynamoDb.PutItemAsync(request);
        }
        catch (Exception ex)
        {
             context.Logger.LogLine($"Failed to log metric to DynamoDB: {ex.Message}");
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

    [Newtonsoft.Json.JsonProperty("categories_tags")]
    public List<string>? CategoriesTags { get; set; }
}
