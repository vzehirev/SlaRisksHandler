using LinqToExcel;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using XlProcessor.Models;
using XlProcessor.Db;
using System;
using System.Data;
using System.Collections.Generic;

namespace XlProcessor
{
    class Startup
    {
        static void Main()
        {
            try
            {
                var config = Funcs.GetConfig();

                var file = config["FileFolder"] + config["FileName"];

                // If the file is being opened and/or edited by another program - stop
                if (Funcs.IsFileLocked(new FileInfo(file)))
                {
                    return;
                }

                // Create backup
                File.Copy(file, $"{config["FileFolder"]}{DateTime.UtcNow:dd-MMM-yy-HH-mm-ss}-{config["FileName"]}");

                var excelClient = new ExcelQueryFactory(file);

                // Load all the records from the file in memory as a dictionary (VLookupName as key)
                var fileRecords = excelClient.Worksheet<RiskRecord>("Raw Data").Where(x => x.VLookupName != "");

                using (var dbContext = new ApplicationDbContext())
                {
                    // If there're pending migrations - apply them
                    if (dbContext.Database.GetPendingMigrations().Count() > 0)
                    {
                        dbContext.Database.Migrate();
                    }

                    // Load all the records from db in memory as dictionary (VLookupName as key)
                    var dbRecords = dbContext.RiskRecords.ToDictionary(x => x.VLookupName);

                    // Dictionary containing VLookupName and TotalHoldHours for records with updated TotalHoldHours info
                    // Used for updating the sheets file (VLookupName as is key)
                    var fileChangesToBeMade = new Dictionary<string, double>();

                    // VLookupName is considered unique, if the value doesn't exist in the db - add it,
                    // if it exist and is with different status, change its status and create a new status change record in db
                    foreach (var fileRecord in fileRecords)
                    {
                        if (!dbRecords.ContainsKey(fileRecord.VLookupName))
                        {
                            dbContext.RiskRecords.Add(fileRecord);
                        }
                        else if (fileRecord.DxcStatus != dbRecords[fileRecord.VLookupName].DxcStatus)
                        {
                            var statusChange = new RiskStatusChange
                            {
                                RiskRecordId = dbRecords[fileRecord.VLookupName].Id,
                                OldStatus = dbRecords[fileRecord.VLookupName].DxcStatus,
                                NewStatus = fileRecord.DxcStatus,
                                ChangedAt = fileRecord.LastStatusChange ?? DateTime.UtcNow,
                            };

                            dbContext.StatusChanges.Add(statusChange);

                            dbRecords[fileRecord.VLookupName].DxcStatus = statusChange.NewStatus;

                            // If there's a status change, old status was On Hold and we have LastStatusChange
                            // calculate and add the hold duration to the TotalHoldHours
                            if (statusChange.OldStatus.ToLower().Contains("hold")
                                && dbRecords[fileRecord.VLookupName].LastStatusChange != null)
                            {
                                Funcs.UpdateRecordHoldHours(dbRecords[fileRecord.VLookupName], statusChange.ChangedAt);

                                // Add the new hold hours value in the dictionary (VLookupName is key, TotalHoldHours is value)
                                fileChangesToBeMade.Add(dbRecords[fileRecord.VLookupName].VLookupName, dbRecords[fileRecord.VLookupName].TotalHoldHours);
                            }

                            dbRecords[fileRecord.VLookupName].LastStatusChange = statusChange.ChangedAt;

                            dbContext.RiskRecords.Update(dbRecords[fileRecord.VLookupName]);
                        }
                        // If LastStatusChange was updated manually in the sheet
                        else if (dbRecords[fileRecord.VLookupName].LastStatusChange == null && fileRecord.LastStatusChange != null)
                        {
                            dbRecords[fileRecord.VLookupName].LastStatusChange = fileRecord.LastStatusChange;
                        }

                        // If record is on hold and we have from when (LastStatusChange) - update what is its hold time until now
                        if (dbRecords.ContainsKey(fileRecord.VLookupName)
                            && dbRecords[fileRecord.VLookupName].DxcStatus.ToLower().Contains("hold")
                            && (dbRecords[fileRecord.VLookupName].LastTotalHoldHoursUpdate != null || dbRecords[fileRecord.VLookupName].LastStatusChange != null))
                        {
                            Funcs.UpdateRecordHoldHours(dbRecords[fileRecord.VLookupName]);
                            dbContext.RiskRecords.Update(dbRecords[fileRecord.VLookupName]);

                            fileChangesToBeMade.Add(dbRecords[fileRecord.VLookupName].VLookupName, dbRecords[fileRecord.VLookupName].TotalHoldHours);
                        }
                    }
                    dbContext.SaveChanges();

                    // After data is saved in DB - update the sheets file with the populated dictionary
                    Funcs.UpdateTotalHoldHoursInSheet(fileChangesToBeMade, file);
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"{DateTime.UtcNow} - {ex.Message}{Environment.NewLine}";
                var filePath = Environment.CurrentDirectory + "\\SlaRiskHandlerErrorLog.txt";
                File.AppendAllText(filePath, errorMessage);
            }
        }
    }
}
