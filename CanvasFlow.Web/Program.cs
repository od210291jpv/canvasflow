using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddSession(); // Enable Session

builder.Services.AddHttpClient("ApiUrl", client =>
{
    client.BaseAddress = new Uri("https://192.168.88.68:5000/"); 
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // Add Authentication
app.UseAuthorization();
app.UseSession(); // Enable Session middleware

app.MapRazorPages();

app.Run();
