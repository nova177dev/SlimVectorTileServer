using SlimVectorTileServer.Application.Common;
using SlimVectorTileServer.Application.Static.Queries;
using SlimVectorTileServer.Application.Static.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System.ComponentModel.DataAnnotations;
using SlimVectorTileServer.Domain.Entities.Static;

namespace SlimVectorTileServer.WebApi.Controllers.Static
{
    /// <summary>
    /// Controller for serving MapBox (mvt) tiles.
    /// </summary>
    [Route("api/tiles")]
    [ApiController]
    public class VectorTileController : ControllerBase
    {
        private readonly IMediator _mediator;

        public VectorTileController(IMediator mediator, TilesService tilesHelper)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Creates a request params.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("request-params")]
        public async Task<IActionResult> CreateRequestParams([Required][FromBody] VectorTileRequestParams requestParams)
        {
            try
            {
                VectorTileRequestParams response = await _mediator.Send(new CreateRequestParamsCommand(requestParams));
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Returns a MapBox Vector Tile (MVT) for the specified tile coordinates in Mercator projection.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("{z}/{x}/{y}/{uuid}.mvt")]
        [Produces("application/x-protobuf")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileResult))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetVectorTiles(int z, int x, int y, string uuid)
        {
            Response.Headers.Append("Content-Encoding", "gzip");
            Response.Headers.Vary = HeaderNames.AcceptEncoding;

            byte[] response = await _mediator.Send(new GetVectorTileQuery(z, x, y, uuid));
            return File(response, "application/x-protobuf");

        }
    }
}