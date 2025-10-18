using System.ComponentModel.DataAnnotations;

namespace Litlog.Models
{
    public class Livro
    {
        public string Id { get; set; }

        public string CapaUrl { get; set; }

        [Required]
        [StringLength(100)]
        public string Titulo { get; set; }

        [Required]
        [StringLength(100)]
        public string Autor { get; set; }
        public int? Ano { get; set; }

        [StringLength(1000)]
        public string Sinopse { get; set; }
        public double? Avaliacao { get; set; }
    }
}
