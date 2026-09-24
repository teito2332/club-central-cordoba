using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace ClubBackendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JugadoresController : ControllerBase
    {
        private readonly string _connectionString;

        public JugadoresController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // GET: api/Jugadores
        [HttpGet]
        public IActionResult GetJugadores()
        {
            var jugadores = new List<object>();

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                string query = "SELECT id, nombre_completo, posicion, imagen_url FROM jugadores_destacados";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            jugadores.Add(new
                            {
                                Id = reader.GetInt32(0),
                                NombreCompleto = reader.GetString(1),
                                Posicion = reader.GetString(2),
                                ImagenUrl = reader.GetString(3)
                            });
                        }
                    }
                }
            }
            return Ok(jugadores);
        }

        // POST: api/Jugadores
        [HttpPost]
        public IActionResult AgregarJugador([FromBody] NuevoJugadorRequest request)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                string query = @"
                    INSERT INTO jugadores_destacados (nombre_completo, posicion, imagen_url) 
                    VALUES (@Nombre, @Posicion, @ImagenUrl)";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Nombre", request.NombreCompleto);
                    cmd.Parameters.AddWithValue("@Posicion", request.Posicion);
                    cmd.Parameters.AddWithValue("@ImagenUrl", request.ImagenUrl);

                    int filas = cmd.ExecuteNonQuery();

                    if (filas > 0) return Ok(new { mensaje = "Jugador agregado con éxito" });
                    else return BadRequest(new { mensaje = "Error al agregar jugador" });
                }
            }
        }
    }

    public class NuevoJugadorRequest
    {
        public string NombreCompleto { get; set; }
        public string Posicion { get; set; }
        public string ImagenUrl { get; set; }
    }
}
