create function dbo.interpolate_zoom2precission (
    @zoom float
)
returns float
as
begin
    declare @result float

    if @zoom <= 10
    begin
        set @result = 1 + (@zoom - 0) * (6 - 1) / (10 - 0)
    end
    else
    begin
        set @result = 6
    end

    return convert(int, @result)
end