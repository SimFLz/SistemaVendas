using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesManagement.Data;
using SalesManagement.Models;
using SalesManagement.ViewModels;

namespace SalesManagement.Controllers;

public class ProductsController : Controller
{
    private readonly ApplicationDbContext _context;

    public ProductsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /Products (Lista com pesquisa)
    public async Task<IActionResult> Index(string search = "")
    {
        ViewData["Title"] = "Produtos";
        ViewData["Search"] = search;

        var query = _context.Products
            .Include(p => p.Prices)
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

    // GET: /Products/Create
    public IActionResult Create()
    {
        ViewData["Title"] = "Novo Produto";
        return View(new ProductViewModel());
    }

    // POST: /Products/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductViewModel model)
    {
        if (ModelState.IsValid)
        {
            // Verificar código duplicado
            if (await _context.Products.AnyAsync(p => p.Code == model.Code))
            {
                ModelState.AddModelError("Code", "Já existe um produto com este código.");
                ViewData["Title"] = "Novo Produto";
                return View(model);
            }

            var product = new Product
            {
                Name = model.Name,
                Code = model.Code,
                IsActive = model.IsActive
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Adicionar preços
            foreach (var priceVm in model.Prices)
            {
                var barcode = GenerateBarcode(model.Code, priceVm.Price);

                // Verificar se já existe este preço para o produto
                if (await _context.ProductPrices.AnyAsync(pp => pp.ProductId == product.Id && pp.Price == priceVm.Price))
                {
                    continue; // Pula preço duplicado
                }

                var productPrice = new ProductPrice
                {
                    ProductId = product.Id,
                    Price = priceVm.Price,
                    Barcode = barcode
                };

                _context.ProductPrices.Add(productPrice);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Produto '{product.Name}' cadastrado com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        ViewData["Title"] = "Novo Produto";
        return View(model);
    }

    // GET: /Products/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var product = await _context.Products
            .Include(p => p.Prices)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null) return NotFound();

        ViewData["Title"] = "Editar Produto";

        var model = new ProductViewModel
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

        return View(model);
    }

    // POST: /Products/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProductViewModel model)
    {
        if (id != model.Id) return NotFound();

        if (ModelState.IsValid)
        {
            // Verificar código duplicado
            if (await _context.Products.AnyAsync(p => p.Code == model.Code && p.Id != id))
            {
                ModelState.AddModelError("Code", "Já existe um produto com este código.");
                ViewData["Title"] = "Editar Produto";
                return View(model);
            }

            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null) return NotFound();

                product.Name = model.Name;
                product.Code = model.Code;
                product.IsActive = model.IsActive;

                _context.Update(product);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Produto '{product.Name}' atualizado com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(model.Id)) return NotFound();
                throw;
            }
        }

        ViewData["Title"] = "Editar Produto";
        return View(model);
    }

    // POST: /Products/AddPrice (AJAX)
   
    [HttpPost]
    public async Task<IActionResult> AddPrice(int productId, string price)
    {
        // 🔧 PARSING EXPLÍCITO pt-BR para garantir 39,90 e nunca 3990,00
        if (!decimal.TryParse(price, System.Globalization.NumberStyles.Currency,
            new System.Globalization.CultureInfo("pt-BR"), out decimal parsedPrice))
        {
            return Json(new { success = false, message = "Preço inválido. Use o formato 39,90" });
        }

        if (parsedPrice <= 0)
        {
            return Json(new { success = false, message = "O preço deve ser maior que zero." });
        }

        var product = await _context.Products.FindAsync(productId);
        if (product == null)
        {
            return Json(new { success = false, message = "Produto não encontrado." });
        }

        // Verificar se já existe este preço
        if (await _context.ProductPrices.AnyAsync(pp => pp.ProductId == productId && pp.Price == parsedPrice))
        {
            return Json(new { success = false, message = "Este preço já existe para este produto." });
        }

        var barcode = GenerateBarcode(product.Code, parsedPrice);

        // Verificar se código de barras já existe
        if (await _context.ProductPrices.AnyAsync(pp => pp.Barcode == barcode))
        {
            return Json(new { success = false, message = "Código de barras já existe." });
        }

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

    // POST: /Products/RemovePrice (AJAX)
    [HttpPost]
    public async Task<IActionResult> RemovePrice(int priceId)
    {
        var price = await _context.ProductPrices.FindAsync(priceId);
        if (price == null)
        {
            return Json(new { success = false, message = "Preço não encontrado." });
        }

        // Verificar se o preço foi usado em vendas
        var hasSales = await _context.SaleItems.AnyAsync(si => si.ProductPriceId == priceId);
        if (hasSales)
        {
            return Json(new { success = false, message = "Não é possível excluir este preço pois já foi usado em vendas." });
        }

        _context.ProductPrices.Remove(price);
        await _context.SaveChangesAsync();

        return Json(new { success = true });
    }

    // GET: /Products/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var product = await _context.Products
            .Include(p => p.Prices)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null) return NotFound();

        ViewData["Title"] = "Detalhes do Produto";
        return View(product);
    }

    // POST: /Products/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _context.Products
            .Include(p => p.Prices)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null) return NotFound();

        // Verificar se o produto foi usado em vendas
        var hasSales = await _context.SaleItems.AnyAsync(si => si.ProductId == id);
        if (hasSales)
        {
            product.IsActive = false;
            _context.Update(product);
            await _context.SaveChangesAsync();
            TempData["Warning"] = $"Produto '{product.Name}' foi inativado (possui vendas registradas).";
        }
        else
        {
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Produto '{product.Name}' excluído com sucesso!";
        }

        return RedirectToAction(nameof(Index));
    }

    // Helper: Gerar código de barras
    private string GenerateBarcode(string productCode, decimal price)
    {
        // Remove vírgula e ponto do preço
        var priceString = price.ToString("F2").Replace(",", "").Replace(".", "");
        return productCode + priceString;
    }

    private bool ProductExists(int id)
    {
        return _context.Products.Any(e => e.Id == id);
    }
}