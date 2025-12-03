using PrimeBakes.Shared.Services;
using PrimeBakes.Web.Components;
using PrimeBakes.Web.Services;

using PrimeBakesLibrary.DataAccess;

using Syncfusion.Blazor;

using Toolbelt.Blazor.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(Secrets.SyncfusionLicense);
Dapper.SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());

// Add services to the container.
builder.Services
	.AddSyncfusionBlazor()
	.AddHotKeys2()
	.AddRazorComponents()
	.AddInteractiveServerComponents();

// Add device-specific services used by the PrimeBakes.Shared project
builder.Services.AddSingleton<IFormFactor, FormFactor>();
builder.Services.AddSingleton<IUpdateService, UpdateService>();
builder.Services.AddSingleton<IVibrationService, VibrationService>();
builder.Services.AddSingleton<INotificationService, NotificationService>();

builder.Services.AddScoped<ISaveAndViewService, SaveAndViewService>();
builder.Services.AddScoped<ISoundService, SoundService>();
builder.Services.AddScoped<IDataStorageService, DataStorageService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error", createScopeForErrors: true);
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode()
	.AddAdditionalAssemblies(typeof(PrimeBakes.Shared._Imports).Assembly);

app.Run();
