﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Damselfly.Core.DbModels.Models;
using Damselfly.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Damselfly.Core.Services;

public class StatisticsService
{
    private readonly ILogger<StatisticsService> _logger;

    public StatisticsService( ILogger<StatisticsService> logger )
    {
        _logger = logger;
    }

    public async Task<Statistics> GetStatistics()
    {
        using var db = new ImageContext();

        var stats = new Statistics
        {
            TotalImages = await db.Images.CountAsync(),
            TotalTags = await db.Tags.CountAsync(),
            TotalFolders = await db.Folders.CountAsync(),
            TotalImagesSizeBytes = await db.Images.SumAsync(x => (long)x.FileSizeBytes),
            PeopleFound = await db.People.CountAsync(),
            PeopleIdentified = await db.People.Where(x => x.Name != "Unknown").CountAsync(),
            ObjectsRecognised = await db.ImageObjects.CountAsync(),
            PendingAIScans = await db.ImageMetaData.Where(x => !x.AILastUpdated.HasValue).CountAsync(),
            PendingThumbs = await db.ImageMetaData.Where(x => !x.ThumbLastUpdated.HasValue).CountAsync(),
            PendingImages = await db.Images.Where(x => x.MetaData == null || x.LastUpdated > x.MetaData.LastUpdated).Include(x => x.MetaData).CountAsync(),
            PendingKeywordOps = await db.KeywordOperations.Where(x => x.State == ExifOperation.FileWriteState.Pending).CountAsync(),
            PendingKeywordImages = await db.KeywordOperations.Where(x => x.State == ExifOperation.FileWriteState.Pending)
                                                    .Select(x => x.ImageId)
                                                    .Distinct().CountAsync(),
        };

        // TODO: Should pull this out of the TransThrottle instance.
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0);
        var monthEnd = monthStart.AddMonths(1).AddSeconds(-1);
        var totalTrans = await db.CloudTransactions
                                 .Where(x => x.Date >= monthStart && x.Date <= monthEnd)
                                 .SumAsync(x => x.TransCount);

        if (totalTrans > 0)
            stats.AzureMonthlyTransactions = $"{totalTrans} (during {monthStart:MMM})";

        return stats;
    }
}

