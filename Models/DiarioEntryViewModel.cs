using System.Collections.Generic;

namespace Litlog.Models
{
    public class DiarioEntryViewModel
    {
        public Diario Diario { get; set; } = null!;
        public List<Comentario> Comentarios { get; set; } = new();
    }
}
