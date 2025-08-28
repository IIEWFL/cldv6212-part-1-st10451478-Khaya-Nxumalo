using ABCRetail.Services;

namespace ABCRetail
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Retrieve the connection string from appsettings.json
            var storageConnectionString = builder.Configuration.GetConnectionString("StorageConnectionString");

            // Ensure the connection string is not null before using it
            if (string.IsNullOrEmpty(storageConnectionString))
            {
                throw new InvalidOperationException("StorageConnectionString is missing from configuration.");
            }

            // Register storage services 
            builder.Services.AddSingleton(new TableStorageService(storageConnectionString, "Customer"));
            builder.Services.AddSingleton(new BlobStorageService(storageConnectionString, "product-images"));
            builder.Services.AddSingleton(new QueueStorageService(storageConnectionString, "abc-log-messages"));
            builder.Services.AddSingleton(new FileShareStorageService(storageConnectionString, "abc-log-files"));


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
