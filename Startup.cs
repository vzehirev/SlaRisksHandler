using LinqToExcel;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using XlProcessor.Models;
using XlProcessor.Db;
using System;

namespace XlProcessor
{
    class Startup
    {
        static void Main()
        {
            try
            {
                var config = Config.Get();

                var originalFile = config["FileFolder"] + config["FileName"];

                // If the file is being opened and/or edited by another program - return
                if (IsFileLocked(new FileInfo(originalFile)))
                {
                    return;
                }

                var copiedFile = config["FileFolder"] + "temp-" + config["FileName"];

                // Copy the file, so the app can work with an independent instance of the file
                File.Copy(originalFile, copiedFile, true);

                var excelClient = new ExcelQueryFactory(copiedFile);

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

                    // VLookupName is considered unique, if the value doesn't exist in the db - add it,
                    // if it exist and is with different status, change its status and create a new status change record in db
                    foreach (var fileRecord in fileRecords)
                    {
                        var vLookupName = fileRecord.Key;

                        if (!dbRecords.ContainsKey(vLookupName))
                        {
                            dbContext.RiskRecords.Add(fileRecord.Value);
                        }
                        else if (fileRecord.Value.DxcStatus != dbRecords[vLookupName].DxcStatus)
                        {
                            var statusChange = new RiskStatusChange
                            {
                                RiskRecordId = dbRecords[vLookupName].Id,
                                OldStatus = dbRecords[vLookupName].DxcStatus,
                                NewStatus = fileRecord.Value.DxcStatus,
                                ChangedAt = fileRecord.Value.LastStatusChange ?? DateTime.UtcNow,
                            };

                            dbContext.StatusChanges.Add(statusChange);

                            dbRecords[vLookupName].DxcStatus = statusChange.NewStatus;

                            // If there's a status change and old status was On Hold add the hold duration to
                            // the the TotalHoldHours (smallest step is 1 hr, so using only whole numbers)
                            if (statusChange.OldStatus.ToLower().Contains("hold"))
                            {
                                var holdTime = statusChange.ChangedAt - dbRecords[vLookupName].LastStatusChange;

                                dbRecords[vLookupName].TotalHoldHours += Math.Ceiling(holdTime.Value.TotalHours);
                            }

                            dbRecords[vLookupName].LastStatusChange = statusChange.ChangedAt;

                            dbContext.RiskRecords.Update(dbRecords[vLookupName]);
                        }
                    }
                    dbContext.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"{DateTime.UtcNow} - {ex.Message}{Environment.NewLine}";
                var filePath = Environment.CurrentDirectory + "\\SlaRiskHandlerErrorLog.txt";
                File.AppendAllText(filePath, errorMessage);
            }
        }

        private static bool IsFileLocked(FileInfo file)
        {
            using (FileStream stream = file.OpenRead())
            {
                stream.Close();
                return false;
            }
        }
    }
}
