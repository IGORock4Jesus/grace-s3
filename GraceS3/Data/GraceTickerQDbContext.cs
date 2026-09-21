using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TickerQ.EntityFrameworkCore.DbContextFactory;
using TickerQ.Utilities.Entities;

namespace GraceS3.Data;

public class GraceTickerQDbContext(DbContextOptions<GraceTickerQDbContext> options)
	: TickerQDbContext<TimeTickerEntity, CronTickerEntity>(options) { }

// public class MyTickerQDbContextFactory : IDesignTimeDbContextFactory<GraceTickerQDbContext>
// {
//     public GraceTickerQDbContext CreateDbContext(string[] args)
//     {
// 		DbContextOptionsBuilder<TickerQDbContext> optionsBuilder = new();
//         optionsBuilder.UseNpgsql("***;");

//         return new GraceTickerQDbContext(optionsBuilder.Options);
//     }
// }
