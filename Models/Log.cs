using Litlog.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Litlog.Models
{
    public class Log
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }
        public string LivroId { get; set; }
        public int UserId { get; set; }
        public DateTime DataLeitura { get; set; }
        public double? Nota { get; set; }

        public Livro Livro { get; set; }
    }
}