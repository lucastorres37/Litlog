using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Litlog.Models;

namespace Litlog
{
    // Simple thread-safe in-memory comment store used by views/controllers.
    public static class ComentarioStore
    {
        // Key: livro title
        private static readonly ConcurrentDictionary<string, List<Comentario>> _store
            = new(StringComparer.OrdinalIgnoreCase);

        public static void Adicionar(string titulo, Comentario comentario)
        {
            if (string.IsNullOrWhiteSpace(titulo) || comentario == null)
                return;

            var list = _store.GetOrAdd(titulo, _ => new List<Comentario>());

            lock (list)
            {
                list.Add(comentario);
            }
        }

        public static List<Comentario> BuscarPorTitulo(string titulo)
        {
            if (string.IsNullOrWhiteSpace(titulo))
                return new List<Comentario>();

            if (_store.TryGetValue(titulo, out var list))
            {
                lock (list)
                {
                    return list.ToList(); // return a copy
                }
            }

            return new List<Comentario>();
        }
    }
}