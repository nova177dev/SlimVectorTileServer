create procedure dbo.site_clusters_get (
    @x int null,
    @y int null,
    @z int null,
    @uuid char(36)
) as
begin
    declare @params nvarchar(max),
            --
            @dma_code varchar(16),
            @cbs_code varchar(16),
            @zip_code varchar(16),
            --
            @bounds geography,
            @precision int = dbo.interpolate_zoom2geohash(@z)

    declare @sql nvarchar(max),
            @sql_params nvarchar(max)

    set @params =
    (
        select params
            from dbo.request_params (nolock)
        where uuid = @uuid
    )

    select  @dma_code = json_value(value,'$.dma_code'),
            @cbs_code = json_value(value,'$.cbs_code'),
            @zip_code = json_value(value,'$.zip_code')
    from openjson(@params)
    --select top 10 * from dbo.sites

    set @bounds = dbo.mercator_tile2geography(@x, @y, @z)

    if @z >= 3
    begin
        set @sql = '
            select  geohash'+convert(varchar, @precision)+',
                    avg(geo_lat) as geo_lat,
                    avg(geo_lon) as geo_lon,
                    count(*) as [count]
            from (
                select  geohash'+convert(varchar, @precision)+',
                        lat as geo_lat,
                        lon as geo_lon
                    from dbo.sites (nolock)
                where (geo.Filter(@bounds) = 1)
                    and (@dma_code is null or dma_code = @dma_code)
                    and (@cbs_code is null or cbs_code = @cbs_code)
                    and (@zip_code is null or zip_code = @zip_code)
            ) sub
            group by geohash' + convert(varchar, @precision)
        --print @sql
        --return
        set @sql_params = N'@bounds geography, @dma_code varchar(16), @cbs_code varchar(16), @zip_code varchar(16)'
        execute sp_executesql @sql, @sql_params, @bounds, @dma_code, @cbs_code, @zip_code
    end
    else
    begin
        set @sql = '
            select  geohash'+convert(varchar, @precision)+',
                    avg(geo_lat) as geo_lat,
                    avg(geo_lon) as geo_lon,
                    count(*) as [count]
            from (
                select  geohash'+convert(varchar, @precision)+',
                        lat as geo_lat,
                        lon as geo_lon
                    from dbo.sites (nolock)
                where (@dma_code is null or dma_code = @dma_code)
                    and (@cbs_code is null or cbs_code = @cbs_code)
                    and (@zip_code is null or zip_code = @zip_code)
            ) sub
            group by geohash' + convert(varchar, @precision)
        --print @sql
        --return
        set @sql_params = N'@dma_code varchar(16), @cbs_code varchar(16), @zip_code varchar(16)'
        execute sp_executesql @sql, @sql_params, @dma_code, @cbs_code, @zip_code
    end
end
go
grant execute
    on object::dbo.site_clusters_get to vector_tile_server_app
    as dbo;
go
