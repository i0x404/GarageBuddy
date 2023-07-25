﻿namespace GarageBuddy.Data.Seeding.Vehicle
{
    using System;
    using System.Threading.Tasks;

    using Common;

    using Microsoft.EntityFrameworkCore;

    using Models.Vehicle;

    public class BrandSeeder : ISeeder
    {
        public async Task SeedAsync(ApplicationDbContext dbContext, IServiceProvider serviceProvider)
        {
            if (await dbContext.Brands.AnyAsync(b => b.IsSeeded) == false)
            {
                var brands = await SeedHelper.GetSeedDataFromJson<Brand>("BrandSeed.json");
                brands.ForEach(j => j.IsSeeded = true);
                await dbContext.Brands.AddRangeAsync(brands);
            }
        }
    }
}
