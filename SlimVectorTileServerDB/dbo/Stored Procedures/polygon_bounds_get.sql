create procedure [dbo].[polygon_bounds_get] (
    @id int
) as
begin
    select  p.id,
            p.name,
            p.level,
            upper(p.object_type) as [type],
            p.geo.EnvelopeCenter().Long as center_lng,
            p.geo.EnvelopeCenter().Lat as center_lat,
            geometry::STGeomFromWKB(p.geo.STAsBinary(), 4326).STEnvelope().STPointN(1).STX as bounds_west,
            geometry::STGeomFromWKB(p.geo.STAsBinary(), 4326).STEnvelope().STPointN(1).STY as bounds_south,
            geometry::STGeomFromWKB(p.geo.STAsBinary(), 4326).STEnvelope().STPointN(3).STX as bounds_east,
            geometry::STGeomFromWKB(p.geo.STAsBinary(), 4326).STEnvelope().STPointN(3).STY as bounds_north
        from dbo.polygons p (nolock)
    where p.id = @id
end
go
grant execute
    on object::dbo.polygon_bounds_get to vector_tile_server_app
    as dbo;
go
