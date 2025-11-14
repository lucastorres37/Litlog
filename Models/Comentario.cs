using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Litlog.Models
{
    public class Comentario
    {
        public int Id { get; set; }
        public string LivroId { get; set; } = string.Empty;

        public string? DiarioId { get; set; }

        public string Autor { get; set; } = string.Empty;
        public string Conteudo { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? UserId { get; set; }

        public Diario? Diario { get; set; }
    }
}