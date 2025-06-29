# Shopping Basket API

Root folder/
├────────── API/ # The API
├────────── API_Tests/ # Suite of tests around the API services

API:
A REST-based Web API written in .NET 8 for an online shopping basket system with comprehensive features including VAT calculations, discounts, and shipping costs.

## Features

- Add an item to the basket
- Add multiple items to the basket
- Remove an item from the basket
- Add multiple of the same item to the basket
- Get the total cost for the basket (including 20% VAT)
- Get the total cost without VAT
- Add a discounted item to the basket
- Add a discount code to the basket (excluding discounted items)
- Remove a discount code from the basket
- Add shipping cost to the UK (£5.99)
- Add shipping cost to other countries (£15.99)
- SQLite database with automatic creation and seeding
- Swagger/OpenAPI documentation

The project uses:

- Entity Framework Core for data access
- SQLite as the database
- Swagger/OpenAPI for API documentation
- .NET 8 - Latest .NET framework
- ASP.NET Core Web API - REST API framework

General info:

The Swagger endpoint for this on Azure is
https://shoppingbasketdemo-api-e6faeee4ghcmebfw.uksouth-01.azurewebsites.net/swagger/

All basket operations return a `BasketResponse` object (see below) which provides key basket information to the frontend. This would save multiple requests e.g. to get the shipping costs, but I've included those API endpoints anyway for demo purposes.

## Setup and Installation

### Prerequisites

- .NET 8 SDK installed on your machine
- Any code editor (Visual Studio, VS Code, etc.)

1. Clone or download the project

   ```bash
   cd ShoppingBasketAPI
   dotnet restore
   dotnet run
   ```

2. Access the API

   - API Base URL: `https://localhost:7001` or `http://localhost:5000`
   - Swagger UI: `https://localhost:7001/swagger` or `http://localhost:5000/swagger`

## Database

The application uses SQLite with automatic database creation. The database file (`ShoppingBasket.db`) will be created and seeded automatically when you first run the application.

### Sample Products

The seeding creates the following sample products:

- Laptop (£999.99)
- Smartphone (£699.99)
- Wireless Headphones (£199.99)
- Coffee Maker (£89.99)
- Running Shoes (£129.99)
- Discounted T-Shirt (£29.99, 15% off)
- Discounted Book (£19.99, 25% off)

## API Endpoints

### Products

| Method | Endpoint             | Description             |
| ------ | -------------------- | ----------------------- |
| GET    | `/api/products`      | Get all active products |
| GET    | `/api/products/{id}` | Get a specific product  |
| POST   | `/api/products/seed` | Create sample products  |

### Basket Operations

| Method | Endpoint                                    | Description                  |
| ------ | ------------------------------------------- | ---------------------------- |
| GET    | `/api/basket/{sessionId}`                   | Get basket contents          |
| POST   | `/api/basket/{sessionId}/items`             | Add single item to basket    |
| POST   | `/api/basket/{sessionId}/items/multiple`    | Add multiple items to basket |
| DELETE | `/api/basket/{sessionId}/items`             | Remove item from basket      |
| POST   | `/api/basket/{sessionId}/discount`          | Add discount code            |
| DELETE | `/api/basket/{sessionId}/discount`          | Remove discount code         |
| POST   | `/api/basket/{sessionId}/shipping`          | Add shipping information     |
| GET    | `/api/basket/{sessionId}/total`             | Get total cost with VAT      |
| GET    | `/api/basket/{sessionId}/total-without-vat` | Get total cost without VAT   |
| DELETE | `/api/basket/{sessionId}`                   | Clear entire basket          |

## Business Rules

### VAT Calculation

- 20% VAT is applied to the total (excluding shipping)
- VAT is calculated on: Subtotal - Discounts + Shipping

### Discount Codes

Available discount codes:

- `SAVE10` - 10% discount
- `SAVE20` - 20% discount
- `SAVE25` - 25% discount

**Note**: Discount codes only apply to non-discounted items in the basket.

### Shipping Costs

- UK: £5.99
- International: £15.99

### Product Discounts

- Some products have built-in discounts (e.g., T-Shirt 15% off, Book 25% off)
- These discounts are applied at the product level and are not affected by basket discount codes

## Response Format

All basket operations return a `BasketResponse` object:

```json
{
  "id": 1,
  "sessionId": "session123",
  "items": [
    {
      "id": 1,
      "productId": 1,
      "productName": "Laptop",
      "quantity": 2,
      "unitPrice": 999.99,
      "totalPrice": 1999.98,
      "isDiscounted": false,
      "productDiscountPercentage": null
    }
  ],
  "discountCode": "SAVE20",
  "discountPercentage": 20.0,
  "shippingCountry": "UK",
  "shippingCost": 5.99,
  "subtotal": 1999.98,
  "discountAmount": 399.99,
  "totalWithoutVat": 1605.98,
  "vatAmount": 321.2,
  "totalWithVat": 1927.18,
  "createdAt": "2024-01-01T10:00:00Z",
  "updatedAt": "2024-01-01T10:30:00Z"
}
```

## Error Handling

The API returns appropriate HTTP status codes:

- `200 OK` - Successful operation
- `400 Bad Request` - Invalid input or business rule violation
- `404 Not Found` - Resource not found
- `500 Internal Server Error` - Server error

Error responses include a message:

```json
{
  "error": "Product with ID 999 not found or inactive"
}
```

## Development notes

### Project Structure

```
API/
├── Controllers/         # API controllers
├── Data/                # Entity Framework context
├── DTOs/                # Data transfer objects
├── Models/              # Entity models
├── Services/            # Business logic services
├── Program.cs           # Application entry point
├── appsettings.json     # Configuration
└── README.md            # This file
```
