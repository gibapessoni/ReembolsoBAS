using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReembolsoBAS.Data;
using ReembolsoBAS.Models;

namespace ReembolsoBAS.Controllers
{
        [ApiController]
        [Route("api/[controller]")]
        [Authorize(Roles = "rh,gerente_rh")]
        public class EmpregadosController : ControllerBase
        {
            private readonly AppDbContext _ctx;
            public EmpregadosController(AppDbContext ctx) => _ctx = ctx;

            [HttpGet]
            public async Task<IActionResult> GetAll() =>
                Ok(await _ctx.Empregados.AsNoTracking().ToListAsync());

            [HttpPost]
            public async Task<IActionResult> Create(Empregado emp)
            {
                _ctx.Empregados.Add(emp);
                await _ctx.SaveChangesAsync();
                return CreatedAtAction(nameof(GetById), new { id = emp.Id }, emp);
            }

            [HttpGet("{id:int}")]
            public async Task<IActionResult> GetById(int id) =>
                Ok(await _ctx.Empregados.FindAsync(id));

            [HttpPut("{id:int}")]
            public async Task<IActionResult> Update(int id, Empregado emp)
            {
                if (id != emp.Id) return BadRequest();
                _ctx.Entry(emp).State = EntityState.Modified;
                await _ctx.SaveChangesAsync();
                return NoContent();
            }

            [HttpDelete("{id:int}")]
            public async Task<IActionResult> Delete(int id)
            {
                var emp = await _ctx.Empregados.FindAsync(id);
                if (emp is null) return NotFound();
                _ctx.Empregados.Remove(emp);
                await _ctx.SaveChangesAsync();
                return NoContent();
            }
        }

    }

