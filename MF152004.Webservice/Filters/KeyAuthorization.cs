using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MF152004.Webservice.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class KeyAuthorization : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        //check API
        if (context.HttpContext.Request.Headers.TryGetValue("api-key", out var key))
        {
            var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            var configKey = config.GetValue<string>("my_key");

            if (key.Equals(configKey))
            {
                await next();
            }
        }

        context.Result = new UnauthorizedResult();
    }
}