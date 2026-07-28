using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesManagement.Data;
using SalesManagement.Models;
using SalesManagement.ViewModels;

namespace SalesManagement.Controllers;

public class ProductsController : BaseController
{
    private readonly ApplicationDbContext _context;

    public ProductsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search)
    {
        ViewData["Title"] = "Produtos";
        ViewData["Search"] = search;

        var query = _context.Products
            .Include(p => p.Prices)
            .Where(p => p.UserId == GetCurrentUserId())
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim().ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(search) ||
                p.Code.ToLower().Contains(search));
        }

        var products = await query
            .OrderBy(p => p.Name)
            .ToListAsync();

        return View(products);
    }

    public IActionResult Create()
    {
        ViewData["Title"] = "Novo Produto";
        return View(new ProductViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Novo Produto";
            return View(model);
        }

        if (await _context.Products.AnyAsync(p => p.Code == model.Code && p.UserId == GetCurrentUserId()))
        {
            ModelState.AddModelError("Code", "Já existe um produto com este código.");
            ViewData["Title"] = "Novo Produto";
            return View(model);
        }

        var product = new Product
        {
            Name = model.Name,
            Code = model.Code,
            IsActive = model.IsActive,
            UserId = GetCurrentUserId()
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        if (model.Prices != null && model.Prices.Any())
        {
            foreach (var priceVm in model.Prices)
            {
                var barcode = GenerateBarcode(model.Code, priceVm.Price);
                if (await _context.ProductPrices.AnyAsync(pp => pp.Barcode == barcode))
                    continue;

                _context.ProductPrices.Add(new ProductPrice
                {
                    ProductId = product.Id,
                    Price = priceVm.Price,
                    Barcode = barcode
                });
            }
            await _context.SaveChangesAsync();
        }

        TempData["Success"] = $"Produto '{product.Name}' cadastrado com sucesso!";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var product = await _context.Products
            .Include(p => p.Prices)
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == GetCurrentUserId());

        if (product == null) return NotFound();

        ViewData["Title"] = "Editar Produto";

        var vm = new ProductViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Code = product.Code,
            IsActive = product.IsActive,
            Prices = product.Prices.Select(pp => new ProductPriceViewModel
            {
                Id = pp.Id,
                Price = pp.Price,
                Barcode = pp.Barcode
            }).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProductViewModel model)
    {
        if (id != model.Id) return NotFound();

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Editar Produto";
            return View(model);
        }

        if (await _context.Products.AnyAsync(p => p.Code == model.Code && p.Id != id && p.UserId == GetCurrentUserId()))
        {
            ModelState.AddModelError("Code", "Já existe um produto com este código.");
            ViewData["Title"] = "Editar Produto";
            return View(model);
        }

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == GetCurrentUserId());

        if (product == null) return NotFound();

        product.Name = model.Name;
        product.Code = model.Code;
        product.IsActive = model.IsActive;

        _context.Update(product);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Produto '{product.Name}' atualizado com sucesso!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> AddPrice(int productId, string price)
    {
        if (!decimal.TryParse(price, System.Globalization.NumberStyles.Currency,
            new System.Globalization.CultureInfo("pt-BR"), out decimal parsedPrice))
        {
            return Json(new { success = false, message = "Preço inválido. Use o formato 39,90" });
        }

        if (parsedPrice <= 0)
            return Json(new { success = false, message = "O preço deve ser maior que zero." });

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == productId && p.UserId == GetCurrentUserId());

        if (product == null)
            return Json(new { success = false, message = "Produto não encontrado." });

        if (await _context.ProductPrices.AnyAsync(pp => pp.ProductId == productId && pp.Price == parsedPrice))
            return Json(new { success = false, message = "Este preço já existe para este produto." });

        var barcode = GenerateBarcode(product.Code, parsedPrice);

        if (await _context.ProductPrices.AnyAsync(pp => pp.Barcode == barcode))
            return Json(new { success = false, message = "Código de barras já existe." });

        var productPrice = new ProductPrice
        {
            ProductId = productId,
            Price = parsedPrice,
            Barcode = barcode
        };

        _context.ProductPrices.Add(productPrice);
        await _context.SaveChangesAsync();

        return Json(new { success = true, price = new { id = productPrice.Id, price = productPrice.Price, barcode = productPrice.Barcode } });
    }

    [HttpPost]
    public async Task<IActionResult> RemovePrice(int priceId)
    {
        var price = await _context.ProductPrices
            .Include(pp => pp.Product)
            .FirstOrDefaultAsync(pp => pp.Id == priceId);

        if (price == null || price.Product.UserId != GetCurrentUserId())
            return Json(new { success = false, message = "Preço não encontrado." });

        _context.ProductPrices.Remove(price);
        await _context.SaveChangesAsync();

        return Json(new { success = true });
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var product = await _context.Products
            .Include(p => p.Prices)
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == GetCurrentUserId());

        if (product == null) return NotFound();

        ViewData["Title"] = "Detalhes do Produto";
        return View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _context.Products
            .Include(p => p.Prices)
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == GetCurrentUserId());

        if (product == null) return NotFound();

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Produto '{product.Name}' excluído com sucesso!";
        return RedirectToAction(nameof(Index));
    }

    private string GenerateBarcode(string productCode, decimal price)
    {
        var priceString = price.ToString("F2").Replace(",", "").Replace(".", "");
        return productCode + priceString;
    }
}