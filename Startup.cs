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
            var config = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))
                .AddJsonFile("appSettings.json", true, true).Build();

            var originalFile = $"{config["FileFolder"]}{config["FileName"]}";
            var copiedFile = $@"{config["FileFolder"]}temp-{config["FileName"]}";

            File.Copy(originalFile, copiedFile, true);

            var excelClient = new ExcelQueryFactory(copiedFile);

            var fileRecords = excelClient.Worksheet<RiskRecord>("Raw Data")
                .Where(x => x.VLookupName != "")
                .ToDictionary(x => x.VLookupName);

            using (var dbContext = new ApplicationDbContext(config["ConnectionString"]))
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
                            RiskRecordVLookupName = vLookupName,
                            OldStatus = dbRecords[vLookupName].DxcStatus,
                            NewStatus = fileRecord.Value.DxcStatus,
                            ChangedAt = fileRecord.Value.LastStatusChange ?? DateTime.UtcNow,
                        };

                        dbContext.StatusChanges.Add(statusChange);

                        dbRecords[vLookupName].DxcStatus = statusChange.NewStatus;

                        if (statusChange.OldStatus == "On Hold")
                        {
                            var holdTime = statusChange.ChangedAt - dbRecords[vLookupName].LastStatusChange;

                            if (dbRecords[vLookupName].TotalHoldTime == null)
                            {
                                dbRecords[vLookupName].TotalHoldTime = new TimeSpan();
                            }
                            dbRecords[vLookupName].TotalHoldTime += holdTime;
                        }

                        dbRecords[vLookupName].LastStatusChange = statusChange.ChangedAt;

                        dbContext.RiskRecords.Update(dbRecords[vLookupName]);
                    }
                }
                dbContext.SaveChanges();
            }
        }
    }
}
