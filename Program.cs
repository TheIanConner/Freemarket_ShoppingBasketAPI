using Microsoft.EntityFrameworkCore;
using ShoppingBasketAPI.Data;
using ShoppingBasketAPI.Services;
using ShoppingBasketAPI.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Entity Framework with SQLite
builder.Services.AddDbContext<ShoppingBasketContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=ShoppingBasket.db"));

// Add services
builder.Services.AddScoped<IBasketService, BasketService>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Ensure database is created and seed products if needed
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ShoppingBasketContext>();
    context.Database.EnsureCreated();

    // Seed products if none exist
    if (!context.Products.Any())
    {
        var products = new List<Product>
        {
            new Product
            {
                Name = "Laptop",
                Price = 999.99m,
                Description = "High-performance laptop for work and gaming",
                Category = "Electronics",
                IsActive = true
            },
            new Product
            {
                Name = "Smartphone",
                Price = 699.99m,
                Description = "Latest smartphone with advanced features",
                Category = "Electronics",
                IsActive = true
            },
            new Product
            {
                Name = "Wireless Headphones",
                Price = 199.99m,
                Description = "Premium wireless headphones with noise cancellation",
                Category = "Electronics",
                IsActive = true
            },
            new Product
            {
                Name = "Coffee Maker",
                Price = 89.99m,
                Description = "Automatic coffee maker for home use",
                Category = "Home & Kitchen",
                IsActive = true
            },
            new Product
            {
                Name = "Running Shoes",
                Price = 129.99m,
                Description = "Comfortable running shoes for athletes",
                Category = "Sports",
                IsActive = true
            },
            new Product
            {
                Name = "Discounted T-Shirt",
                Price = 29.99m,
                Description = "Comfortable cotton t-shirt",
                Category = "Clothing",
                IsActive = true,
                IsDiscounted = true,
                DiscountPercentage = 15.0m
            },
            new Product
            {
                Name = "Discounted Book",
                Price = 19.99m,
                Description = "Bestselling novel",
                Category = "Books",
                IsActive = true,
                IsDiscounted = true,
                DiscountPercentage = 25.0m
            }
        };
        context.Products.AddRange(products);
        context.SaveChanges();
    }
}

app.Run();
