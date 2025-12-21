create procedure [dbo].[polygons_get] (
    @x int null,
    @y int null,
    @z int null,
    @uuid char(36)
) as
begin
    declare @params nvarchar(max),
            --
            @search_string varchar(256),
            --
            @bounds geography

    set @params =
    (
        select params
            from dbo.request_params (nolock)
        where uuid = @uuid
    )

    select  @search_string = nullif(json_value(value,'$.search_string'), '')
        from openjson(@params)

    set @bounds = dbo.mercator_tile2geography(@x, @y, @z)

    if @z <= 4
    begin
        select  p.id,
                p.country_code,
                p.name,
                p.level,
                upper(p.object_type) as [type],
                p.geo.STAsText() as geometry_wkt
            from dbo.polygons p (nolock)
        where (p.geo.Filter(@bounds) = 1)
            and p.[level] = 1
            and (
                    @search_string is null
                        or contains(p.search_string, @search_string)
                )
    end
    else if @z > 4
         and @z <= 7
    begin
        select  p.id,
                p.country_code,
                p.name,
                p.level,
                upper(p.object_type) as [type],
                p.geo.STAsText() as geometry_wkt
            from dbo.polygons p (nolock)
        where (p.geo.Filter(@bounds) = 1)
            and p.[level] = 2
            and (
                    @search_string is null
                        or contains(p.search_string, @search_string)
                )

    end
    else if @z > 7
         and @z <= 10
    begin
        select  p.id,
                p.country_code,
                p.name,
                p.level,
                upper(p.object_type) as [type],
                p.geo.STAsText() as geometry_wkt
            from dbo.polygons p (nolock)
        where (p.geo.Filter(@bounds) = 1)
            and p.[level] = 3
            and (
                    @search_string is null
                        or contains(p.search_string, @search_string)
                )

    end
    else
    begin
        select  p.id,
                p.country_code,
                p.name,
                p.level,
                upper(p.object_type) as [type],
                p.geo.STAsText() as geometry_wkt
            from dbo.polygons p (nolock)
        where (p.geo.Filter(@bounds) = 1)
            and p.[level] = 4
            and (
                    @search_string is null
                        or contains(p.search_string, @search_string)
                )
    end
end
