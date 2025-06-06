using ChatCompletion.Hubs;
using Microsoft.SemanticKernel;

var builder = WebApplication.CreateBuilder(args);

var openAIApiKey = builder.Configuration["OpenAI:ApiKey"];

var modelId = builder.Configuration["OpenAI:ModelId"] ?? throw new Exception("Model Id is missing");

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddCors(x => 
{
    x.AddPolicy("maui_dev", x =>
    {
        x.AllowAnyMethod();
        x.AllowAnyOrigin();
        x.AllowAnyMethod();
    });
});

builder.Services.AddOpenAIChatCompletion(modelId, openAIApiKey);
builder.Services.AddSignalR();
builder.Services.AddTransient((serviceProvider) =>
{
    return new Kernel(serviceProvider);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseCors("maui_dev");
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapHub<AIStreamingHub>("/aiHub");
app.Run();
