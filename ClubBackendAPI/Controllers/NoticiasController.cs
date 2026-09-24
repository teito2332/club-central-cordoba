using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace ClubBackendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NoticiasController : ControllerBase
    {
        private readonly string _connectionString;

        public NoticiasController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // GET: api/Noticias
        // La web pública usa esto para mostrar las noticias ordenadas por fecha
        [HttpGet]
        public IActionResult GetNoticias()
        {
            var noticias = new List<object>();

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                // FORMAT nos devuelve la fecha lista para mostrar en la web
                string query = @"
                    SELECT id, titulo, contenido, imagen_url, 
                           FORMAT(fecha_publicacion, 'dd/MM/yyyy') as fecha 
                    FROM noticias 
                    ORDER BY fecha_publicacion DESC";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            noticias.Add(new
                            {
                                Id = reader.GetInt32(0),
                                Titulo = reader.GetString(1),
                                Contenido = reader.GetString(2),
                                ImagenUrl = reader.IsDBNull(3) ? null : reader.GetString(3),
                                Fecha = reader.GetString(4)
                            });
                        }
                    }
                }
            }
            return Ok(noticias);
        }

        // POST: api/Noticias
        // El administrador usa esto desde el panel privado para publicar
        [HttpPost]
        public IActionResult CrearNoticia([FromBody] NuevaNoticiaRequest request)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                string query = @"
                    INSERT INTO noticias (titulo, contenido, imagen_url, autor_id, fecha_publicacion) 
                    VALUES (@Titulo, @Contenido, @ImagenUrl, @AutorId, GETDATE())";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Titulo", request.Titulo);
                    cmd.Parameters.AddWithValue("@Contenido", request.Contenido);
                    // Si no hay imagen, mandamos un NULL a la base de datos
                    cmd.Parameters.AddWithValue("@ImagenUrl", string.IsNullOrEmpty(request.ImagenUrl) ? DBNull.Value : request.ImagenUrl);
                    cmd.Parameters.AddWithValue("@AutorId", request.AutorId);

                    int filasAfectadas = cmd.ExecuteNonQuery();

                    if (filasAfectadas > 0)
                    {
                        return Ok(new { mensaje = "Noticia publicada con éxito" });
                    }
                    else
                    {
                        return BadRequest(new { mensaje = "Error al publicar la noticia" });
                    }
                }
            }
        }
    }

    // Modelo para recibir el JSON desde el frontend
    public class NuevaNoticiaRequest
    {
        public string Titulo { get; set; }
        public string Contenido { get; set; }
        public string ImagenUrl { get; set; }
        public int AutorId { get; set; }
    }
}
