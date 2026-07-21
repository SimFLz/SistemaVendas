using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesManagement.Data;
using SalesManagement.Models;

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

        var query = _context.Products.AsQueryable();

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
        return View();
    }

    // POST: /Products/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Code,Price,IsActive")] Product product)
    {
        if (ModelState.IsValid)
        {
            // Verificar código duplicado
            if (await _context.Products.AnyAsync(p => p.Code == product.Code))
            {
                ModelState.AddModelError("Code", "Já existe um produto com este código.");
                ViewData["Title"] = "Novo Produto";
                return View(product);
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Produto '{product.Name}' cadastrado com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        ViewData["Title"] = "Novo Produto";
        return View(product);
    }

    // GET: /Products/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();

        ViewData["Title"] = "Editar Produto";
        return View(product);
    }

    // POST: /Products/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Code,Price,IsActive")] Product product)
    {
        if (id != product.Id) return NotFound();

        if (ModelState.IsValid)
        {
            // Verificar código duplicado (exceto o próprio produto)
            if (await _context.Products.AnyAsync(p => p.Code == product.Code && p.Id != id))
            {
                ModelState.AddModelError("Code", "Já existe um produto com este código.");
                ViewData["Title"] = "Editar Produto";
                return View(product);
            }

            try
            {
                _context.Update(product);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Produto '{product.Name}' atualizado com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(product.Id)) return NotFound();
                throw;
            }
        }

        ViewData["Title"] = "Editar Produto";
        return View(product);
    }

    // GET: /Products/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var product = await _context.Products
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
        var product = await _context.Products.FindAsync(id);
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

    private bool ProductExists(int id)
    {
        return _context.Products.Any(e => e.Id == id);
    }
}