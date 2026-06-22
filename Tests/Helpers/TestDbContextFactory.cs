using Microsoft.EntityFrameworkCore;
using OrderManagement.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tests.Helpers
{
    public static class TestDbContextFactory
    {
        public static AppDbContext Create()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

            return new AppDbContext(options);
        }
    }
}
