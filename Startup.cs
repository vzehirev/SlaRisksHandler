using LinqToExcel;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using XlProcessor.Models;
using XlProcessor.Db;
using System;
using Microsoft.Extensions.Configuration;
using System.Reflection;

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

                if (IsFileLocked(new FileInfo(originalFile)))
                {
                    return;
                }

                var copiedFile = config["FileFolder"] + "temp-" + config["FileName"];

                File.Copy(originalFile, copiedFile, true);

                var excelClient = new ExcelQueryFactory(copiedFile);

                var fileRecords = excelClient.Worksheet<RiskRecord>("Raw Data")
                    .Where(x => x.VLookupName != "")
                    .ToDictionary(x => x.VLookupName);

                using (var dbContext = new ApplicationDbContext())
                {
                    if (dbContext.Database.GetPendingMigrations().Count() > 0)
                    {
                        dbContext.Database.Migrate();
                    }

                    var dbRecords = dbContext.RiskRecords.ToDictionary(x => x.VLookupName);

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

                            if (statusChange.OldStatus == config["OnHoldStatusValue"])
                            {
                                var holdTime = statusChange.ChangedAt - dbRecords[vLookupName].LastStatusChange;

                                dbRecords[vLookupName].TotalHoldHours += holdTime.Value.TotalHours;
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
                var filePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\SlaRiskHandlerErrorLog.txt";
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
