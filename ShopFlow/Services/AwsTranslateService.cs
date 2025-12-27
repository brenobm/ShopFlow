using Amazon.Translate;
using Amazon.Translate.Model;
using Microsoft.Extensions.Logging;

namespace BrenoMachado.ShopFlow.Services
{
    public class AwsTranslateService : ITranslationService
    {
        private readonly ILogger<AwsTranslateService> _logger;
        private readonly AmazonTranslateClient _translateClient;

        public AwsTranslateService(ILogger<AwsTranslateService> logger, IAuthService authService)
        {
            _logger = logger;
            // Assuming we use the same region as other AWS services
            // Note: Securely we should use the credentials from AuthService or Cognito
            // For now, initializing with Anonymous/Cognito credentials handled by the SDK if configured globally
            // or we might need to pass credentials explicitly if AuthService holds them.
            // Using logic similar to GraphQLService/VoiceService setup.
            
             // We'll use the fallback region or specific region
             _translateClient = new AmazonTranslateClient(Amazon.RegionEndpoint.USEast1); 
        }

        public async Task<string> TranslateTextAsync(string text, string sourceLang, string targetLang)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;

            try
            {
                var request = new TranslateTextRequest
                {
                    Text = text,
                    SourceLanguageCode = sourceLang,
                    TargetLanguageCode = targetLang
                };

                var response = await _translateClient.TranslateTextAsync(request);
                return response.TranslatedText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Translation failed");
                return text; // Fallback to original
            }
        }
    }
}
