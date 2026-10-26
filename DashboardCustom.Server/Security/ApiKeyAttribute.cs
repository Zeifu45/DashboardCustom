using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DashboardCustom.Server.Security
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ApiKeyAttribute : Attribute, IAsyncActionFilter
    {
        private const string HeaderName = "X-Api-Key";
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var extractedApiKey))
            {
                context.Result = new UnauthorizedObjectResult(new { error = "Clé API manquante." });
                return;
            }
            var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            var apiKey = configuration["SecuritySettings:ApiKey"];
            if (string.IsNullOrEmpty(apiKey) || !apiKey.Equals(extractedApiKey))
            {
                context.Result = new UnauthorizedObjectResult(new { error = "Clé API invalide." });
                return;
            }
            await next();
        }
    }
}
