create procedure [dbo].[request_params_create] (
    @params nvarchar(max)
) as
begin
    declare @hash varchar(32) = convert(varchar(32), hashbytes('MD5', @params), 2),
            @uuid char(36)

    set @uuid = (
        select uuid
            from dbo.request_params (nolock)
        where params_hash = @hash
    )

    if @uuid is null
    begin
        insert into dbo.request_params
        (
            params
        )
        values
        (
            @params
        )

        select	uuid as "uuid"
            from dbo.request_params (nolock)
        where id = scope_identity()
        for json path, without_array_wrapper
    end
    else
    begin
        select	@uuid as "uuid"
        for json path, without_array_wrapper
    end
end
go
grant execute
    on object::dbo.request_params_create to vector_tile_server_app
    as dbo;
go
