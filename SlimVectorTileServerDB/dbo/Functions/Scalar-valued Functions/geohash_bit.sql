create function dbo.geohash_bit (
    @bit tinyint
)
returns tinyint
as
begin
    return case @bit
        when 0 then 16
        when 1 then 8
        when 2 then 4
        when 3 then 2
        when 4 then 1
    end
end