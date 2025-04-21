create function dbo.geohash_base32 (
    @_index tinyint
)
returns char(1)
as
begin
    return case @_index
        when 0 then '0'
        when 1 then '1'
        when 2 then '2'
        when 3 then '3'
        when 4 then '4'
        when 5 then '5'
        when 6 then '6'
        when 7 then '7'
        when 8 then '8'
        when 9 then '9'
        when 10 then 'b'
        when 11 then 'c'
        when 12 then 'd'
        when 13 then 'e'
        when 14 then 'f'
        when 15 then 'g'
        when 16 then 'h'
        when 17 then 'j'
        when 18 then 'k'
        when 19 then 'm'
        when 20 then 'n'
        when 21 then 'p'
        when 22 then 'q'
        when 23 then 'r'
        when 24 then 's'
        when 25 then 't'
        when 26 then 'u'
        when 27 then 'v'
        when 28 then 'w'
        when 29 then 'x'
        when 30 then 'y'
        when 31 then 'z'
    end
end