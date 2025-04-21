create function dbo.interpolate_zoom2geohash (
    @zoom float
)
returns float
as
begin
    return iif(@zoom > 10, 4, cast(1 + (@zoom - 0) * (4 - 1) / (10 - 0) as int))
end