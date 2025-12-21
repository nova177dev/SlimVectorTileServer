create procedure [dbo].[sites_populate] (
    @point_count int = 1000000
) as
begin
    set nocount on;
    
    -- Create temporary tables for population distribution data
    create table #states (
        state_code char(2),
        state_name varchar(50),
        population_weight float
    );
    
    create table #dma_regions (
        dma_code varchar(16),
        dma_name varchar(100),
        population_weight float
    );
    
    create table #cbsa_regions (
        cbs_code varchar(16),
        cbsa_name varchar(100),
        population_weight float
    );
    
    create table #zip_codes (
        zip_code varchar(16),
        state_code char(2),
        population_weight float
    );
    
    -- Insert population data for states (based on approximate 2020 census data)
    insert into #states (state_code, state_name, population_weight) values
    ('CA', 'California', 0.12),
    ('TX', 'Texas', 0.09),
    ('FL', 'Florida', 0.07),
    ('NY', 'New York', 0.06),
    ('PA', 'Pennsylvania', 0.04),
    ('IL', 'Illinois', 0.04),
    ('OH', 'Ohio', 0.035),
    ('GA', 'Georgia', 0.033),
    ('NC', 'North Carolina', 0.032),
    ('MI', 'Michigan', 0.03),
    ('NJ', 'New Jersey', 0.027),
    ('VA', 'Virginia', 0.026),
    ('WA', 'Washington', 0.024),
    ('AZ', 'Arizona', 0.023),
    ('MA', 'Massachusetts', 0.022),
    ('TN', 'Tennessee', 0.021),
    ('IN', 'Indiana', 0.021),
    ('MO', 'Missouri', 0.019),
    ('MD', 'Maryland', 0.019),
    ('WI', 'Wisconsin', 0.018),
    ('CO', 'Colorado', 0.018),
    ('MN', 'Minnesota', 0.017),
    ('SC', 'South Carolina', 0.016),
    ('AL', 'Alabama', 0.015),
    ('LA', 'Louisiana', 0.014),
    ('KY', 'Kentucky', 0.014),
    ('OR', 'Oregon', 0.013),
    ('OK', 'Oklahoma', 0.012),
    ('CT', 'Connecticut', 0.011),
    ('UT', 'Utah', 0.01),
    ('IA', 'Iowa', 0.01),
    ('NV', 'Nevada', 0.01),
    ('AR', 'Arkansas', 0.009),
    ('MS', 'Mississippi', 0.009),
    ('KS', 'Kansas', 0.009),
    ('NM', 'New Mexico', 0.007),
    ('NE', 'Nebraska', 0.006),
    ('WV', 'West Virginia', 0.005),
    ('ID', 'Idaho', 0.005),
    ('HI', 'Hawaii', 0.004),
    ('NH', 'New Hampshire', 0.004),
    ('ME', 'Maine', 0.004),
    ('MT', 'Montana', 0.003),
    ('RI', 'Rhode Island', 0.003),
    ('DE', 'Delaware', 0.003),
    ('SD', 'South Dakota', 0.003),
    ('ND', 'North Dakota', 0.002),
    ('AK', 'Alaska', 0.002),
    ('VT', 'Vermont', 0.002),
    ('WY', 'Wyoming', 0.002),
    ('DC', 'District of Columbia', 0.002);
    
    -- Insert sample DMA regions (Designated Market Areas)
    insert into #dma_regions (dma_code, dma_name, population_weight) values
    ('501', 'New York', 0.065),
    ('803', 'Los Angeles', 0.05),
    ('602', 'Chicago', 0.033),
    ('618', 'Philadelphia', 0.028),
    ('506', 'Boston', 0.023),
    ('623', 'Dallas-Fort Worth', 0.025),
    ('807', 'San Francisco-Oakland-San Jose', 0.024),
    ('511', 'Washington, DC', 0.023),
    ('528', 'Miami-Fort Lauderdale', 0.017),
    ('613', 'Atlanta', 0.022),
    ('616', 'Houston', 0.023),
    ('819', 'Seattle-Tacoma', 0.015),
    ('505', 'Detroit', 0.015),
    ('512', 'Tampa-St. Petersburg', 0.012),
    ('602', 'Phoenix', 0.018),
    ('539', 'Cleveland-Akron', 0.011),
    ('524', 'Orlando-Daytona Beach', 0.01),
    ('535', 'Denver', 0.016),
    ('504', 'St. Louis', 0.01),
    ('517', 'Charlotte', 0.012),
    ('538', 'Pittsburgh', 0.009),
    ('623', 'Sacramento-Stockton-Modesto', 0.008),
    ('506', 'Portland, OR', 0.009),
    ('547', 'Indianapolis', 0.008),
    ('509', 'Baltimore', 0.009),
    ('548', 'San Diego', 0.011),
    ('534', 'Raleigh-Durham', 0.01),
    ('610', 'Nashville', 0.008),
    ('514', 'Minneapolis-St. Paul', 0.017),
    ('527', 'Kansas City', 0.007),
    ('508', 'Columbus, OH', 0.008),
    ('544', 'Cincinnati', 0.007),
    ('560', 'Milwaukee', 0.006),
    ('602', 'Salt Lake City', 0.006),
    ('624', 'San Antonio', 0.008),
    ('516', 'Austin', 0.008),
    ('537', 'Las Vegas', 0.007),
    ('567', 'Oklahoma City', 0.005),
    ('510', 'Jacksonville', 0.005),
    ('522', 'Louisville', 0.005),
    ('575', 'New Orleans', 0.004),
    ('581', 'Memphis', 0.004),
    ('619', 'Buffalo', 0.004),
    ('545', 'Richmond-Petersburg', 0.004),
    ('520', 'Birmingham', 0.004),
    ('530', 'Providence-New Bedford', 0.004),
    ('577', 'Tucson', 0.003),
    ('600', 'Honolulu', 0.003),
    ('554', 'Omaha', 0.003),
    ('555', 'Albuquerque-Santa Fe', 0.003);
    
    -- Insert sample CBSA regions (Core Based Statistical Areas)
    insert into #cbsa_regions (cbs_code, cbsa_name, population_weight) values
    ('35620', 'New York-Newark-Jersey City', 0.064),
    ('31080', 'Los Angeles-Long Beach-Anaheim', 0.043),
    ('16980', 'Chicago-Naperville-Elgin', 0.031),
    ('19100', 'Dallas-Fort Worth-Arlington', 0.025),
    ('26420', 'Houston-The Woodlands-Sugar Land', 0.024),
    ('47900', 'Washington-Arlington-Alexandria', 0.022),
    ('33100', 'Miami-Fort Lauderdale-Pompano Beach', 0.02),
    ('37980', 'Philadelphia-Camden-Wilmington', 0.02),
    ('12060', 'Atlanta-Sandy Springs-Alpharetta', 0.021),
    ('38060', 'Phoenix-Mesa-Chandler', 0.018),
    ('14460', 'Boston-Cambridge-Newton', 0.016),
    ('41860', 'San Francisco-Oakland-Berkeley', 0.016),
    ('40140', 'Riverside-San Bernardino-Ontario', 0.015),
    ('19740', 'Denver-Aurora-Lakewood', 0.011),
    ('42660', 'Seattle-Tacoma-Bellevue', 0.014),
    ('33460', 'Minneapolis-St. Paul-Bloomington', 0.012),
    ('41740', 'San Diego-Chula Vista-Carlsbad', 0.011),
    ('45300', 'Tampa-St. Petersburg-Clearwater', 0.011),
    ('41180', 'St. Louis', 0.009),
    ('12580', 'Baltimore-Columbia-Towson', 0.009),
    ('38900', 'Portland-Vancouver-Hillsboro', 0.009),
    ('40900', 'Sacramento-Roseville-Folsom', 0.008),
    ('38300', 'Pittsburgh', 0.008),
    ('19820', 'Detroit-Warren-Dearborn', 0.014),
    ('16740', 'Charlotte-Concord-Gastonia', 0.009),
    ('41700', 'San Antonio-New Braunfels', 0.009),
    ('28140', 'Kansas City', 0.007),
    ('27260', 'Jacksonville', 0.005),
    ('35380', 'New Orleans-Metairie', 0.004),
    ('17140', 'Cincinnati', 0.007),
    ('18140', 'Columbus, OH', 0.008),
    ('26900', 'Indianapolis-Carmel-Anderson', 0.007),
    ('12420', 'Austin-Round Rock-Georgetown', 0.008),
    ('29820', 'Las Vegas-Henderson-Paradise', 0.008),
    ('39580', 'Raleigh-Cary', 0.006),
    ('36740', 'Orlando-Kissimmee-Sanford', 0.01),
    ('31140', 'Louisville/Jefferson County', 0.004),
    ('25540', 'Hartford-East Hartford-Middletown', 0.004),
    ('41620', 'Salt Lake City', 0.004),
    ('34980', 'Nashville-Davidson--Murfreesboro--Franklin', 0.007),
    ('13820', 'Birmingham-Hoover', 0.004),
    ('36420', 'Oklahoma City', 0.005),
    ('47260', 'Virginia Beach-Norfolk-Newport News', 0.006),
    ('39300', 'Providence-Warwick', 0.005),
    ('32820', 'Memphis', 0.004),
    ('40060', 'Richmond', 0.004),
    ('15380', 'Buffalo-Cheektowaga', 0.004),
    ('17460', 'Cleveland-Elyria', 0.007),
    ('26620', 'Huntsville', 0.002),
    ('46060', 'Tucson', 0.003);
    
    -- Insert sample ZIP codes with population weights
    insert into #zip_codes (zip_code, state_code, population_weight) values
    ('10001', 'NY', 0.0015),
    ('10002', 'NY', 0.0018),
    ('10003', 'NY', 0.0016),
    ('10016', 'NY', 0.0014),
    ('10019', 'NY', 0.0013),
    ('10021', 'NY', 0.0012),
    ('10025', 'NY', 0.0017),
    ('10128', 'NY', 0.0011),
    ('11201', 'NY', 0.0014),
    ('11215', 'NY', 0.0013),
    ('11220', 'NY', 0.0016),
    ('11235', 'NY', 0.0012),
    ('90001', 'CA', 0.0014),
    ('90011', 'CA', 0.0017),
    ('90026', 'CA', 0.0013),
    ('90210', 'CA', 0.0008),
    ('90250', 'CA', 0.0012),
    ('90291', 'CA', 0.0009),
    ('94016', 'CA', 0.0011),
    ('94102', 'CA', 0.0010),
    ('94110', 'CA', 0.0013),
    ('94122', 'CA', 0.0012),
    ('60601', 'IL', 0.0008),
    ('60614', 'IL', 0.0011),
    ('60622', 'IL', 0.0012),
    ('60640', 'IL', 0.0013),
    ('60657', 'IL', 0.0014),
    ('77001', 'TX', 0.0007),
    ('77002', 'TX', 0.0009),
    ('77024', 'TX', 0.0011),
    ('77056', 'TX', 0.0010),
    ('77096', 'TX', 0.0012),
    ('75001', 'TX', 0.0008),
    ('75204', 'TX', 0.0011),
    ('75225', 'TX', 0.0009),
    ('33101', 'FL', 0.0006),
    ('33139', 'FL', 0.0010),
    ('33156', 'FL', 0.0009),
    ('33180', 'FL', 0.0008),
    ('33301', 'FL', 0.0007),
    ('33401', 'FL', 0.0008),
    ('33480', 'FL', 0.0006),
    ('19102', 'PA', 0.0007),
    ('19103', 'PA', 0.0009),
    ('19106', 'PA', 0.0006),
    ('19130', 'PA', 0.0008),
    ('19147', 'PA', 0.0010),
    ('02108', 'MA', 0.0005),
    ('02116', 'MA', 0.0008),
    ('02118', 'MA', 0.0007),
    ('02199', 'MA', 0.0004),
    ('20001', 'DC', 0.0008),
    ('20007', 'DC', 0.0006),
    ('20016', 'DC', 0.0007),
    ('20036', 'DC', 0.0005),
    ('30303', 'GA', 0.0006),
    ('30305', 'GA', 0.0008),
    ('30309', 'GA', 0.0007),
    ('30319', 'GA', 0.0009),
    ('30328', 'GA', 0.0008),
    ('98101', 'WA', 0.0006),
    ('98105', 'WA', 0.0008),
    ('98115', 'WA', 0.0009),
    ('98199', 'WA', 0.0007),
    ('85001', 'AZ', 0.0005),
    ('85016', 'AZ', 0.0008),
    ('85251', 'AZ', 0.0007),
    ('85282', 'AZ', 0.0009),
    ('80202', 'CO', 0.0006),
    ('80206', 'CO', 0.0007),
    ('80209', 'CO', 0.0008),
    ('80220', 'CO', 0.0009);
    
    -- Create a table to store generated points
    create table #generated_points (
        id int identity(1,1),
        lat float,
        lon float,
        dma_code varchar(16),
        cbs_code varchar(16),
        zip_code varchar(16)
    );
    
    -- Variables for the generation process
    declare @points_remaining int = @point_count;
    declare @batch_size int = 10000; -- Process in batches for better performance
    declare @current_batch int;
    
    -- State boundaries (approximate)
    create table #state_boundaries (
        state_code char(2),
        min_lat float,
        max_lat float,
        min_lon float,
        max_lon float
    );
    
    -- Insert approximate state boundaries
    insert into #state_boundaries (state_code, min_lat, max_lat, min_lon, max_lon) values
    ('AL', 30.22, 35.00, -88.47, -84.89),
    ('AK', 51.20, 71.50, -179.15, -130.00),
    ('AZ', 31.33, 37.00, -114.82, -109.05),
    ('AR', 33.00, 36.50, -94.62, -89.64),
    ('CA', 32.53, 42.00, -124.48, -114.13),
    ('CO', 37.00, 41.00, -109.06, -102.04),
    ('CT', 40.98, 42.05, -73.73, -71.79),
    ('DE', 38.45, 39.84, -75.79, -75.05),
    ('FL', 24.52, 31.00, -87.63, -80.03),
    ('GA', 30.36, 35.00, -85.61, -80.84),
    ('HI', 18.91, 22.24, -160.24, -154.81),
    ('ID', 42.00, 49.00, -117.24, -111.04),
    ('IL', 36.97, 42.51, -91.51, -87.50),
    ('IN', 37.77, 41.76, -88.10, -84.78),
    ('IA', 40.38, 43.50, -96.64, -90.14),
    ('KS', 37.00, 40.00, -102.05, -94.59),
    ('KY', 36.50, 39.15, -89.57, -81.96),
    ('LA', 28.93, 33.02, -94.04, -88.82),
    ('ME', 43.06, 47.46, -71.08, -66.95),
    ('MD', 37.91, 39.72, -79.49, -75.05),
    ('MA', 41.24, 42.89, -73.50, -69.93),
    ('MI', 41.70, 48.30, -90.42, -82.13),
    ('MN', 43.50, 49.38, -97.24, -89.53),
    ('MS', 30.19, 35.00, -91.65, -88.10),
    ('MO', 35.99, 40.61, -95.77, -89.10),
    ('MT', 44.36, 49.00, -116.05, -104.04),
    ('NE', 40.00, 43.00, -104.05, -95.31),
    ('NV', 35.00, 42.00, -120.00, -114.04),
    ('NH', 42.70, 45.31, -72.56, -70.71),
    ('NJ', 38.93, 41.36, -75.56, -73.89),
    ('NM', 31.33, 37.00, -109.05, -103.00),
    ('NY', 40.50, 45.01, -79.76, -71.85),
    ('NC', 33.84, 36.59, -84.32, -75.46),
    ('ND', 45.94, 49.00, -104.05, -96.55),
    ('OH', 38.40, 42.33, -84.82, -80.52),
    ('OK', 33.62, 37.00, -103.00, -94.43),
    ('OR', 42.00, 46.29, -124.57, -116.47),
    ('PA', 39.72, 42.27, -80.52, -74.70),
    ('RI', 41.14, 42.02, -71.86, -71.12),
    ('SC', 32.04, 35.22, -83.35, -78.54),
    ('SD', 42.48, 45.95, -104.06, -96.44),
    ('TN', 34.98, 36.68, -90.31, -81.65),
    ('TX', 25.84, 36.50, -106.65, -93.51),
    ('UT', 37.00, 42.00, -114.05, -109.04),
    ('VT', 42.73, 45.02, -73.44, -71.47),
    ('VA', 36.54, 39.47, -83.68, -75.24),
    ('WA', 45.54, 49.00, -124.77, -116.92),
    ('WV', 37.20, 40.64, -82.64, -77.72),
    ('WI', 42.49, 47.08, -92.89, -86.76),
    ('WY', 41.00, 45.01, -111.06, -104.05),
    ('DC', 38.79, 39.00, -77.12, -76.91);
    
    -- Process in batches
    while @points_remaining > 0
    begin
        set @current_batch = case when @points_remaining > @batch_size then @batch_size else @points_remaining end;
        
        -- Generate points based on state population weights
        insert into #generated_points (lat, lon, dma_code, cbs_code, zip_code)
        select 
            -- Generate random latitude within state boundaries with some clustering
            sb.min_lat + (sb.max_lat - sb.min_lat) * power(rand(checksum(newid())), 0.5),
            -- Generate random longitude within state boundaries with some clustering
            sb.min_lon + (sb.max_lon - sb.min_lon) * power(rand(checksum(newid())), 0.5),
            -- Assign DMA code based on probability
            (select top 1 dma_code from #dma_regions order by rand(checksum(newid())) / population_weight),
            -- Assign CBSA code based on probability
            (select top 1 cbs_code from #cbsa_regions order by rand(checksum(newid())) / population_weight),
            -- Assign ZIP code based on probability
            (select top 1 zc.zip_code from #zip_codes zc where zc.state_code = s.state_code order by rand(checksum(newid())) / zc.population_weight)
        from 
            (select top (@current_batch) 
                state_code,
                row_number() over (order by newid()) as rn
             from #states
             cross join (select top (cast(ceiling(@current_batch / 50.0) as int)) n = row_number() over (order by (select null)) from sys.objects) as nums
             order by population_weight desc, newid()
            ) as s
        join #state_boundaries sb on s.state_code = sb.state_code
        order by s.rn;
        
        set @points_remaining = @points_remaining - @current_batch;
    end
    
    -- Insert the generated points into the sites table
    insert into dbo.sites (
        uuid,
        dma_code,
        cbs_code,
        zip_code,
        geo,
        geohash9,
        geohash8,
        geohash7,
        geohash6,
        geohash5,
        geohash4,
        geohash3,
        geohash2,
        geohash1
    )
    select
        newid(),
        dma_code,
        cbs_code,
        zip_code,
        geography::Point(lat, lon, 4326),
        dbo.geohash_encode(lat, lon, 9),
        dbo.geohash_encode(lat, lon, 8),
        dbo.geohash_encode(lat, lon, 7),
        dbo.geohash_encode(lat, lon, 6),
        dbo.geohash_encode(lat, lon, 5),
        dbo.geohash_encode(lat, lon, 4),
        dbo.geohash_encode(lat, lon, 3),
        dbo.geohash_encode(lat, lon, 2),
        dbo.geohash_encode(lat, lon, 1)
    from #generated_points;
    
    -- Clean up temporary tables
    drop table #generated_points;
    drop table #states;
    drop table #dma_regions;
    drop table #cbsa_regions;
    drop table #zip_codes;
    drop table #state_boundaries;
    
    -- Return the count of inserted points
    select @point_count as points_inserted;
end
go

grant execute
    on object::dbo.sites_populate to vector_tile_server_app
    as dbo;
go