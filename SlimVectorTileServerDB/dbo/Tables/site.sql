create table dbo.[sites] (
    id int identity(1,1) not null
        constraint pk_sites primary key clustered,
    uuid uniqueidentifier not null
        index ix_sites_uuid
        constraint df_site_uuid default newid(),
    --
    dma_code varchar(16) null
        index ix_sites_dma_code,
    cbs_code varchar(16) null
        index ix_sites_cbs_code,
    zip_code varchar(16) null
        index ix_sites_zip_code,
    lat as geo.Lat persisted not null,
    lon as geo.Long persisted not null,
    geo geography not null
)
go
create spatial index six_site on dbo.sites (geo)
go
create nonclustered index ix_site_lat on dbo.sites (lat)
go
create nonclustered index ix_site_lon on dbo.sites (lon)
