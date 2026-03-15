namespace RaceControl.Middleware;

public class RobotsTxtMiddleware(RequestDelegate next, IWebHostEnvironment env)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/robots.txt"))
        {
           var robotTxtInfo = env.ContentRootFileProvider.GetFileInfo("robots.txt");
           var output = await File.ReadAllTextAsync(robotTxtInfo.PhysicalPath);
           
           context.Response.ContentType = "text/plain";
           await context.Response.WriteAsync(output);
        }
        else
        {
            await next(context);
        }
    }
}

public static class RobotsTxtMiddlewareExtensions
{
    public static IApplicationBuilder UseRobotsTxt(this IApplicationBuilder builder, IWebHostEnvironment env)
    {
        return builder.MapWhen(
            ctx => ctx.Request.Path.StartsWithSegments("/robots.txt"),
            b => b.UseMiddleware<RobotsTxtMiddleware>(env)
        );
    }
}