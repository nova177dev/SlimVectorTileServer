create procedure dbo.request_params_create (
    @params nvarchar(max)
) as
begin
    insert into dbo.request_params
    (
        params
    )
    values
    (
        @params
    )

    select uuid as "data.uuid"
        from dbo.request_params (nolock)
    where id = scope_identity()
    for json path, without_array_wrapper
end
go
grant execute
    on object::dbo.request_params_create to vector_tile_server_app
    as dbo;
go
