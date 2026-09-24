using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace ClubBackendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AsistenciasController : ControllerBase
    {
        private readonly string _connectionString;

        public AsistenciasController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // GET: api/Asistencias/plantel/{categoriaId}
        // Devuelve todos los jugadores asignados a una categoría específica
        [HttpGet("plantel/{categoriaId}")]
        public IActionResult GetPlantel(int categoriaId)
        {
            var plantel = new List<object>();

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                string query = @"
                    SELECT u.id, u.nombre, u.apellido 
                    FROM usuarios u
                    INNER JOIN detalle_jugadores d ON u.id = d.usuario_id
                    WHERE d.categoria_id = @CategoriaId";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@CategoriaId", categoriaId);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            plantel.Add(new
                            {
                                Id = reader.GetInt32(0),
                                Nombre = reader.GetString(1),
                                Apellido = reader.GetString(2)
                            });
                        }
                    }
                }
            }

            return Ok(plantel);
        }

        // POST: api/Asistencias/registrar
        // Guarda la asistencia masiva de todo el plantel en un día
        [HttpPost("registrar")]
        public IActionResult RegistrarAsistencia([FromBody] RegistroAsistenciaRequest request)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                // Usamos una transacción para guardar todo de golpe
                using (SqlTransaction transaction = con.BeginTransaction())
                {
                    try
                    {
                        string query = @"
                            INSERT INTO asistencias (usuario_id, categoria_id, fecha, estado, registrado_por) 
                            VALUES (@UsuarioId, @CategoriaId, @Fecha, @Estado, @RegistradoPor)";

                        foreach (var jugador in request.Jugadores)
                        {
                            using (SqlCommand cmd = new SqlCommand(query, con, transaction))
                            {
                                cmd.Parameters.AddWithValue("@UsuarioId", jugador.UsuarioId);
                                cmd.Parameters.AddWithValue("@CategoriaId", request.CategoriaId);
                                cmd.Parameters.AddWithValue("@Fecha", request.Fecha.ToString("yyyy-MM-dd"));
                                cmd.Parameters.AddWithValue("@Estado", jugador.Estado); // 'Presente', 'Ausente' o 'Justificado'
                                cmd.Parameters.AddWithValue("@RegistradoPor", request.ProfesorId);

                                cmd.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                        return Ok(new { mensaje = "Asistencia registrada correctamente." });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return StatusCode(500, new { mensaje = "Error al guardar asistencia", detalle = ex.Message });
                    }
                }
            }
        }
    }

    // Modelos para recibir el JSON
    public class RegistroAsistenciaRequest
    {
        public int CategoriaId { get; set; }
        public int ProfesorId { get; set; }
        public DateTime Fecha { get; set; }
        public List<JugadorAsistencia> Jugadores { get; set; }
    }

    public class JugadorAsistencia
    {
        public int UsuarioId { get; set; }
        public string Estado { get; set; }
    }
}
