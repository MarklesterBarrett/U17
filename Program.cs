WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddComposers()
    .Build();

WebApplication app = builder.Build();

await app.BootUmbracoAsync();

app.MapGet("/css/generated-tokens.css", (Guid? tenant, Site.DesignTokens.IDesignTokenCssCache cache) =>
{
    if (tenant is null || tenant == Guid.Empty)
    {
        return Results.NotFound();
    }

    return Results.Content(cache.GetCss(tenant.Value), "text/css");
});

app.UseUmbraco()
    .WithMiddleware(u =>
    {
        u.UseBackOffice();
        u.UseWebsite();
    })
    .WithEndpoints(u =>
    {
        u.UseBackOfficeEndpoints();
        u.UseWebsiteEndpoints();
    });

await app.RunAsync();
