using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using ServiceDeskDesktop.Models;
using System;
using System.Collections.Generic;

namespace ServiceDeskDesktop.Services
{
    public class LocalDatabaseService
    {
        private readonly string connectionString = "Data Source=localdata.db";

        public LocalDatabaseService()
        {
            using var con = new SqliteConnection(connectionString);
            con.Open();
            var cmd = con.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Requests (
                    DocumentId TEXT PRIMARY KEY,
                    JsonData TEXT,
                    IsDirty INTEGER DEFAULT 0,
                    PendingOperation TEXT
                );";
            cmd.ExecuteNonQuery();
        }

        public void SaveRequest(ServiceRequest request, bool isDirty = false, string pendingOperation = null)
        {
            using var con = new SqliteConnection(connectionString);
            con.Open();
            var cmd = con.CreateCommand();
            cmd.CommandText = @"
                INSERT OR REPLACE INTO Requests (DocumentId, JsonData, IsDirty, PendingOperation)
                VALUES (@id, @json, @dirty, @op)";
            cmd.Parameters.AddWithValue("@id", request.DocumentId ?? request.Id);
            cmd.Parameters.AddWithValue("@json", JsonConvert.SerializeObject(request));
            cmd.Parameters.AddWithValue("@dirty", isDirty ? 1 : 0);
            cmd.Parameters.AddWithValue("@op", (object)pendingOperation ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        public List<ServiceRequest> GetAllRequests()
        {
            var list = new List<ServiceRequest>();
            using var con = new SqliteConnection(connectionString);
            con.Open();
            var cmd = con.CreateCommand();
            cmd.CommandText = "SELECT JsonData FROM Requests";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(JsonConvert.DeserializeObject<ServiceRequest>(reader.GetString(0)));
            }
            return list;
        }

        public void DeleteRequest(string documentId)
        {
            using var con = new SqliteConnection(connectionString);
            con.Open();
            var cmd = con.CreateCommand();
            cmd.CommandText = "DELETE FROM Requests WHERE DocumentId = @id";
            cmd.Parameters.AddWithValue("@id", documentId);
            cmd.ExecuteNonQuery();
        }

        public List<(ServiceRequest request, string operation)> GetPendingOperations()
        {
            var list = new List<(ServiceRequest, string)>();
            using var con = new SqliteConnection(connectionString);
            con.Open();
            var cmd = con.CreateCommand();
            cmd.CommandText = "SELECT JsonData, PendingOperation FROM Requests WHERE IsDirty = 1";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var req = JsonConvert.DeserializeObject<ServiceRequest>(reader.GetString(0));
                string op = reader.IsDBNull(1) ? null : reader.GetString(1);
                list.Add((req, op));
            }
            return list;
        }
        public void MarkAsClean(string documentId)
        {
            using var con = new SqliteConnection(connectionString);
            con.Open();
            var cmd = con.CreateCommand();
            cmd.CommandText = "UPDATE Requests SET IsDirty = 0, PendingOperation = NULL WHERE DocumentId = @id";
            cmd.Parameters.AddWithValue("@id", documentId);
            cmd.ExecuteNonQuery();
        }
    }
}