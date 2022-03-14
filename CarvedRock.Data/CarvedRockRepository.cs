using CarvedRock.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace CarvedRock.Data
{
    public class CarvedRockRepository : ICarvedRockRepository
    {
        private readonly LocalContext _ctx;
        private readonly ILogger<CarvedRockRepository> _logger;
        private readonly ILogger _loggerFactory;

        // Normally, you would never use both ILogger and ILoggerFactory
        public CarvedRockRepository(LocalContext ctx, ILogger<CarvedRockRepository> logger,
            ILoggerFactory loggerFactory)
        {
            _ctx = ctx;
            _logger = logger;

            // Create logger with custom category, instead of CarvedRock.Data
            _loggerFactory = loggerFactory.CreateLogger("DataAccessLayer");
        }
        public async Task<List<Product>> GetProductsAsync(string category)
        {
            _logger.LogInformation("Getting products in repository for {category}", category);

            return await _ctx.Products.Where(p => p.Category == category || category == "all").ToListAsync();
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            return await _ctx.Products.FindAsync(id);
        }

        public List<Product> GetProducts(string category)
        {
            return _ctx.Products.Where(p => p.Category == category || category == "all").ToList();
        }

        public Product? GetProductById(int id)
        {
            var timer = new Stopwatch();
            timer.Start();

            var product = _ctx.Products.Find(id);
            timer.Stop();

            _logger.LogDebug("Querying products for {id} finished in {milliseconds} milliseconds", 
                id, timer.ElapsedMilliseconds);

            _loggerFactory.LogInformation("(F) Querying products for {id} finished in {ticks} ticks", 
                id, timer.ElapsedTicks);


            return product;
        }
    }
}
