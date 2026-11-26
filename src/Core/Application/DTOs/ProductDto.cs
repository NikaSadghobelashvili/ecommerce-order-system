namespace Application.DTOs;

public record ProductDto(
    int Id,
    string Name,
    string Description,
    decimal Price,
    int StockQuantity);

public record CreateProductRequest(
    string Name,
    string Description,
    decimal Price,
    int StockQuantity);
