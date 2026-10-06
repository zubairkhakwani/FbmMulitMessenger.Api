using FBMMultiMessenger.Buisness.Request.FacebookWebSocket;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace FBMMultiMessenger.Api.Controllers
{
    // TEMPORARY load-testing controller — mirrors the RegisterExtension IsActive DB check so it can be
    // hammered over plain HTTP. Gated to Development so it can never be hit in production. REMOVE when done.
    [ApiController]
    [Route("api/test")]
    public class TestController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IWebHostEnvironment _env;
        private readonly IMediator mediatR;

        public TestController(ApplicationDbContext dbContext, IWebHostEnvironment env, IMediator mediatR)
        {
            _dbContext = dbContext;
            _env = env;
            this.mediatR = mediatR;
        }

        [HttpPost("payload-test")]
        public async Task<IActionResult> FbPayloadTest([FromBody] WebSocketModelRequest request)
        {
            if (!_env.IsDevelopment())
            {
                return NotFound();
            }

            await mediatR.Send(request);

            return Ok();
        }

        // GET /api/test/db-check?accountId=1&userId=1
        // Runs the same account "is active" lookup used by RegisterExtension and returns the outcome +
        // how long the query took. On failure (pool timeout / 53300 / etc.) it returns 500 with the
        // actual error message, so the load tester can see exactly what/how many failed.
        [HttpGet("db-check")]
        public async Task<IActionResult> DbCheck([FromQuery] int accountId, [FromQuery] int userId, CancellationToken cancellationToken)
        {
            if (!_env.IsDevelopment())
            {
                return NotFound();
            }

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var isActive = await _dbContext.Accounts
                    .AnyAsync(a => a.Id == accountId && a.UserId == userId && a.IsActive, cancellationToken);

                stopwatch.Stop();
                Console.WriteLine("yayyyyyyyy");
                return Ok(new { isActive, ms = stopwatch.ElapsedMilliseconds });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return StatusCode(500, new { error = ex.Message, ms = stopwatch.ElapsedMilliseconds });
            }
        }
    }
}
