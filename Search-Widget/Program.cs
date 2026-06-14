using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Identity.Web.UI;
using Microsoft.Identity.Web;

//var builder = WebApplication.CreateBuilder(args);

var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);

//commented on 13/06/2026 as part of testing without authentication
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
.AddMicrosoftIdentityWebApp(builder.Configuration);

//commented on 13/06/2026 as part of testing without authentication
// Add services to the container.
builder.Services
    .AddControllersWithViews(options =>
    {
        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
        options.Filters.Add(new AuthorizeFilter(policy));
    })
    .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);

//commented on 13/06/2026 as part of testing without authentication
builder.Services.AddRazorPages()
    .AddMvcOptions(options =>
    {
        var policy = new AuthorizationPolicyBuilder()
                         .RequireAuthenticatedUser()
                         .Build();
        options.Filters.Add(new AuthorizeFilter(policy));
    })
    .AddMicrosoftIdentityUI();

//Added on 13/06/2026 as part of testing without authentication
//builder.Services.AddControllersWithViews();
//builder.Services.AddRazorPages();



var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();

app.UseRouting();

//commented on 13/06/2026 as part of testing without authentication
app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
