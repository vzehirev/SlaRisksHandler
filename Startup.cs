using LinqToExcel;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using XlProcessor.Models;
using XlProcessor.Db;
using System;
using System.Data.OleDb;
using System.Data;
using System.Collections.Generic;

namespace XlProcessor
{
    class Startup
    {
        static void Main()
        {
            //try
            //{
            var config = Config.Get();

            var file = config["FileFolder"] + config["FileName"];

            // If the file is being opened and/or edited by another program - return
            if (IsFileLocked(new FileInfo(file)))
            {
                return;
            }

            // Create backup
            File.Copy(file, $"{config["FileFolder"]}{DateTime.UtcNow:dd-MMM-yy-HH-mm}-{config["FileName"]}");

            var excelClient = new ExcelQueryFactory(file);

            // Load all the records from the file in memory as a dictionary
            var fileRecords = excelClient.Worksheet<RiskRecord>("Raw Data")
                .Where(x => x.VLookupName != "")
                .ToDictionary(x => x.VLookupName);

            using (var dbContext = new ApplicationDbContext())
            {
                // If there're pending migrations - apply them
                if (dbContext.Database.GetPendingMigrations().Count() > 0)
                {
                    dbContext.Database.Migrate();
                }

                // Load all the records from db in memory as dictionary
                var dbRecords = dbContext.RiskRecords.ToDictionary(x => x.VLookupName);

                // Dictionary containing VLookupName and TotalHoldHours for records with updated TotalHoldHours info
                // Used for updating the sheets file
                var fileChangesToBeMade = new Dictionary<string, double>();

                // VLookupName is considered unique, if the value doesn't exist in the db - add it,
                // if it exist and is with different status, change its status and create a new status change record in db
                foreach (var fileRecord in fileRecords)
                {
                    if (!dbRecords.ContainsKey(fileRecord.Key))
                    {
                        dbContext.RiskRecords.Add(fileRecord.Value);
                    }
                    else if (fileRecord.Value.DxcStatus != dbRecords[fileRecord.Key].DxcStatus)
                    {
                        var statusChange = new RiskStatusChange
                        {
                            RiskRecordId = dbRecords[fileRecord.Key].Id,
                            OldStatus = dbRecords[fileRecord.Key].DxcStatus,
                            NewStatus = fileRecord.Value.DxcStatus,
                            ChangedAt = fileRecord.Value.LastStatusChange ?? DateTime.UtcNow,
                        };

                        dbContext.StatusChanges.Add(statusChange);

                        dbRecords[fileRecord.Key].DxcStatus = statusChange.NewStatus;

                        // If there's a status change and old status was On Hold add the hold duration to
                        // the TotalHoldHours
                        if (statusChange.OldStatus.ToLower().Contains("hold"))
                        {
                            var holdTime = statusChange.ChangedAt - dbRecords[fileRecord.Key].LastStatusChange;

                            // 30 mins = 1 hr hold time, 1 hr 30 mins = 2 hr hold time
                            if (holdTime.Value.TotalHours % 1 >= 0.5)
                            {
                                dbRecords[fileRecord.Key].TotalHoldHours += Math.Ceiling(holdTime.Value.TotalHours);
                            }
                            // 29 mins = 0 hrs hold time, 1 hr 29 mins = 1 hr hold time
                            else
                            {
                                dbRecords[fileRecord.Key].TotalHoldHours += Math.Floor(holdTime.Value.TotalHours);
                            }

                            fileChangesToBeMade.Add(dbRecords[fileRecord.Key].VLookupName, dbRecords[fileRecord.Key].TotalHoldHours);
                        }

                        dbRecords[fileRecord.Key].LastStatusChange = statusChange.ChangedAt;

                        dbContext.RiskRecords.Update(dbRecords[fileRecord.Key]);
                    }
                }
                dbContext.SaveChanges();

                // After data is saved in DB try updating the sheets file
                UpdateTotalHoldHoursInExcel(fileChangesToBeMade, file);
            }
            //}
            //catch (Exception ex)
            //{
            //    var errorMessage = $"{DateTime.UtcNow} - {ex.Message}{Environment.NewLine}";
            //    var filePath = Environment.CurrentDirectory + "\\SlaRiskHandlerErrorLog.txt";
            //    File.AppendAllText(filePath, errorMessage);
            //}
        }

        private static bool IsFileLocked(FileInfo file)
        {
            using (FileStream stream = file.OpenRead())
            {
                stream.Close();
                return false;
            }
        }

        private static void UpdateTotalHoldHoursInExcel(Dictionary<string, double> changes, string file)
        {

            var oleDbConnStr = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={file};Extended Properties=\"Excel 12.0;HDR = YES\";";

            using (var conn = new OleDbConnection(oleDbConnStr))
            {
                conn.Open();
                var cmd = new OleDbCommand();
                cmd.Connection = conn;

                foreach (var record in changes)
                {
                    cmd.CommandText = $"UPDATE [Raw Data$] SET [Total Hold Hours] = {record.Value} WHERE [Vlookup Name] = '{record.Key}';";
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
