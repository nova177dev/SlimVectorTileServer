create procedure [dbo].[sites_get] (
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
            @bounds geography

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

    set @bounds = dbo.mercator_tile_to_geography(@x, @y, @z)

    if @z >= 3
    begin
        set @bounds = dbo.mercator_tile_to_geography(@x, @y, @z)

        select	lat as geo_lat,
                lon as geo_lon
            from dbo.sites (nolock)
        where (geo.Filter(@bounds) = 1)
            and (@dma_code is null or dma_code = @dma_code)
            and (@cbs_code is null or cbs_code = @cbs_code)
            and (@zip_code is null or zip_code = @zip_code)
    end
    else
    begin
        select	lat as geo_lat,
                lon as geo_lon
            from dbo.sites (nolock)
        where (@dma_code is null or dma_code = @dma_code)
            and (@cbs_code is null or cbs_code = @cbs_code)
            and (@zip_code is null or zip_code = @zip_code)
    end
end

go
grant execute
    on object::dbo.sites_get to vector_tile_server_app
    as dbo;
go
