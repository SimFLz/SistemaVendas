using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesManagement.Data;
using SalesManagement.Models;
using SalesManagement.Models.Enums;
using SalesManagement.ViewModels;

namespace SalesManagement.Controllers;

public class SalesController : Controller
{
    private readonly ApplicationDbContext _context;

    public SalesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /Sales (Tela Inicial)
    public IActionResult Index()
    {
        return View();
    }

    // GET: /Sales/Register (Tela de PDV)
    public IActionResult Register()
    {
        ViewData["Title"] = "Nova Venda";

        var cart = HttpContext.Session.GetString("SaleCart");
        var viewModel = new SaleRegisterViewModel();

        if (!string.IsNullOrEmpty(cart))
        {
            viewModel = System.Text.Json.JsonSerializer.Deserialize<SaleRegisterViewModel>(cart)
                ?? new SaleRegisterViewModel();
        }

        return View(viewModel);
    }

    // POST: /Sales/AddItem (AJAX) — busca APENAS por código de barras completo
    [HttpPost]
    public async Task<IActionResult> AddItem(string productCode)
    {
        // 🔧 AGORA SÓ ACEITA O CÓDIGO DE BARRAS COMPLETO (ex: 00139990)
        var productPrice = await _context.ProductPrices
            .Include(pp => pp.Product)
            .FirstOrDefaultAsync(pp => pp.Barcode == productCode && pp.Product.IsActive);

        if (productPrice == null)
        {
            return Json(new { success = false, message = "Produto não encontrado. Use o código de barras completo (ex: 00139990)." });
        }

        var cart = GetCartFromSession();

        var existingItem = cart.Items.FirstOrDefault(i => i.ProductPriceId == productPrice.Id);
        if (existingItem != null)
        {
            existingItem.Quantity++;
        }
        else
        {
            cart.Items.Add(new SaleItemViewModel
            {
                ProductId = productPrice.ProductId,
                ProductPriceId = productPrice.Id,
                ProductName = productPrice.Product.Name,
                ProductCode = productPrice.Product.Code,
                Barcode = productPrice.Barcode,
                UnitPrice = productPrice.Price,
                Quantity = 1
            });
        }

        SaveCartToSession(cart);
        return Json(new { success = true, cart = cart });
    }

    // POST: /Sales/UpdateQuantity (AJAX) — agora por productPriceId
    [HttpPost]
    public IActionResult UpdateQuantity(int productPriceId, int quantity)
    {
        if (quantity < 1)
        {
            return Json(new { success = false, message = "Quantidade inválida." });
        }

        var cart = GetCartFromSession();
        var item = cart.Items.FirstOrDefault(i => i.ProductPriceId == productPriceId);

        if (item == null)
        {
            return Json(new { success = false, message = "Item não encontrado." });
        }

        item.Quantity = quantity;
        SaveCartToSession(cart);
        return Json(new { success = true, cart = cart });
    }

    // POST: /Sales/UpdatePrice (AJAX) — agora por productPriceId
    [HttpPost]
    public IActionResult UpdatePrice(int productPriceId, decimal newPrice)
    {
        if (newPrice <= 0)
        {
            return Json(new { success = false, message = "Preço inválido." });
        }

        var cart = GetCartFromSession();
        var item = cart.Items.FirstOrDefault(i => i.ProductPriceId == productPriceId);

        if (item == null)
        {
            return Json(new { success = false, message = "Item não encontrado." });
        }

        item.UnitPrice = newPrice;
        SaveCartToSession(cart);
        return Json(new { success = true, cart = cart });
    }

    // POST: /Sales/ApplyDiscount (AJAX) — agora por productPriceId
    [HttpPost]
    public IActionResult ApplyDiscount(int productPriceId, decimal discount)
    {
        var cart = GetCartFromSession();
        var item = cart.Items.FirstOrDefault(i => i.ProductPriceId == productPriceId);

        if (item == null)
        {
            return Json(new { success = false, message = "Item não encontrado." });
        }

        var maxDiscount = item.UnitPrice * item.Quantity;
        if (discount > maxDiscount)
        {
            return Json(new { success = false, message = "Desconto não pode ser maior que o valor do item." });
        }

        item.Discount = discount;
        SaveCartToSession(cart);
        return Json(new { success = true, cart = cart });
    }

    // POST: /Sales/RemoveItem (AJAX) — agora por productPriceId
    [HttpPost]
    public IActionResult RemoveItem(int productPriceId)
    {
        var cart = GetCartFromSession();
        var item = cart.Items.FirstOrDefault(i => i.ProductPriceId == productPriceId);

        if (item == null)
        {
            return Json(new { success = false, message = "Item não encontrado." });
        }

        cart.Items.Remove(item);
        SaveCartToSession(cart);
        return Json(new { success = true, cart = cart });
    }

    // POST: /Sales/ApplyGeneralDiscount (AJAX)
    [HttpPost]
    public IActionResult ApplyGeneralDiscount(decimal discount)
    {
        var cart = GetCartFromSession();

        if (discount > cart.Subtotal)
        {
            return Json(new { success = false, message = "Desconto não pode ser maior que o subtotal." });
        }

        cart.GeneralDiscount = discount;
        SaveCartToSession(cart);
        return Json(new { success = true, cart = cart });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Finalize(SaleRegisterViewModel model)
    {
        var cart = GetCartFromSession();

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Preencha todos os campos obrigatórios.";
            return RedirectToAction(nameof(Register));
        }

        var sale = new Sale
        {
            SaleDate = DateTime.Now,
            PaymentMethod = model.PaymentMethod,
            Installments = model.PaymentMethod == PaymentMethod.CreditCard ? model.Installments : 1,
            Discount = cart.GeneralDiscount,
            TotalAmount = cart.TotalAmount,
            Status = SaleStatus.Completed
        };

        _context.Sales.Add(sale);
        await _context.SaveChangesAsync();

        foreach (var item in cart.Items)
        {
            var saleItem = new SaleItem
            {
                SaleId = sale.Id,
                ProductId = item.ProductId,
                ProductPriceId = item.ProductPriceId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Discount = item.Discount,
                TotalPrice = item.TotalPrice
            };

            _context.SaleItems.Add(saleItem);
        }

        await _context.SaveChangesAsync();

        HttpContext.Session.Remove("SaleCart");

        TempData["Success"] = $"Venda #{sale.Id} finalizada com sucesso!";
        return RedirectToAction(nameof(Receipt), new { id = sale.Id });
    }

    // GET: /Sales/Receipt/5 (Notinha da venda)
    public async Task<IActionResult> Receipt(int? id)
    {
        if (id == null) return NotFound();

        var sale = await _context.Sales
            .Include(s => s.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (sale == null) return NotFound();

        ViewData["Title"] = $"Notinha - Venda #{sale.Id}";
        return View(sale);
    }

    // GET: /Sales/History
    public async Task<IActionResult> History(string? search, DateTime? dateFrom, DateTime? dateTo, PaymentMethod? paymentMethod)
    {
        ViewData["Title"] = "Histórico de Vendas";

        var query = _context.Sales
            .Where(s => s.Status != SaleStatus.Cancelled)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            if (int.TryParse(search, out int saleId))
            {
                query = query.Where(s => s.Id == saleId);
            }
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(s => s.SaleDate.Date >= dateFrom.Value.Date);
        }

        if (dateTo.HasValue)
        {
            query = query.Where(s => s.SaleDate.Date <= dateTo.Value.Date);
        }

        if (paymentMethod.HasValue)
        {
            query = query.Where(s => s.PaymentMethod == paymentMethod.Value);
        }

        var sales = await query
            .OrderByDescending(s => s.SaleDate)
            .ToListAsync();

        ViewData["Search"] = search;
        ViewData["DateFrom"] = dateFrom?.ToString("yyyy-MM-dd");
        ViewData["DateTo"] = dateTo?.ToString("yyyy-MM-dd");
        ViewData["PaymentMethod"] = paymentMethod;

        return View(sales);
    }

    // GET: /Sales/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var sale = await _context.Sales
            .Include(s => s.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (sale == null) return NotFound();

        ViewData["Title"] = $"Detalhes da Venda #{sale.Id}";
        return View(sale);
    }

    // POST: /Sales/Cancel/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var sale = await _context.Sales.FindAsync(id);
        if (sale == null) return NotFound();

        sale.Status = SaleStatus.Cancelled;
        _context.Update(sale);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Venda #{sale.Id} cancelada com sucesso.";
        return RedirectToAction(nameof(History));
    }

    // POST: /Sales/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var sale = await _context.Sales
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (sale == null) return NotFound();

        _context.SaleItems.RemoveRange(sale.Items);
        _context.Sales.Remove(sale);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Venda #{id} excluída com sucesso.";
        return RedirectToAction(nameof(History));
    }

    // Helpers
    private SaleRegisterViewModel GetCartFromSession()
    {
        var cart = HttpContext.Session.GetString("SaleCart");
        if (string.IsNullOrEmpty(cart))
        {
            return new SaleRegisterViewModel();
        }

        return System.Text.Json.JsonSerializer.Deserialize<SaleRegisterViewModel>(cart)
            ?? new SaleRegisterViewModel();
    }

    private void SaveCartToSession(SaleRegisterViewModel cart)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(cart);
        HttpContext.Session.SetString("SaleCart", json);
    }
}