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
            var response = await _httpClient.GetAsync($"https://www.googleapis.com/books/v1/volumes?q={termo}&maxResults=40");
            if (!response.IsSuccessStatusCode)
                return new List<Livro>();

            var json = await response.Content.ReadAsStringAsync();
            var resultado = JsonDocument.Parse(json);

            var livros = new List<Livro>();
            foreach (var item in resultado.RootElement.GetProperty("items").EnumerateArray())
            {
                var volumeInfo = item.GetProperty("volumeInfo");
                double? avaliacao = volumeInfo.TryGetProperty("averageRating", out var rating) ? rating.GetDouble() : null;
                
                livros.Add(new Livro
                {
                    Id = item.GetProperty("id").GetString() ?? "",
                    Titulo = volumeInfo.GetProperty("title").GetString() ?? "",
                    Autor = volumeInfo.TryGetProperty("authors", out var autores) ? string.Join(", ", autores.EnumerateArray().Select(a => a.GetString())) : "Autor desconhecido",
                    Sinopse = volumeInfo.TryGetProperty("description", out var desc) ? desc.GetString().Truncate(500) ?? "" : "Sem sinopse disponível",
                    CapaUrl = volumeInfo.TryGetProperty("imageLinks", out var imagens) && imagens.TryGetProperty("thumbnail", out var thumb) ? thumb.GetString() ?? "" : "https://upload.wikimedia.org/wikipedia/commons/thumb/a/ac/No_image_available.svg/480px-No_image_available.svg.png",
                    Avaliacao = avaliacao
                });
            }

            livros = livros.OrderByDescending(l => l.Avaliacao ?? 0).ToList();
            livros = livros.Where(l => !string.IsNullOrEmpty(l.CapaUrl) && !l.CapaUrl.Contains("No_image_available")).ToList();

            return livros;
        }

        public async Task<List<Livro>> BuscarLivrosAsyncPorIds(List<string> ids)
        {
            var todosLivros = await BuscarLivrosAsync("");
            return todosLivros.Where(l => ids.Contains(l.Id)).ToList();
     
        }
        public async Task<Livro> BuscarLivroPorIdAsync(string id)
        {
            // AJUDA DO COPILOT ENTENDI NADA
            // O id do Google Books normalmente é uma string, mas seu modelo usa int.
            // Se o id for realmente string, troque o tipo do parâmetro para string.
            // Aqui, convertemos para string para montar a URL.
            var response = await _httpClient.GetAsync($"https://www.googleapis.com/books/v1/volumes/{id}");
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            var resultado = JsonDocument.Parse(json);

            if (!resultado.RootElement.TryGetProperty("volumeInfo", out var volumeInfo))
                return null;

            double? avaliacao = volumeInfo.TryGetProperty("averageRating", out var rating) ? rating.GetDouble() : null;

            var livro = new Livro
            {
                Id = id,
                Titulo = volumeInfo.GetProperty("title").GetString() ?? "",
                Autor = volumeInfo.TryGetProperty("authors", out var autores) ? string.Join(", ", autores.EnumerateArray().Select(a => a.GetString())) : "Autor desconhecido",
                Sinopse = volumeInfo.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "Sem sinopse disponível",
                CapaUrl = volumeInfo.TryGetProperty("imageLinks", out var imagens) && imagens.TryGetProperty("thumbnail", out var thumb) ? thumb.GetString() ?? "" : "https://upload.wikimedia.org/wikipedia/commons/thumb/a/ac/No_image_available.svg/480px-No_image_available.svg.png",
                Avaliacao = avaliacao
            };

            return livro;
        }

    }
}
