using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SuggestionAppUI.Areas.Identity;
using SuggestionAppUI.Data;

namespace SuggestionAppUI;

public static class RegisterServices
{

    public static void ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddRazorPages();
        builder.Services.AddServerSideBlazor();
        builder.Services.AddMemoryCache();

        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();
        builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<IdentityUser>>();

        builder.Services.AddSingleton<IDbConnection, DbConnection>();
        builder.Services.AddSingleton<ISuggestionData, MongoSuggestionData>();
        builder.Services.AddSingleton<IStatusData, MongoStatusData>();
        builder.Services.AddSingleton<ICategoryData, MongoCategoryData>();
        builder.Services.AddSingleton<IUserData, MongoUserData>();

    }

}
