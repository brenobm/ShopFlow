using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using BrenoMachado.ShopFlow.Models;
using System.Net.Http.Headers;

namespace BrenoMachado.ShopFlow.Services
{
    public class GraphQLService : IGraphQLService
    {
        private readonly GraphQLHttpClient _client;
        private readonly IAuthService _authService;

        public GraphQLService(IAuthService authService)
        {
            _authService = authService;
            
            // Initialize GraphQL Client
            var options = new GraphQLHttpClientOptions
            {
                EndPoint = new Uri(AwsConfig.GraphqlEndpoint)
            };
            
            _client = new GraphQLHttpClient(options, new NewtonsoftJsonSerializer());
        }

        private void SetAuthorizationHeader()
        {
            var token = _authService.GetIdToken();
            if (!string.IsNullOrEmpty(token))
            {
                _client.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(token);
            }
        }

        public async Task<List<ShoppingList>> GetListsAsync()
        {
            SetAuthorizationHeader();
            var request = new GraphQLRequest
            {
                Query = @"
                query GetLists {
                    getLists {
                        id
                        name
                        createdAt
                        isDefault
                    }
                }"
            };

            var response = await _client.SendQueryAsync<GetListsResponse>(request);
            if (response.Errors != null && response.Errors.Any())
            {
                throw new Exception($"GraphQL Error: {response.Errors[0].Message}");
            }

            return response.Data.GetLists ?? new List<ShoppingList>();
        }

        public async Task<ShoppingList> CreateListAsync(string name, bool isDefault = false)
        {
            SetAuthorizationHeader();
            var request = new GraphQLRequest
            {
                Query = @"
                mutation CreateList($name: String!, $isDefault: Boolean) {
                    createList(name: $name, isDefault: $isDefault) {
                        id
                        name
                        createdAt
                        isDefault
                    }
                }",
                Variables = new { name, isDefault }
            };

            var response = await _client.SendMutationAsync<CreateListResponse>(request);
            if (response.Errors != null && response.Errors.Any())
            {
                throw new Exception($"GraphQL Error: {response.Errors[0].Message}");
            }
            
            return response.Data.CreateList;
        }

        public async Task<ShoppingList> UpdateListAsync(string id, string name, bool isDefault)
        {
             SetAuthorizationHeader();
             var request = new GraphQLRequest
            {
                Query = @"
                mutation UpdateList($id: ID!, $name: String, $isDefault: Boolean) {
                    updateList(id: $id, name: $name, isDefault: $isDefault) {
                        id
                        name
                        createdAt
                        isDefault
                    }
                }",
                Variables = new { id, name, isDefault }
            };

            var response = await _client.SendMutationAsync<UpdateListResponse>(request);
            if (response.Errors != null && response.Errors.Any()) throw new Exception(response.Errors[0].Message);
            
            return response.Data.UpdateList;
        }

        public async Task<ShoppingList> DeleteListAsync(string id)
        {
             SetAuthorizationHeader();
             var request = new GraphQLRequest
            {
                Query = @"
                mutation DeleteList($id: ID!) {
                    deleteList(id: $id) {
                        id
                    }
                }",
                Variables = new { id }
            };

            var response = await _client.SendMutationAsync<DeleteListResponse>(request);
             if (response.Errors != null && response.Errors.Any())
            {
                throw new Exception($"GraphQL Error: {response.Errors[0].Message}");
            }
            return new ShoppingList { Id = id };
        }

        public async Task<ShoppingList> ShareListAsync(string id, string email)
        {
             SetAuthorizationHeader();
             var request = new GraphQLRequest
             {
                 Query = @"
                 mutation ShareList($id: ID!, $email: String!) {
                     shareList(id: $id, email: $email) {
                         id
                         name
                         owner
                         members
                     }
                 }",
                 Variables = new { id, email }
             };

             var response = await _client.SendMutationAsync<ShareListResponse>(request);
             if (response.Errors != null && response.Errors.Any())
                throw new Exception(response.Errors[0].Message);
             
             return response.Data.ShareList;
        }

        public async Task<List<ShoppingItem>> GetItemsAsync(string listId)
        {
             SetAuthorizationHeader();
             var request = new GraphQLRequest
            {
                Query = @"
                query GetItems($listId: ID!) {
                    getItems(listId: $listId) {
                        id
                        listId
                        name
                        quantity
                        unit
                        category
                        price
                        isPurchased
                    }
                }",
                Variables = new { listId }
            };

            var response = await _client.SendQueryAsync<GetItemsResponse>(request);
            if (response.Errors != null && response.Errors.Any())
            {
                 throw new Exception($"GraphQL Error: {response.Errors[0].Message}");
            }
            
            return response.Data.GetItems ?? new List<ShoppingItem>();
        }

        public async Task<ShoppingItem> AddItemAsync(string listId, string id, string name, double quantity, string unit, string category, double? price = null)
        {
             SetAuthorizationHeader();
             var request = new GraphQLRequest
            {
                Query = @"
                mutation AddItem($id: ID, $listId: ID!, $name: String!, $quantity: Float, $unit: String, $category: String, $price: Float) {
                    addItem(id: $id, listId: $listId, name: $name, quantity: $quantity, unit: $unit, category: $category, price: $price) {
                        id
                        listId
                        name
                        quantity
                        unit
                        category
                        price
                        isPurchased
                    }
                }",
                Variables = new { id, listId, name, quantity, unit, category, price }
            };

            var response = await _client.SendMutationAsync<AddItemResponse>(request);
            if (response.Errors != null && response.Errors.Any()) throw new Exception(response.Errors[0].Message);
            return response.Data.AddItem;
        }

        public async Task<ShoppingItem> UpdateItemAsync(ShoppingItem item)
        {
             SetAuthorizationHeader();
             var request = new GraphQLRequest
            {
                Query = @"
                mutation UpdateItem($listId: ID!, $id: ID!, $name: String, $quantity: Float, $unit: String, $category: String, $price: Float, $isPurchased: Boolean) {
                    updateItem(listId: $listId, id: $id, name: $name, quantity: $quantity, unit: $unit, category: $category, price: $price, isPurchased: $isPurchased) {
                        id
                        isPurchased
                        price
                        name
                        category
                    }
                }",
                Variables = new { listId = item.ListId, id = item.Id, name = item.Name, quantity = item.Quantity, unit = item.Unit, category = item.Category, price = item.Price, isPurchased = item.IsPurchased }
            };
             var response = await _client.SendMutationAsync<UpdateItemResponse>(request);
             if (response.Errors != null && response.Errors.Any()) throw new Exception(response.Errors[0].Message);
             return item;
        }

        public async Task<ShoppingItem> DeleteItemAsync(string listId, string id)
        {
             SetAuthorizationHeader();
              var request = new GraphQLRequest
            {
                Query = @"
                mutation DeleteItem($listId: ID!, $id: ID!) {
                    deleteItem(listId: $listId, id: $id) {
                        id
                    }
                }",
                Variables = new { listId, id }
            };
            await _client.SendMutationAsync<DeleteItemResponse>(request);
            return new ShoppingItem { Id = id };
        }
        public async Task<GlobalProduct> GetGlobalProductAsync(string barcode)
        {
             SetAuthorizationHeader();
             var request = new GraphQLRequest
            {
                Query = @"
                query GetGlobalProduct($barcode: String!) {
                    getGlobalProduct(barcode: $barcode) {
                        barcode
                        name
                        category
                    }
                }",
                Variables = new { barcode }
            };

            var response = await _client.SendQueryAsync<GetGlobalProductResponse>(request);
            // Return null if not found or errors (common for lookup)
            if (response.Errors != null && response.Errors.Any()) return null;
            if (response.Data.GetGlobalProduct == null) return null;

            return response.Data.GetGlobalProduct;
        }

        public async Task<GlobalProduct> CreateGlobalProductAsync(string barcode, string name, string category)
        {
             SetAuthorizationHeader();
             var request = new GraphQLRequest
            {
                Query = @"
                mutation CreateGlobalProduct($barcode: String!, $name: String!, $category: String) {
                    createGlobalProduct(barcode: $barcode, name: $name, category: $category) {
                        barcode
                        name
                        category
                    }
                }",
                Variables = new { barcode, name, category }
            };

            var response = await _client.SendMutationAsync<CreateGlobalProductResponse>(request);
            if (response.Errors != null && response.Errors.Any()) throw new Exception(response.Errors[0].Message);
            
            return response.Data.CreateGlobalProduct;
        }

        public async Task RegisterDeviceAsync(string token, string platform)
        {
             SetAuthorizationHeader();
             var request = new GraphQLRequest
            {
                Query = @"
                mutation RegisterDevice($token: String!, $platform: String) {
                    registerDevice(token: $token, platform: $platform) {
                        userId
                        token
                    }
                }",
                Variables = new { token, platform }
            };

            var response = await _client.SendMutationAsync<RegisterDeviceResponse>(request);
            if (response.Errors != null && response.Errors.Any()) throw new Exception(response.Errors[0].Message);
        }

        public async Task<ProductSearchDto> ProductSearchAsync(string barcode)
        {
             SetAuthorizationHeader();
             var request = new GraphQLRequest
            {
                Query = @"
                query ProductSearch($barcode: String!) {
                    productSearch(barcode: $barcode) {
                        name
                        category
                        imageUrl
                    }
                }",
                Variables = new { barcode }
            };

            var response = await _client.SendQueryAsync<ProductSearchResponse>(request);
            if (response.Errors != null && response.Errors.Any()) return null; // Log?
            if (response.Data.ProductSearch == null) return null;

            return response.Data.ProductSearch;
        }

        public async Task<SmartParseResult> ParseItemAsync(string text)
        {
             SetAuthorizationHeader();
             var request = new GraphQLRequest
            {
                Query = @"
                query ParseItem($text: String!) {
                    parseItem(text: $text) {
                        name
                        quantity
                        unit
                        category
                        emoji
                    }
                }",
                Variables = new { text }
            };

            var response = await _client.SendQueryAsync<ParseItemResponse>(request);
            if (response.Errors != null && response.Errors.Any()) 
            {
                // Log error but don't crash, return null so SmartService knows it failed
                FileLoggerService.LogStatic($"[GraphQLService] ParseItem Error: {response.Errors[0].Message}");
                return null;
            }
            if (response.Data.ParseItem == null) return null;

            return response.Data.ParseItem;
        }
    }

    // DTOs for GraphQL Responses
    public class GetListsResponse { [JsonProperty("getLists")] public List<ShoppingList> GetLists { get; set; } }
    public class CreateListResponse { [JsonProperty("createList")] public ShoppingList CreateList { get; set; } }
    public class UpdateListResponse { [JsonProperty("updateList")] public ShoppingList UpdateList { get; set; } }
    public class DeleteListResponse { [JsonProperty("deleteList")] public ShoppingList DeleteList { get; set; } }
    public class ShareListResponse { [JsonProperty("shareList")] public ShoppingList ShareList { get; set; } }
    public class GetItemsResponse { [JsonProperty("getItems")] public List<ShoppingItem> GetItems { get; set; } }
    public class AddItemResponse { [JsonProperty("addItem")] public ShoppingItem AddItem { get; set; } }
    public class UpdateItemResponse { [JsonProperty("updateItem")] public ShoppingItem UpdateItem { get; set; } }
    public class DeleteItemResponse { [JsonProperty("deleteItem")] public ShoppingItem DeleteItem { get; set; } }
    public class GetGlobalProductResponse { [JsonProperty("getGlobalProduct")] public GlobalProduct GetGlobalProduct { get; set; } }
    public class CreateGlobalProductResponse { [JsonProperty("createGlobalProduct")] public GlobalProduct CreateGlobalProduct { get; set; } }
    public class RegisterDeviceResponse { [JsonProperty("registerDevice")] public object RegisterDevice { get; set; } }
    public class ProductSearchResponse { [JsonProperty("productSearch")] public ProductSearchDto ProductSearch { get; set; } }
    public class ParseItemResponse { [JsonProperty("parseItem")] public SmartParseResult ParseItem { get; set; } }
}
