using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace ClubBackendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CuotasController : ControllerBase
    {
        private readonly string _connectionString;

        public CuotasController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // GET: api/Cuotas/pendientes/{usuarioId}
        // Devuelve las cuotas impagas de un socio o jugador
        [HttpGet("pendientes/{usuarioId}")]
        public IActionResult GetCuotasPendientes(int usuarioId)
        {
            List<object> cuotas = new List<object>();

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                string query = "SELECT id, monto, mes, anio, estado, fecha_vencimiento FROM cuotas WHERE usuario_id = @UsuarioId AND estado IN ('Pendiente', 'Vencida') ORDER BY anio, mes";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@UsuarioId", usuarioId);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            cuotas.Add(new
                            {
                                Id = reader.GetInt32(0),
                                Monto = reader.GetDecimal(1),
                                Mes = reader.GetInt32(2),
                                Anio = reader.GetInt32(3),
                                Estado = reader.GetString(4),
                                FechaVencimiento = reader.GetDateTime(5).ToString("yyyy-MM-dd")
                            });
                        }
                    }
                }
            }

            if (cuotas.Count == 0)
            {
                return Ok(new { mensaje = "El usuario está al día", deudas = cuotas });
            }

            return Ok(new { mensaje = $"El usuario debe {cuotas.Count} cuota(s)", deudas = cuotas });
        }

        // POST: api/Cuotas/pagar
        // Cambia el estado de la cuota a "Pagada" y registra la fecha y el administrador que cobró
        [HttpPost("pagar")]
        public IActionResult PagarCuota([FromBody] PagoRequest request)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                // GETDATE() toma la fecha y hora actual del servidor SQL
                string query = "UPDATE cuotas SET estado = 'Pagada', fecha_pago = GETDATE(), registrado_por = @AdminId WHERE id = @CuotaId AND estado != 'Pagada'";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@CuotaId", request.CuotaId);
                    cmd.Parameters.AddWithValue("@AdminId", request.AdminId);

                    int filasAfectadas = cmd.ExecuteNonQuery();

                    if (filasAfectadas > 0)
                    {
                        return Ok(new { mensaje = "Cuota pagada con éxito" });
                    }
                    else
                    {
                        return BadRequest(new { mensaje = "No se pudo procesar el pago. Quizás la cuota ya estaba pagada o no existe." });
                    }
                }
            }
        }
    }

    public class PagoRequest
    {
        public int CuotaId { get; set; }
        public int AdminId { get; set; }
    }
}
