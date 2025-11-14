using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Litlog.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Litlog.Data;
using Humanizer;

namespace Litlog.Services
{
    public class GoogleBooksService
    {
        private readonly HttpClient _httpClient;
        private readonly ApplicationDbContext _dbContext;

        public GoogleBooksService(HttpClient httpClient, ApplicationDbContext dbContext)
        {
            _httpClient = httpClient;
            _dbContext = dbContext;
        }


        public async Task<List<Livro>> BuscarLivrosAsync(string termo)
        {
            var (items, _) = await BuscarLivrosAsync(termo, 0, 40);
            return items;
        }

        public async Task<(List<Livro> Items, int TotalItems)> BuscarLivrosAsync(string termo, int startIndex = 0, int maxResults = 40)
        {
            var query = string.IsNullOrWhiteSpace(termo) ? "marvel" : termo;
            var encoded = Uri.EscapeDataString(query);
            maxResults = Math.Clamp(maxResults, 1, 40);
            var response = await _httpClient.GetAsync($"https://www.googleapis.com/books/v1/volumes?q={encoded}&startIndex={startIndex}&maxResults={maxResults}");
            if (!response.IsSuccessStatusCode)
                return (new List<Livro>(), 0);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            int totalItems = 0;
            if (doc.RootElement.TryGetProperty("totalItems", out var totalEl) && totalEl.ValueKind == JsonValueKind.Number)
            {
                totalItems = totalEl.GetInt32();
            }

            if (!doc.RootElement.TryGetProperty("items", out var itemsEl) || itemsEl.ValueKind != JsonValueKind.Array)
                return (new List<Livro>(), totalItems);

            var livros = new List<Livro>();
            foreach (var item in itemsEl.EnumerateArray())
            {
                if (!item.TryGetProperty("volumeInfo", out var volumeInfo))
                    continue;

                var id = item.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? "" : "";
                var titulo = volumeInfo.TryGetProperty("title", out var titleEl) ? titleEl.GetString() ?? "" : "";
                var autor = "Autor desconhecido";
                if (volumeInfo.TryGetProperty("authors", out var authorsEl) && authorsEl.ValueKind == JsonValueKind.Array)
                {
                    var authors = authorsEl.EnumerateArray().Select(a => a.GetString()).Where(s => !string.IsNullOrEmpty(s));
                    autor = string.Join(", ", authors);
                    if (string.IsNullOrEmpty(autor))
                        autor = "Autor desconhecido";
                }

                var sinopse = volumeInfo.TryGetProperty("description", out var descEl) ? descEl.GetString() ?? "Sem sinopse disponível" : "Sem sinopse disponível";

                string capa = "https://upload.wikimedia.org/wikipedia/commons/thumb/a/ac/No_image_available.svg/480px-No_image_available.svg.png";
                if (volumeInfo.TryGetProperty("imageLinks", out var imagens) && imagens.TryGetProperty("thumbnail", out var thumb))
                {
                    capa = thumb.GetString() ?? capa;
                }

                double? avaliacao = null;
                if (volumeInfo.TryGetProperty("averageRating", out var rating) && rating.ValueKind == JsonValueKind.Number)
                {
                    if (rating.TryGetDouble(out var r))
                        avaliacao = r;
                }

                livros.Add(new Livro
                {
                    Id = id,
                    Titulo = titulo,
                    Autor = autor,
                    Sinopse = sinopse.Truncate(1000),
                    CapaUrl = capa,
                    Avaliacao = avaliacao
                });
            }

            livros = livros.OrderByDescending(l => l.Avaliacao ?? 0).ToList();

            return (livros, totalItems);
        }

        public async Task<List<Livro>> BuscarLivrosAsyncPorIds(List<string> ids)
        {
            var result = new List<Livro>();
            if (ids == null || ids.Count == 0)
                return result;

            foreach (var id in ids.Distinct())
            {
                try
                {
                    var livro = await BuscarLivroPorIdAsync(id);
                    if (livro != null)
                        result.Add(livro);
                }
                catch
                {
                    
                }
            }

            return result;
        }

        public async Task<Livro?> BuscarLivroPorIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            var encodedId = Uri.EscapeDataString(id);
            var response = await _httpClient.GetAsync($"https://www.googleapis.com/books/v1/volumes/{encodedId}");
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("volumeInfo", out var volumeInfo))
                return null;

            var titulo = volumeInfo.TryGetProperty("title", out var titleEl) ? titleEl.GetString() ?? "" : "";
            var autor = "Autor desconhecido";
            if (volumeInfo.TryGetProperty("authors", out var authorsEl) && authorsEl.ValueKind == JsonValueKind.Array)
            {
                var authors = authorsEl.EnumerateArray().Select(a => a.GetString()).Where(s => !string.IsNullOrEmpty(s));       
                autor = string.Join(", ", authors);
            }

            var sinopse = volumeInfo.TryGetProperty("description", out var descEl) ? descEl.GetString() ?? "Sem sinopse disponível" : "Sem sinopse disponível";

            string capa = "https://upload.wikimedia.org/wikipedia/commons/thumb/a/ac/No_image_available.svg/480px-No_image_available.svg.png";
            if (volumeInfo.TryGetProperty("imageLinks", out var imagens) && imagens.TryGetProperty("thumbnail", out var thumb))
            {
                capa = thumb.GetString() ?? capa;
            }

            double? avaliacao = null;
            if (volumeInfo.TryGetProperty("averageRating", out var rating) && rating.ValueKind == JsonValueKind.Number)
            {
                if (rating.TryGetDouble(out var r))
                    avaliacao = r;
            }

            return new Livro
            {
                Id = id,
                Titulo = titulo,
                Autor = autor,
                Sinopse = sinopse.Truncate(1000),
                CapaUrl = capa,
                Avaliacao = avaliacao
            };
        }
    }
}
