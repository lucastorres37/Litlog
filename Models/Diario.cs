using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Litlog.Models
{
    public class Diario
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        [Required]
        public string LivroId { get; set; }
        public Livro Livro { get; set; }

        [Required]
        public string UserId { get; set; }

        public double? Nota { get; set; }

        [Required]
        public DateTime DataLeitura { get; set; }
        public bool Liked { get; set; }
    }
}