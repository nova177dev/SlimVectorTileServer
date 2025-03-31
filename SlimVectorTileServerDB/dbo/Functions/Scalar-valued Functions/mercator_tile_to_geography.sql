create function dbo.mercator_tile_to_geography
(
    @x int,
    @y int,
    @z int
)
returns geography
as
begin
    declare @tileSize int = 256;
    declare @earthRadius float = 6378137.0;
    declare @mapSize float = @tileSize * power(2, @z);

    -- calculate pixel coordinates of the top-left corner of the tile
    declare @pixelX float = @x * @tileSize;
    declare @pixelY float = @y * @tileSize;

    -- convert pixel coordinates to mercator coordinates (epsg:3857)
    declare @mercatorX float = (@pixelX / @mapSize) * 2 * pi() * @earthRadius - pi() * @earthRadius;
    declare @mercatorY float = pi() * @earthRadius - (@pixelY / @mapSize) * 2 * pi() * @earthRadius;

    -- convert mercator coordinates to latitude and longitude (epsg:4326)
    declare @lonLeft float = (@mercatorX / @earthRadius) * (180 / pi());
    declare @latTop float = (atan(exp(@mercatorY / @earthRadius)) * (360 / pi())) - 90;

    -- calculate pixel coordinates of the bottom-right corner of the tile
    set @pixelX = (@x + 1) * @tileSize;
    set @pixelY = (@y + 1) * @tileSize;

    -- convert pixel coordinates to mercator coordinates (epsg:3857)
    set @mercatorX = (@pixelX / @mapSize) * 2 * pi() * @earthRadius - pi() * @earthRadius;
    set @mercatorY = pi() * @earthRadius - (@pixelY / @mapSize) * 2 * pi() * @earthRadius;

    -- convert mercator coordinates to latitude and longitude (epsg:4326)
    declare @lonRight float = (@mercatorX / @earthRadius) * (180 / pi());
    declare @latBottom float = (atan(exp(@mercatorY / @earthRadius)) * (360 / pi())) - 90;

    -- create a geography polygon from the bounding box coordinates
    declare @polygon geography;
    set @polygon = geography::STPolyFromText(
        'POLYGON((' +
        cast(@lonLeft as varchar(20)) + ' ' + cast(@latTop as varchar(20)) + ', ' +
        cast(@lonRight as varchar(20)) + ' ' + cast(@latTop as varchar(20)) + ', ' +
        cast(@lonRight as varchar(20)) + ' ' + cast(@latBottom as varchar(20)) + ', ' +
        cast(@lonLeft as varchar(20)) + ' ' + cast(@latBottom as varchar(20)) + ', ' +
        cast(@lonLeft as varchar(20)) + ' ' + cast(@latTop as varchar(20)) +
        '))', 4326);

    return iif(@polygon.STArea() < 18000000000000, @polygon, @polygon.ReorientObject())
end