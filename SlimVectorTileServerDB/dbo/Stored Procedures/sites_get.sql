create procedure dbo.sites_get (
    @x int null,
    @y int null,
    @z int null,
    @uuid char(36),
    @cluster int
) as
begin
    declare @params nvarchar(max),
            --
            @dma_code varchar(16),
            @cbs_code varchar(16),
            @zip_code varchar(16),
            --
            @bounds geography,
            --
            @precission int

    set @params =
    (
        select params
            from dbo.request_params (nolock)
        where uuid = @uuid
    )

    select	@dma_code = json_value(value,'$.dma_code'),
            @cbs_code = json_value(value,'$.cbs_code'),
            @zip_code = json_value(value,'$.zip_code')
    from openjson(@params)

    set @bounds = dbo.mercator_tile2geography(@x, @y, @z)

    if @cluster = 0
    begin
        set	@precission = dbo.interpolate_zoom2precission(@z)

        if @z >= 3
        begin
            set @bounds = dbo.mercator_tile2geography(@x, @y, @z)

            select	round(geo_lat, @precission) as geo_lat,
                    round(geo_lon, @precission) as geo_lon,
                    count(*) as [count]
            from (
                select	lat as geo_lat,
                        lon as geo_lon
                    from dbo.sites (nolock)
                where (geo.Filter(@bounds) = 1)
                    and (@dma_code is null or dma_code = @dma_code)
                    and (@cbs_code is null or cbs_code = @cbs_code)
                    and (@zip_code is null or zip_code = @zip_code)
            ) sub
            group by round(geo_lat, @precission),
                        round(geo_lon, @precission)
        end
        else
        begin
            select	round(geo_lat, @precission) as geo_lat,
                    round(geo_lon, @precission) as geo_lon,
                    count(*) as [count]
            from (
                select	lat as geo_lat,
                        lon as geo_lon
                    from dbo.sites (nolock)
                where (@dma_code is null or dma_code = @dma_code)
                    and (@cbs_code is null or cbs_code = @cbs_code)
                    and (@zip_code is null or zip_code = @zip_code)
            ) sub
            group by round(geo_lat, @precission),
                        round(geo_lon, @precission)
        end
    end
    else
    begin
        set @precission = dbo.interpolate_zoom2geohash(@z)

        if @z >= 3
        begin
            declare @sql nvarchar(max),
                    @sql_params nvarchar(max)

            set @sql = '
                select  geohash'+convert(varchar, @precission)+',
                        avg(geo_lat) as geo_lat,
                        avg(geo_lon) as geo_lon,
                        count(*) as [count]
                from (
                    select  geohash'+convert(varchar, @precission)+',
                            lat as geo_lat,
                            lon as geo_lon
                        from dbo.sites (nolock)
                    where (geo.Filter(@bounds) = 1)
                        and (@dma_code is null or dma_code = @dma_code)
                        and (@cbs_code is null or cbs_code = @cbs_code)
                        and (@zip_code is null or zip_code = @zip_code)
                ) sub
                group by geohash' + convert(varchar, @precission)

            set @sql_params = N'@bounds geography, @dma_code varchar(16), @cbs_code varchar(16), @zip_code varchar(16)'
            execute sp_executesql @sql, @sql_params, @bounds, @dma_code, @cbs_code, @zip_code
        end
        else
        begin
            set @sql = '
                select  geohash'+convert(varchar, @precission)+',
                        avg(geo_lat) as geo_lat,
                        avg(geo_lon) as geo_lon,
                        count(*) as [count]
                from (
                    select  geohash'+convert(varchar, @precission)+',
                            lat as geo_lat,
                            lon as geo_lon
                        from dbo.sites (nolock)
                    where (@dma_code is null or dma_code = @dma_code)
                        and (@cbs_code is null or cbs_code = @cbs_code)
                        and (@zip_code is null or zip_code = @zip_code)
                ) sub
                group by geohash' + convert(varchar, @precission)

            set @sql_params = N'@dma_code varchar(16), @cbs_code varchar(16), @zip_code varchar(16)'
            execute sp_executesql @sql, @sql_params, @dma_code, @cbs_code, @zip_code
        end
    end

end
go
grant execute
    on object::dbo.sites_get to vector_tile_server_app
    as dbo;
go
