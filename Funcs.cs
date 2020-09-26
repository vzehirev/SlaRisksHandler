using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.IO;
using XlProcessor.Models;

namespace XlProcessor
{
    static class Funcs
    {
        // Get appSettings.json config file
        public static IConfigurationRoot GetConfig()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("appSettings.json", true, true).Build();
        }

        public static bool IsFileLocked(FileInfo file)
        {
            using (FileStream stream = file.OpenRead())
            {
                stream.Close();
                return false;
            }
        }

        public static void UpdateRecordHoldHours(RiskRecord riskRecord, DateTime? changedAt = null)
        {
            changedAt = changedAt == null ? DateTime.UtcNow : changedAt;

            var holdTime = changedAt - (riskRecord.LastTotalHoldHoursUpdate ?? riskRecord.LastStatusChange);

            riskRecord.LastTotalHoldHoursUpdate = DateTime.UtcNow;

            // 30 mins = 1 hr hold time, 1 hr 30 mins = 2 hr hold time
            if (holdTime.Value.TotalHours % 1 >= 0.5)
            {
                riskRecord.TotalHoldHours += Math.Ceiling(holdTime.Value.TotalHours);
            }
            // 29 mins = 0 hrs hold time, 1 hr 29 mins = 1 hr hold time
            else
            {
                riskRecord.TotalHoldHours += Math.Floor(holdTime.Value.TotalHours);
            }
        }

        public static void UpdateTotalHoldHoursInSheet(Dictionary<string, double> changes, string file)
        {

            var oleDbConnStr = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={file};Extended Properties=\"Excel 12.0;HDR = YES\";";

            using (var conn = new OleDbConnection(oleDbConnStr))
            {
                conn.Open();
                var cmd = new OleDbCommand();
                cmd.Connection = conn;

                foreach (var record in changes)
                {
                    cmd.CommandText = $"UPDATE [Raw Data$] SET [Total Hold Hours] = {record.Value} WHERE [Vlookup Name] = '{record.Key}'";
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
