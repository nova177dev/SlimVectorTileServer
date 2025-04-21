create function dbo.geohash_encode (
    @latitude float,
    @longitude float,
    @precision tinyint
)
returns varchar(12)
AS
begin
    if @latitude is null or @longitude is null
        return null

    declare @is_even tinyint = 1
    declare @i tinyint = 0

    declare @latL decimal(38, 35) = -90.0,
            @latR decimal(38, 35) = 90.0,

            @lonT decimal(38, 34) = -180.0,
            @lonB decimal(38, 34) = 180.0,

            @bit int = 0,
            @ch int = 0,

            @mid float = null,

            @geohash varchar(12) = ''

    if @precision is null
        set @precision = 12

    while len(@geohash) < @precision
    begin
        if @is_even = 1
        begin
            set @mid = (@lonT + @lonB) / 2;

            --set @mid = ROUND(@mid, 7, 1);

            if @longitude > @mid
            begin
                set @ch = @ch | dbo.geohash_bit(@bit)
                set @lonT = @mid;
            end
            else
            begin
                set @lonB = @mid;
            end
        end
        else
        begin
            set @mid = (@latL + @latR) / 2;

            --set @mid = ROUND(@mid, 7, 1);

            if @mid < @latitude
            begin
                set @ch = @ch | dbo.geohash_bit(@bit);
                set @latL = @mid;
            end
            else
            begin
                set @latR = @mid
            end
        end

        if @is_even = 0
            set @is_even = 1
        else
            set @is_even = 0

        if @bit < 4
            set @bit = @bit + 1;
        else
        begin
            set @geohash = concat(@geohash, dbo.geohash_base32(@ch));
            set @bit = 0;
            set @ch = 0;
        end
    end

    return @geohash
end