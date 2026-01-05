create table dbo.polygons (
    id int identity(1,1) not null
        constraint pk_polygons_id primary key clustered,
    name nvarchar(255) not null,
    type nvarchar(50) null,
    dma_code varchar(16) null
        index ix_polygons_dma_code,
    cbs_code varchar(16) null
        index ix_polygons_cbs_code,
    zip_code varchar(16) null
        index ix_polygons_zip_code,
    geo geography not null,
    [level] int null
        index ix_polygons_level,
    country_code varchar(3) null
        index ix_polygons_country_code,
    object_type varchar(64) null
        index ix_polygons_object_type,
    is_active bit not null
        index ix_polygons_is_active
        constraint df_polygons_is_active default (0),
    parent_id int null
        index ix_polygons_parent_id
        constraint fk_polygons_parent_id foreign key references dbo.polygons (id),
    search_string as ('[void], ' + isnull(country_code, '') + ' > ' + isnull(name, '')) persisted not null
)
go
create spatial index sx_polygons_geo on dbo.polygons(geo)
    using geography_auto_grid
    with (cells_per_object = 16);
go
