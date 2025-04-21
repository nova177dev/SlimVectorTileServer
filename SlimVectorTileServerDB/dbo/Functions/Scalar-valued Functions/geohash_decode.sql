create function dbo.geohash_decode (
    @geohash varchar(12)
)
returns @result table(
    LatL decimal(10, 7),
    LatR decimal(10, 7),
    LngT decimal(10, 7),
    LngB decimal(10, 7),
    LatC decimal(10,7),
    LngC decimal(10, 7),
    LatError decimal(10,7),
    LngError decimal(10, 7)
)
as
begin
    declare @is_even bit = 1,
            @latL decimal(10,7) = -90.0,
            @latR decimal(10,7) = 90.0,
            @latC decimal(10,7),
            @lonT decimal(10,7) = -180,
            @lonB decimal(10,7) = 180,
            @lonC decimal(10,7),

            @lat_err decimal(10,7) = 90.0,
            @lon_err decimal(10,7) = 180.0,

            @i tinyint = 0,
            @len tinyint = LEN(@geohash),

            @c CHAR(1) = '',
            @cd TINYINT = 0,

            @j TINYINT = 0,

            @mask TINYINT = 0,
            @masked_val TINYINT = 0

    while @i < @len
    begin
        set @c = SUBSTRING(@geohash, @i + 1, 1)

        set @cd = dbo.geohash_base32_index(@c)

        set @j = 0

        while @j < 5
        begin
            set @mask = dbo.geohash_bit(@j)
            set @masked_val = @cd & @mask

            if @is_even = 1
            begin
                set @lon_err = @lon_err / 2

                if @masked_val != 0
                    set @lonT = (@lonT + @lonB) / 2
                else
                    set @lonB = (@lonT + @lonB) / 2
            end
            else
            begin
                set @lat_err = @lat_err / 2

                if @masked_val != 0
                    set @latL = (@latL + @latR) / 2
                else
                    set @latR = (@latL + @latR) / 2
            end

            if @is_even = 0
                set @is_even = 1
            else
                set @is_even = 0

            set @j = @j + 1
        end

        set @i = @i + 1
    end

    set @latC = (@latL + @latR) / 2
    set @lonC = (@lonT + @lonB) / 2

    insert @result
    select @latL, @latR, @lonT, @lonB, @latC, @lonC, @lat_err, @lon_err
    return
end