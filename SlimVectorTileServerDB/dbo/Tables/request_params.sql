create table dbo.request_params (
    id int identity(1,1) not null
        constraint pk_request_params_id primary key clustered,
    uuid uniqueidentifier not null
        index ix_request_params
        constraint df_request_params default newid(),
    created_dtm datetime2 not null
        index ix_request_params_created_dtm
        constraint df_request_params_created_dtm default getutcdate(),
    handled_dtm datetime2 null
        index ix_request_params_handled_dtm,
    --
    params nvarchar(max) not null,
    params_hash as convert(varchar(32), hashbytes('MD5', params), 2) persisted
)
go
create nonclustered index ix_request_params_params_hash on dbo.request_params(params_hash)