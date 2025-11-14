using Litlog.Data;
using Litlog.Models;
using Litlog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace Litlog.Controllers
{
    public class LivrosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly GoogleBooksService _googleBooksService;

        public LivrosController(ApplicationDbContext context, GoogleBooksService googleBooksService)
        {
            _context = context;
            _googleBooksService = googleBooksService;
        }

        // Página de Livros Favoritados
        public async Task<IActionResult> Favoritos()
        {
            var userId = User?.Identity?.Name;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var favoritosIds = await _context.Favoritos
                .Where(f => f.UserId == userId)
                .Select(f => f.LivroId)
                .ToListAsync();

            var livrosFavoritos = await _googleBooksService.BuscarLivrosAsyncPorIds(favoritosIds);
            return View(livrosFavoritos);
        }

            // Catálogo de livros da API 
            public async Task<IActionResult> Catalogo(string termo, int page = 1, int pageSize = 10)
        {
            var termoFinal = string.IsNullOrWhiteSpace(termo) ? "marvel" : termo;
            TempData["UltimaBusca"] = termoFinal;

            try
            {
                var livros = await _googleBooksService.BuscarLivrosAsync(termoFinal) ?? new List<Livro>();


                page = Math.Max(1, page);
                pageSize = Math.Clamp(pageSize, 1, 50);
                var total = livros.Count;
                var totalPages = (int)Math.Ceiling(total / (double)pageSize);

                var pageItems = livros
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                ViewBag.Termo = termoFinal;
                ViewBag.Count = total;
                ViewBag.Page = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalPages = totalPages;

                return View(pageItems);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Catalogo error for '{termoFinal}': {ex}");
                TempData["Error"] = "Erro ao buscar livros. Veja logs para detalhes.";
                ViewBag.Termo = termoFinal;
                ViewBag.Count = 0;
                ViewBag.Page = 1;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalPages = 0;
                return View(new List<Livro>());
            }
        }

        // Página de detalhes de um livro da API
        public async Task<IActionResult> Detalhes(string id)
        {
            var livro = await _googleBooksService.BuscarLivroPorIdAsync(id);

            if (livro == null)
                return NotFound();

            var comentarios = await _context.Comentarios
                .Where(c => c.LivroId == id)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            ViewBag.Comentarios = comentarios;
            return View(livro);
        }

        [HttpPost]
        public async Task<IActionResult> Comentar(string id, string conteudo)
        {
            if (string.IsNullOrWhiteSpace(conteudo))
                return RedirectToAction("Detalhes", new { id });

            var termo = TempData["UltimaBusca"]?.ToString() ?? "marvel";
            var livros = await _googleBooksService.BuscarLivrosAsync(termo);
            var livro = livros.FirstOrDefault(l => l.Id == id);

            if (livro == null)
                return NotFound();

            var autor = User?.Identity?.Name ?? "Anonymous";
            var userId = User?.Identity?.Name;

            // prevenir comentários duplicados
            var already = await _context.Comentarios
                .AnyAsync(c => c.LivroId == id && c.UserId == userId && c.Conteudo == conteudo.Trim());

            if (!already)
            {
                var comentario = new Comentario
                {
                    LivroId = id,
                    Autor = autor,
                    Conteudo = conteudo.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    UserId = userId
                };

                _context.Comentarios.Add(comentario);
                await _context.SaveChangesAsync();
            }

            TempData["UltimaBusca"] = termo;
            return RedirectToAction("Detalhes", new { id });
        }

        // Adicionar livro aos favoritos
        [HttpPost]
        public IActionResult AdicionarFavorito(string id)
        {
            var favoritos = TempData["Favoritos"] as List<string> ?? new List<string>();
            if (!favoritos.Contains(id))
                favoritos.Add(id);
            TempData["Favoritos"] = favoritos;

            return RedirectToAction("Favoritos");
        }

        // Logar livro lido com nota
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogarLivroLido(string livroId, double? rating, bool? like, string comentario, bool? favorito)
        {
            var userId = User.Identity?.Name ?? "Anonymous";

            var livro = await _context.Livros.FindAsync(livroId);
            if (livro == null)
            {
                var termo = TempData["UltimaBusca"]?.ToString() ?? "marvel";
                var livrosApi = await _googleBooksService.BuscarLivrosAsync(termo);
                var livroApi = livrosApi.FirstOrDefault(l => l.Id == livroId);
                if (livroApi == null)
                    return NotFound();

                _context.Livros.Add(livroApi);
                await _context.SaveChangesAsync();
                livro = livroApi;
            }

            // Adicionar novo diário de leitura
            var diario = new Diario
            {
                LivroId = livro.Id,
                UserId = userId,
                DataLeitura = DateTime.Now,
                Nota = rating,
                Liked = like ?? false
            };

            _context.Diarios.Add(diario);

            await _context.SaveChangesAsync();

            // Se um comentário foi fornecido, adicioná-lo
            if (!string.IsNullOrWhiteSpace(comentario))
            {
                var trimmed = comentario.Trim();

                // Checar duplicatas
                var duplicate = await _context.Comentarios.AnyAsync(c =>
                    c.DiarioId == diario.Id &&
                    c.UserId == userId &&
                    c.Conteudo == trimmed);

                if (!duplicate)
                {
                    var c = new Comentario
                    {
                        LivroId = livro.Id,
                        DiarioId = diario.Id,
                        Autor = userId,
                        Conteudo = trimmed,
                        CreatedAt = DateTime.UtcNow,
                        UserId = userId
                    };

                    _context.Comentarios.Add(c);
                    await _context.SaveChangesAsync();
                }
            }

            return Json(new { success = true });
        }

        // Página do Diário de leituras
        [Authorize]
        public async Task<IActionResult> Diario()
        {
            var diarios = await _context.Diarios
                .Include(d => d.Livro)
                .OrderByDescending(d => d.DataLeitura)
                .ToListAsync();

            // Pegar IDs de diários para buscar comentários relacionados
            var diarioIds = diarios.Select(d => d.Id).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();

            var comentarios = new List<Comentario>();
            if (diarioIds.Any())
            {
                comentarios = await _context.Comentarios
                    .Where(c => c.DiarioId != null && diarioIds.Contains(c.DiarioId))
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();
            }

            var vm = diarios.Select(d => new DiarioEntryViewModel
            {
                Diario = d,
                // Adicionar comentário somente se for do diário específico
                Comentarios = comentarios.Where(c => c.DiarioId == d.Id).ToList()
            }).ToList();

            return View(vm);
        }

        [HttpPost]
        public IActionResult Create(Livro livro)
        {
            if (!livro.Ano.HasValue || livro.Ano <= 0)
            {
                livro.Ano = null;
            }

            return RedirectToAction("Index");
        }
    }
}