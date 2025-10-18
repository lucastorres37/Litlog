using System.Collections.Generic;
using Litlog.Services;
using Litlog.Models;


namespace Litlog.Services
{
    public static class ComentarioStore
    {
        private static Dictionary<string, List<Comentario>> comentariosPorLivro = new();

        public static void Adicionar(string titulo, Comentario comentario)
        {
            if (!comentariosPorLivro.ContainsKey(titulo))
                comentariosPorLivro[titulo] = new List<Comentario>();

            comentariosPorLivro[titulo].Add(comentario);
        }

        public static List<Comentario> BuscarPorTitulo(string titulo)
        {
            return comentariosPorLivro.ContainsKey(titulo)
                ? comentariosPorLivro[titulo]
                : new List<Comentario>();
        }
    }
}
