using CarvedRock.Data.Entities;
using CarvedRock.Domain;
using Microsoft.AspNetCore.Mvc;

namespace CarvedRock.Api.Controllers;

[ApiController]
[Route("[controller]")]
public partial class ProductController : ControllerBase
{
    private readonly ILogger<ProductController> _logger;
    private readonly IProductLogic _productLogic;

    // if you have very high performance requirements, you can use source generators
    // this uses a compiled template rather than parsed/cached
    // to use this, you create a PARTIAL void method with params to log (class needs to become partial as well)
    // LoggerMessage attribute on this method to trigger the source generator
    [LoggerMessage(CarvedRockEvents.GettingProducts, LogLevel.Information, 
        "SourceGenerated: Getting products in API.")]
    partial void LogGettingProducts();
    public ProductController(ILogger<ProductController> logger, IProductLogic productLogic)
    {
        _logger = logger;
        _productLogic = productLogic;
    }

    [HttpGet]
    public async Task<IEnumerable<Product>> Get(string category = "all")
    {
        using (_logger.BeginScope("ScopeCat: {ScopeCat}", category))
        {
            //_logger.LogInformation(CarvedRockEvents.GettingProducts, "Getting products in API");
            LogGettingProducts();
            return await _productLogic.GetProductsForCategoryAsync(category);
        }

        //return _productLogic.GetProductsForCategory(category);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Product), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int id)
    {
        //var product = await _productLogic.GetProductByIdAsync(id);

        _logger.LogDebug("Getting single product in API for {id}", id);

        var product = _productLogic.GetProductById(id);
        if (product != null)
        {
            return Ok(product);
        }

        _logger.LogWarning("No product found for ID: {id}", id);

        return NotFound();
    }
}