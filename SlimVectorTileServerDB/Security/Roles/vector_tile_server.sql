create role vector_tile_server
go
alter role vector_tile_server add member vector_tile_server_app;
go
alter role db_datareader add member vector_tile_server;
go
alter role db_datawriter add member vector_tile_server;
go
alter role [db_datareader] add member vector_tile_server;
go
alter role [db_datawriter] add member vector_tile_server;
go

