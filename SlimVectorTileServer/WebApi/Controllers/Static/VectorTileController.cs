using SlimVectorTileServer.Application.Static.VectorTiles.Queries;
using SlimVectorTileServer.Application.Static.VectorTiles.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System.ComponentModel.DataAnnotations;
using SlimVectorTileServer.Domain.Entities.Static.VectorTileServer;
using SlimVectorTileServer.Domain.Entities.Common;
using SlimVectorTileServer.Application.Common;

namespace SlimVectorTileServer.WebApi.Controllers.Static
{
    /// <summary>
    /// Controller for serving MapBox (mvt/pbf) tiles.
    /// </summary>
    [Route("api/tiles")]
    [ApiController]
    public class VectorTileController(
        IMediator mediator,
        ResponseHandler responseHandler
    ) : ControllerBase
    {
        private readonly IMediator _mediator = mediator;
        private readonly ResponseHandler _responseHandler = responseHandler;

        /// <summary>
        /// Creates a request params.
        /// </summary>
        [AllowAnonymous]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorMessage), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorMessage), StatusCodes.Status500InternalServerError)]
        [HttpPost("request-params")]
        public async Task<ActionResult<ApiResponse>> CreateRequestParams([Required][FromBody] VectorTileRequestParams requestParams)
        {
            ApiResponse response = await _mediator.Send(new CreateRequestParamsCommand(requestParams));
            return _responseHandler.HandleResponse(response);
        }

        /// <summary>
        /// Returns a MapBox Polygon Vector Tile (MVT/PBF) for the specified tile coordinates in Mercator projection.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("polygons/{zoom:int}/{xTile:int}/{yTile:int}/{uuid}")]
        [Produces("application/x-protobuf")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileResult))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorMessage), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPolygonVectorTiles(int zoom, int xTile, int yTile, string uuid)
        {
            Response.Headers.Append("Content-Encoding", "gzip");
            Response.Headers.Vary = HeaderNames.AcceptEncoding;

            byte[] response = await _mediator.Send(new GetPolygonVectorTileQuery(zoom, xTile, yTile, uuid));
            return File(response, "application/x-protobuf");
        }

        /// <summary>
        /// Returns a MapBox Points Vector Tile (MVT/PBF) for the specified tile coordinates in Mercator projection.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("{zoom:int}/{xTile:int}/{yTile:int}/{cluster:int}/{uuid}")]
        [Produces("application/x-protobuf")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileResult))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorMessage), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetVectorTiles(int zoom, int xTile, int yTile, int cluster, string uuid)
        {
            Response.Headers.Append("Content-Encoding", "gzip");
            Response.Headers.Vary = HeaderNames.AcceptEncoding;

            byte[] response = await _mediator.Send(new GetVectorTileQuery(zoom, xTile, yTile, uuid, cluster));
            return File(response, "application/x-protobuf");
        }
    }
}