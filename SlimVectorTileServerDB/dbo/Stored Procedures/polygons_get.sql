create procedure [dbo].[polygons_get] (
    @x int null,
    @y int null,
    @z int null,
    @uuid char(36)
) as
begin
    declare @params nvarchar(max),
            --
            @parent_id int,
            @search_string varchar(256),
            --
            @geo_filter geography,
            @bounds geography

    set @params =
    (
        select params
            from dbo.request_params (nolock)
        where uuid = @uuid
    )

    select  @parent_id = nullif(json_value(value,'$.parent_id'), ''),
            @search_string = nullif(json_value(value,'$.search_string'), '')
        from openjson(@params)

    set @bounds = dbo.mercator_tile2geography(@x, @y, @z)

    if @parent_id is not null
    begin
        declare @level int,
                @country_code varchar(3)

        select  @geo_filter = p.geo,
                @level = iif(p.[level] < 4, p.[level] + 1, p.[level]),
                @country_code = p.country_code
            from dbo.polygons p (nolock)
        where p.id = @parent_id
            and p.is_active = 1

        set @bounds = @geo_filter

        select  p.id,
                p.country_code,
                p.name,
                p.level,
                upper(p.object_type) as [type],
                p.geo.STArea() as area,
                p.geo.STAsText() as geometry_wkt
            from dbo.polygons p (nolock)
        where p.parent_id = @parent_id
            and p.is_active = 1
    end
    else
    begin
        if @z <= 3
        begin
            select  p.id,
                    p.country_code,
                    p.name,
                    p.level,
                    upper(p.object_type) as [type],
                    p.geo.STArea() as area,
                    p.geo.STAsText() as geometry_wkt
                from dbo.polygons p (nolock)
            where (p.geo.Filter(@bounds) = 1)
                and (p.[level] = 0)
                and (p.country_code = @search_string or @search_string is null)
                --and (p.is_active = 1)
        end
        else if @z > 3
            and @z <= 5
        begin
            select  p.id,
                    p.country_code,
                    p.name,
                    p.level,
                    upper(p.object_type) as [type],
                    p.geo.STArea() as area,
                    p.geo.STAsText() as geometry_wkt
                from dbo.polygons p (nolock)
            where (p.geo.Filter(@bounds) = 1)
                and p.[level] = 1
                and p.is_active = 1
        end
        else if @z > 5
             and @z <= 7
        begin
            select  p.id,
                    p.country_code,
                    p.name,
                    p.level,
                    upper(p.object_type) as [type],
                    p.geo.STArea() as area,
                    geo.STAsText() as geometry_wkt
                    -- p.geo.STAsText() as geometry_wkt
                from dbo.polygons p (nolock)
            where (p.geo.Filter(@bounds) = 1)
                and p.[level] = 2
                and p.is_active = 1
        end
        else if @z > 7
             and @z <= 9
        begin
            select  p.id,
                    p.country_code,
                    p.name,
                    p.level,
                    upper(p.object_type) as [type],
                    p.geo.STArea() as area,
                    p.geo.STAsText() as geometry_wkt
                from dbo.polygons p (nolock)
            where (p.geo.Filter(@bounds) = 1)
                and p.[level] = 3
                and p.is_active = 1
        end
        else
        begin
            select  p.id,
                    p.country_code,
                    p.name,
                    p.level,
                    upper(p.object_type) as [type],
                    p.geo.STArea() as area,
                    p.geo.STAsText() as geometry_wkt
                from dbo.polygons p (nolock)
            where (p.geo.Filter(@bounds) = 1)
                and p.[level] = 4
                and p.is_active = 1
        end
    end
end
go
grant execute
    on object::dbo.polygons_get to vector_tile_server_app
    as dbo;
go
