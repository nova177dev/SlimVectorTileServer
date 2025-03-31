using Dapper;
using SlimVectorTileServer.Application.Common;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;

namespace SlimVectorTileServer.Infrastructure.Data
{
    public class AppDbDataContext
    {
        private readonly IDbConnection _dbConnection;
        private readonly JsonHelper _jsonHelper;
        public AppDbDataContext(IDbConnection dbConnection, JsonHelper jsonHelper)
        {
            _dbConnection = dbConnection;
            _jsonHelper = jsonHelper;
        }

        public DataSet RequestDbForDataSet(string schema, string storedProcedureName, object requestParams)
        {
            using (var command = _dbConnection.CreateCommand())
            {
                command.CommandText = $"{schema}.{storedProcedureName}";
                command.CommandType = CommandType.StoredProcedure;

                foreach (var param in requestParams.GetType().GetProperties())
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = param.Name;
                    parameter.Value = param.GetValue(requestParams) ?? DBNull.Value;
                    command.Parameters.Add(parameter);
                }

                var dataSet = new DataSet();
                using (var adapter = new SqlDataAdapter((SqlCommand)command))
                {
                    adapter.Fill(dataSet);
                }

                if (dataSet.Tables.Count == 0)
                {
                    throw new InvalidOperationException("The database query didn't return any data.");
                }

                return dataSet;
            }
        }

        public JsonElement RequestDbForJson(string schema, string storedProcedureName, object requestParams)
        {
            string str = _dbConnection.ConnectionString;
            string? jsonResponse = _dbConnection.QueryFirstOrDefault<string>(
                schema + "." + storedProcedureName,
                new { @params = _jsonHelper.SerializeObject(requestParams) },
                commandType: CommandType.StoredProcedure
            );

            return _jsonHelper.DeserializeJson<JsonElement>(jsonResponse);
        }

        public byte[] requestDb(string schema, string storedProcedureName, object requestParams)
        {
            byte[]? dbResponse = _dbConnection.QueryFirstOrDefault<byte[]>(
                schema + "." + storedProcedureName,
                new { @params = _jsonHelper.SerializeObject(requestParams) },
                commandType: CommandType.StoredProcedure
            );

            if (dbResponse == null)
            {
                throw new InvalidOperationException("The database query didn't return any data.");
            }

            return dbResponse;
        }
    }
}
