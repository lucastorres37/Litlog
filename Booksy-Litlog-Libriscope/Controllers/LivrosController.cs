using Litlog.Data;
using Litlog.Models;
using Litlog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

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
        public async Task<IActionResult> Catalogo(string termos)
        {
            var termoFinal = string.IsNullOrWhiteSpace(termos) ? "marvel" : termos;
            TempData["UltimaBusca"] = termoFinal;

            var livros = await _googleBooksService.BuscarLivrosAsync(termoFinal);
            return View(livros);
        }

        // Página de detalhes de um livro da API
        public async Task<IActionResult> Detalhes(string id)
        {
            var livro = await _googleBooksService.BuscarLivroPorIdAsync(id);

            if (livro == null)
                return NotFound();

            ViewBag.Comentarios = ComentarioStore.BuscarPorTitulo(livro.Titulo);
            return View(livro);
        }

        // Adicionar comentário
        [HttpPost]
        public IActionResult Comentar(string id, string conteudo)
        {
            var termo = TempData["UltimaBusca"]?.ToString() ?? "marvel";
            var livros = _googleBooksService.BuscarLivrosAsync(termo).Result;
            var livro = livros.FirstOrDefault(l => l.Id == id);

            if (livro == null)
                return NotFound();

            ComentarioStore.Adicionar(livro.Titulo, new Comentario
            {
                Autor = "lowksy",
                Conteudo = conteudo
            });

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
            var userId = User.Identity.Name;

            // Tente encontrar o livro localmente
            var livro = await _context.Livros.FindAsync(livroId);

            // Se não existir, busque na API e salve
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

            // Retorne sucesso para AJAX
            return Json(new { success = true });
        }

        // Página do Diário de leituras
        [Authorize]
        public async Task<IActionResult> Diario()
        {
            var diarios = _context.Diarios
                .Include(d => d.Livro) 
                .ToList();
            return View(diarios); 
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