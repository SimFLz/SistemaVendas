using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesManagement.Data;
using SalesManagement.Models;
using SalesManagement.Models.Enums;
using SalesManagement.ViewModels;

namespace SalesManagement.Controllers;

public class SalesController : BaseController
{
    private readonly ApplicationDbContext _context;
    private const string CartSessionKey = "SaleCart";

    public SalesController(ApplicationDbContext context)
    {
        _context = context;
    }

    private string CartKey => $"{CartSessionKey}_{GetCurrentUserId()}";

    private SaleRegisterViewModel GetCartFromSession()
    {
        var cart = HttpContext.Session.GetString(CartKey);
        if (string.IsNullOrEmpty(cart))
            return new SaleRegisterViewModel();

        return JsonSerializer.Deserialize<SaleRegisterViewModel>(cart) ?? new SaleRegisterViewModel();
    }

    private void SaveCartToSession(SaleRegisterViewModel cart)
    {
        HttpContext.Session.SetString(CartKey, JsonSerializer.Serialize(cart));
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Register()
    {
        ViewData["Title"] = "Nova Venda";
        var cart = GetCartFromSession();
        return View(cart);
    }

    [HttpPost]
    public async Task<IActionResult> AddItem(string productCode)
    {
        var productPrice = await _context.ProductPrices
            .Include(pp => pp.Product)
            .FirstOrDefaultAsync(pp => pp.Barcode == productCode && pp.Product.IsActive && pp.Product.UserId == GetCurrentUserId());

        if (productPrice == null)
            return Json(new { success = false, message = "Produto não encontrado. Use o código de barras completo." });

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

    [HttpPost]
    public IActionResult UpdateQuantity(int productPriceId, int quantity)
    {
        if (quantity < 1)
            return Json(new { success = false, message = "Quantidade inválida." });

        var cart = GetCartFromSession();
        var item = cart.Items.FirstOrDefault(i => i.ProductPriceId == productPriceId);

        if (item == null)
            return Json(new { success = false, message = "Item não encontrado." });

        item.Quantity = quantity;
        SaveCartToSession(cart);
        return Json(new { success = true, cart = cart });
    }

    [HttpPost]
    public IActionResult UpdatePrice(int productPriceId, decimal newPrice)
    {
        if (newPrice <= 0)
            return Json(new { success = false, message = "Preço inválido." });

        var cart = GetCartFromSession();
        var item = cart.Items.FirstOrDefault(i => i.ProductPriceId == productPriceId);

        if (item == null)
            return Json(new { success = false, message = "Item não encontrado." });

        item.UnitPrice = newPrice;
        SaveCartToSession(cart);
        return Json(new { success = true, cart = cart });
    }

    [HttpPost]
    public IActionResult ApplyDiscount(int productPriceId, decimal discount)
    {
        var cart = GetCartFromSession();
        var item = cart.Items.FirstOrDefault(i => i.ProductPriceId == productPriceId);

        if (item == null)
            return Json(new { success = false, message = "Item não encontrado." });

        var maxDiscount = item.UnitPrice * item.Quantity;
        if (discount > maxDiscount)
            return Json(new { success = false, message = "Desconto não pode ser maior que o valor do item." });

        item.Discount = discount;
        SaveCartToSession(cart);
        return Json(new { success = true, cart = cart });
    }

    [HttpPost]
    public IActionResult RemoveItem(int productPriceId)
    {
        var cart = GetCartFromSession();
        var item = cart.Items.FirstOrDefault(i => i.ProductPriceId == productPriceId);

        if (item == null)
            return Json(new { success = false, message = "Item não encontrado." });

        cart.Items.Remove(item);
        SaveCartToSession(cart);
        return Json(new { success = true, cart = cart });
    }

    [HttpPost]
    public IActionResult ApplyGeneralDiscount(decimal discount)
    {
        var cart = GetCartFromSession();

        if (discount > cart.Subtotal)
            return Json(new { success = false, message = "Desconto não pode ser maior que o subtotal." });

        cart.GeneralDiscount = discount;
        SaveCartToSession(cart);
        return Json(new { success = true, cart = cart });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Finalize(SaleRegisterViewModel model)
    {
        var cart = GetCartFromSession();

        if (!ModelState.IsValid || cart.Items.Count == 0)
        {
            TempData["Error"] = "Adicione pelo menos um produto e preencha todos os campos.";
            return RedirectToAction(nameof(Register));
        }

        var sale = new Sale
        {
            SaleDate = DateTime.Now,
            PaymentMethod = model.PaymentMethod,
            Installments = model.PaymentMethod == PaymentMethod.CreditCard ? model.Installments : 1,
            Discount = cart.GeneralDiscount,
            TotalAmount = cart.TotalAmount,
            Status = SaleStatus.Completed,
            UserId = GetCurrentUserId()
        };

        _context.Sales.Add(sale);
        await _context.SaveChangesAsync();

        foreach (var item in cart.Items)
        {
            _context.SaleItems.Add(new SaleItem
            {
                SaleId = sale.Id,
                ProductId = item.ProductId,
                ProductPriceId = item.ProductPriceId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Discount = item.Discount,
                TotalPrice = item.TotalPrice
            });
        }

        await _context.SaveChangesAsync();
        HttpContext.Session.Remove(CartKey);

        TempData["Success"] = $"Venda #{sale.Id} finalizada com sucesso!";
        return RedirectToAction(nameof(Receipt), new { id = sale.Id });
    }

    public async Task<IActionResult> Receipt(int? id)
    {
        if (id == null) return NotFound();

        var sale = await _context.Sales
            .Include(s => s.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == GetCurrentUserId());

        if (sale == null) return NotFound();

        var store = await _context.Users.FindAsync(GetCurrentUserId());
        ViewData["StoreName"] = store?.StoreName ?? "SALESUP";
        ViewData["StoreCnpj"] = store?.Cnpj;
        ViewData["StoreAddress"] = store?.StoreAddress;
        ViewData["StorePhone"] = store?.StorePhone;

        ViewData["Title"] = $"Notinha - Venda #{sale.Id}";
        return View(sale);
    }

    public async Task<IActionResult> History(string? search, DateTime? dateFrom, DateTime? dateTo, PaymentMethod? paymentMethod)
    {
        ViewData["Title"] = "Histórico de Vendas";

        var query = _context.Sales
            .Where(s => s.UserId == GetCurrentUserId() && s.Status != SaleStatus.Cancelled)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search) && int.TryParse(search, out int saleId))
            query = query.Where(s => s.Id == saleId);

        if (dateFrom.HasValue)
            query = query.Where(s => s.SaleDate.Date >= dateFrom.Value.Date);

        if (dateTo.HasValue)
            query = query.Where(s => s.SaleDate.Date <= dateTo.Value.Date);

        if (paymentMethod.HasValue)
            query = query.Where(s => s.PaymentMethod == paymentMethod.Value);

        var sales = await query
            .OrderByDescending(s => s.SaleDate)
            .ToListAsync();

        ViewData["Search"] = search;
        ViewData["DateFrom"] = dateFrom?.ToString("yyyy-MM-dd");
        ViewData["DateTo"] = dateTo?.ToString("yyyy-MM-dd");
        ViewData["PaymentMethod"] = paymentMethod;

        return View(sales);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var sale = await _context.Sales
            .Include(s => s.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == GetCurrentUserId());

        if (sale == null) return NotFound();

        ViewData["Title"] = $"Detalhes da Venda #{sale.Id}";
        return View(sale);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var sale = await _context.Sales
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == GetCurrentUserId());

        if (sale == null) return NotFound();

        sale.Status = SaleStatus.Cancelled;
        _context.Update(sale);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Venda #{sale.Id} cancelada com sucesso.";
        return RedirectToAction(nameof(History));
    }
}