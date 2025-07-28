using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace HNTAS.Core.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HeatNetworksController : ControllerBase
    {
        private readonly IHeatNetworkService _hnService;
        private readonly ILogger<HeatNetworksController> _logger;
        private readonly ICounterService _counterService;

        public HeatNetworksController(IHeatNetworkService hnService, ILogger<HeatNetworksController> logger, ICounterService counterService)
        {
            _hnService = hnService;
            _logger = logger;
            _counterService = counterService;
        }

        [HttpPost("add-heat-network")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(HeatNetwork), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<HeatNetwork>> AddHeatNetwork([FromBody] HeatNetwork heatNetworkDetails)
        {
            //if (!ModelState.IsValid)
            //{
            //    _logger.LogWarning("Invalid initial registration data for UserId: {UserId}, EmailId: {EmailId}. Errors: {Errors}",
            //        registrationData.OneLoginId, registrationData.EmailId, string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            //    return ValidationProblem(ModelState);
            //}

            try
            {
                if (String.IsNullOrWhiteSpace(heatNetworkDetails.hn_id)) 
                {
                    var sequenceID = await _counterService.GetNextSequenceValue("heatNetworkId_sequence");
                    var heatNetworkId = $"HN{sequenceID:D7}";
                    heatNetworkDetails.hn_id = heatNetworkId;
                    _logger.LogInformation("Generated new heat network ID: {HeatNetworkId}", heatNetworkDetails.hn_id);

                }

                await _hnService.CreateAsync(heatNetworkDetails);
                _logger.LogInformation("New heat network initially registered: {HNID} (DB Id: {Id})", heatNetworkDetails.hn_id, heatNetworkDetails.Id);

                return CreatedAtAction(nameof(AddHeatNetwork), new { id = heatNetworkDetails.Id }, heatNetworkDetails);
            }
            //catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            //{
            //    _logger.LogWarning(ex, "Duplicate key error during initial user registration for UserId: {UserId}, EmailId: {EmailId}", registrationData.OneLoginId, registrationData.EmailId);
            //    return Conflict(new ProblemDetails
            //    {
            //        Status = StatusCodes.Status409Conflict,
            //        Title = "User Already Exists",
            //        Detail = $"A user with the provided UserId ({registrationData.OneLoginId}) or EmailId ({registrationData.EmailId}) already exists."
            //    });
            //}
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Unexpected error during initial user registration for UserId: {UserId}, EmailId: {EmailId}", registrationData.OneLoginId, registrationData.EmailId);
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Internal Server Error",
                    Detail = "An unexpected error occurred during initial user registration."
                });
            }
        }
    }
}
