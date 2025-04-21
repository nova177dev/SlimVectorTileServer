create function dbo.geohash_base32_index (
    @ch char(1)
)
returns tinyint
as
begin
    return case @ch
        when '0' then 0
        when '1' then 1
        when '2' then 2
        when '3' then 3
        when '4' then 4
        when '5' then 5
        when '6' then 6
        when '7' then 7
        when '8' then 8
        when '9' then 9
        when 'b' then 10
        when 'c' then 11
        when 'd' then 12
        when 'e' then 13
        when 'f' then 14
        when 'g' then 15
        when 'h' then 16
        when 'j' then 17
        when 'k' then 18
        when 'm' then 19
        when 'n' then 20
        when 'p' then 21
        when 'q' then 22
        when 'r' then 23
        when 's' then 24
        when 't' then 25
        when 'u' then 26
        when 'v' then 27
        when 'w' then 28
        when 'x' then 29
        when 'y' then 30
        when 'z' then 31
    end
end