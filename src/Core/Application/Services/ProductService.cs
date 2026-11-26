using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public class ProductService : IProductService
{
    private const string AllProductsCacheKey = "products:all";
    private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(5);

    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ProductService> _logger;

    public ProductService(
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        ILogger<ProductService> logger)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        // 1. Check Redis Cache first (Cache-Aside Pattern)
        var cachedProducts = await _cacheService.GetAsync<IReadOnlyList<ProductDto>>(AllProductsCacheKey, cancellationToken);
        if (cachedProducts is not null)
        {
            _logger.LogInformation("CACHE HIT: Retrieved all products from Redis.");
            return cachedProducts;
        }

        // 2. Cache Miss -> Fetch from MS SQL Server
        _logger.LogInformation("CACHE MISS: Fetching all products from MS SQL Server.");
        var products = await _productRepository.GetAllAsync(cancellationToken);

        var productDtos = products
            .Select(MapToDto)
            .ToList();

        // 3. Store in Redis for future requests
        await _cacheService.SetAsync(AllProductsCacheKey, productDtos, DefaultCacheDuration, cancellationToken);

        return productDtos;
    }

    public async Task<ProductDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"products:{id}";

        var cachedProduct = await _cacheService.GetAsync<ProductDto>(cacheKey, cancellationToken);
        if (cachedProduct is not null)
        {
            _logger.LogInformation("CACHE HIT: Retrieved product {ProductId} from Redis.", id);
            return cachedProduct;
        }

        _logger.LogInformation("CACHE MISS: Fetching product {ProductId} from MS SQL Server.", id);
        var product = await _productRepository.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            return null;
        }

        var dto = MapToDto(product);
        await _cacheService.SetAsync(cacheKey, dto, DefaultCacheDuration, cancellationToken);

        return dto;
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            StockQuantity = request.StockQuantity
        };

        await _productRepository.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Cache Invalidation: clear the list cache so new product appears immediately
        await _cacheService.RemoveAsync(AllProductsCacheKey, cancellationToken);
        _logger.LogInformation("Product {ProductId} created and '{CacheKey}' invalidated.", product.Id, AllProductsCacheKey);

        return MapToDto(product);
    }

    private static ProductDto MapToDto(Product product) =>
        new(product.Id, product.Name, product.Description, product.Price, product.StockQuantity);
}
