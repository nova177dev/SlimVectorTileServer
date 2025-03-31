create table dbo.vector_tile_cache (
    Id nvarchar(449) not null,
    [Value] varbinary(max) not null,
    ExpiresAtTime datetimeoffset(7) not null,
    SlidingExpirationInSeconds bigint null,
    AbsoluteExpiration datetimeoffset(7) null,
primary key clustered
(
    id asc
) with (pad_index = off,
        statistics_norecompute = off,
        ignore_dup_key = off,
        allow_row_locks = on,
        allow_page_locks = on,
        optimize_for_sequential_key = off) on [primary]
) on [primary] textimage_on [primary]
