using BrenoMachado.ShopFlow.Models;
using BrenoMachado.ShopFlow.Repositories;
using BrenoMachado.ShopFlow.Resources.Strings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BrenoMachado.ShopFlow.Services
{
    public class BarcodeWorkflowService : IBarcodeWorkflowService
    {
        private readonly IDatabaseService _databaseService;
        private readonly IProductService _productService;
        private readonly IGraphQLService _graphQLService;
        private readonly ITranslationService _translationService;
        private readonly IDialogService _dialogService;
        private readonly ISmartService _smartService;
        private readonly IShoppingRepository _shoppingRepository;

        public BarcodeWorkflowService(
            IDatabaseService databaseService,
            IProductService productService,
            IGraphQLService graphQLService,
            ITranslationService translationService,
            IDialogService dialogService,
            ISmartService smartService,
            IShoppingRepository shoppingRepository)
        {
            _databaseService = databaseService;
            _productService = productService;
            _graphQLService = graphQLService;
            _translationService = translationService;
            _dialogService = dialogService;
            _smartService = smartService;
            _shoppingRepository = shoppingRepository;
        }

        public async Task ExecuteScanWorkflowAsync(string listId)
        {
            try
            {
                // 1. Scan Barcode
                string barcode = await BrenoMachado.ShopFlow.Views.ScanPage.ScanAsync(Microsoft.Maui.Controls.Shell.Current.Navigation);
                if (string.IsNullOrWhiteSpace(barcode)) return;

                string productName = null;
                string category = "Scanned";

                // 2. Lookup Local Database
                var cached = await _databaseService.GetCachedProductAsync(barcode);
                if (cached != null)
                {
                    productName = cached.Name;
                    category = cached.Category ?? category;
                }
                else
                {
                    // 3. Lookup Online (OpenFoodFacts)
                    var offProduct = await _productService.LookupProductAsync(barcode);
                    bool foundOnline = offProduct != null && !string.IsNullOrWhiteSpace(offProduct.Name);
                    
                    await _databaseService.LogBarcodeSearchAsync(barcode, foundOnline);

                    if (foundOnline)
                    {
                        productName = offProduct.Name;
                        category = offProduct.Category ?? category;
                        
                        await _databaseService.SaveCachedProductAsync(new CachedProduct 
                        {
                            Barcode = barcode,
                            Name = productName,
                            Category = category,
                            LastUpdated = DateTime.UtcNow
                        });
                    }
                }

                // Global Product Fallback
                if (string.IsNullOrWhiteSpace(productName))
                {
                     var globalProduct = await _graphQLService.GetGlobalProductAsync(barcode);
                    if (globalProduct != null)
                    {
                        productName = globalProduct.Name;
                        category = globalProduct.Category ?? category;
                         await _databaseService.SaveCachedProductAsync(new CachedProduct 
                        {
                            Barcode = barcode,
                            Name = productName,
                            Category = category,
                            LastUpdated = DateTime.UtcNow
                        });
                    }
                }

                // 5. Not Found anywhere? -> Register
                if (string.IsNullOrWhiteSpace(productName))
                {
                    bool register = await _dialogService.DisplayAlertAsync(AppResources.Warning, AppResources.Msg_ProductNotFoundRegister, AppResources.Yes, AppResources.No);
                    if (!register) return;

                    productName = await _dialogService.DisplayPromptAsync("Register Product", "Enter product name:");
                    if (string.IsNullOrWhiteSpace(productName)) return;

                    try 
                    {
                        await _graphQLService.CreateGlobalProductAsync(barcode, productName, category);
                        await _databaseService.SaveCachedProductAsync(new CachedProduct 
                        {
                            Barcode = barcode,
                            Name = productName,
                            Category = category,
                            LastUpdated = DateTime.UtcNow
                        });
                    }
                    catch (Exception ex)
                    {
                        FileLoggerService.LogStatic($"Failed to save global product: {ex.Message}");
                    }
                }
                
                // 6. Smart Matching
                bool matchFound = await SmartMatchAndMarkAsync(productName, listId);

                if (matchFound)
                {
                     await _dialogService.DisplayAlertAsync("Match!", string.Format(AppResources.Msg_MarkedBought, productName), "OK");
                }
                else
                {
                    // 7. Add if new
                    bool add = await _dialogService.DisplayAlertAsync(AppResources.AddItem, string.Format(AppResources.Msg_AddProductToList, productName), AppResources.Yes, AppResources.No);
                    if (add)
                    {
                        var parsed = _smartService.ParseItemInput(productName);
                        var newItem = new ShoppingItem { ListId = listId, Name = parsed.Name, Quantity = parsed.Quantity, Unit = parsed.Unit, Category = category };
                        await _shoppingRepository.SaveItemAsync(newItem);
                    }
                }
            }
            catch (Exception ex)
            {
                FileLoggerService.LogStatic($"Scan Error: {ex}");
                await _dialogService.DisplayAlertAsync(AppResources.Error, string.Format(AppResources.Msg_ScanError, ex.Message), "OK");
            }
        }

        private async Task TogglePurchasedAsync(ShoppingItem item)
        {
            if (item == null) return;
            item.IsPurchased = !item.IsPurchased;
            item.IsDirty = true;
            await _shoppingRepository.SaveItemAsync(item);
        }

        private async Task<bool> SmartMatchAndMarkAsync(string scannedName, string listId)
        {
            var candidates = new List<string> { scannedName };
            
            var pt = await _translationService.TranslateTextAsync(scannedName, "auto", "pt");
            if (!string.IsNullOrWhiteSpace(pt) && !candidates.Contains(pt)) candidates.Add(pt);
            
            var en = await _translationService.TranslateTextAsync(scannedName, "auto", "en");
            if (!string.IsNullOrWhiteSpace(en) && !candidates.Contains(en)) candidates.Add(en);

             var items = await _graphQLService.GetItemsAsync(listId);
             var unboughtItems = items.Where(i => !i.IsPurchased).ToList();

            foreach (var candidate in candidates)
            {
                 var match = unboughtItems.FirstOrDefault(i => candidate.Contains(i.Name, StringComparison.OrdinalIgnoreCase));
                 
                 if (match != null)
                 {
                     await TogglePurchasedAsync(match);
                     return true;
                 }
                 
                 var matchReverse = unboughtItems.FirstOrDefault(i => i.Name.Contains(candidate, StringComparison.OrdinalIgnoreCase));
                  if (matchReverse != null)
                 {
                     await TogglePurchasedAsync(matchReverse);
                     return true;
                 }
            }

            return false;
        }
    }
}
