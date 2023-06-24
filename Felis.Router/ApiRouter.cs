using Microsoft.AspNetCore.Builder;

namespace Felis.Router;

internal abstract class ApiRouter
{
    /// <summary>
    /// Init routers
    /// </summary>
    /// <param name="app"></param>
    public abstract void Init(WebApplication app);
}