﻿namespace GarageBuddy.Data.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading.Tasks;

    using GarageBuddy.Data.Common.Models;
    using GarageBuddy.Data.Common.Repositories;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.ChangeTracking;

    public class EfRepository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : BaseModel<TKey>
    {
        public EfRepository(ApplicationDbContext context)
        {
            this.Context = context ?? throw new ArgumentNullException(nameof(context));
            this.DbSet = this.Context.Set<TEntity>();
        }

        protected DbSet<TEntity> DbSet { get; set; }

        protected ApplicationDbContext Context { get; set; }

        public virtual IQueryable<TEntity> All(bool asReadonly)
        {
            return asReadonly ? this.DbSet.AsNoTracking() : this.DbSet;
        }

        public virtual IQueryable<TEntity> All(Expression<Func<TEntity, bool>> search, bool asReadonly)
        {
            var query = this.DbSet.Where(search);
            return asReadonly ? query.AsNoTracking() : query;
        }

        public virtual TEntity Find(TKey id, bool asReadonly = false)
        {
            ArgumentNullException.ThrowIfNull(id);

            var entity = this.DbSet.Find(id);

            if (entity == null)
            {
                throw new InvalidOperationException(string.Format(Errors.NoEntityWithPropertyFound, "entity", nameof(id)));
            }

            if (asReadonly)
            {
                this.Context.Entry(entity).State = EntityState.Detached;
            }

            return entity;
        }

        public virtual async Task<TEntity> FindAsync(TKey id, bool asReadonly = false)
        {
            ArgumentNullException.ThrowIfNull(id);

            var entity = await this.DbSet.FindAsync(id);

            if (entity == null)
            {
                throw new InvalidOperationException(string.Format(Errors.NoEntityWithPropertyFound, "entity", nameof(id)));
            }

            if (asReadonly)
            {
                this.Context.Entry(entity).State = EntityState.Detached;
            }

            return entity;
        }

        public virtual EntityEntry<TEntity> Add(TEntity entity)
        {
            entity.CreatedOn = DateTime.UtcNow;
            return this.DbSet.Add(entity);
        }

        public virtual async Task<EntityEntry<TEntity>> AddAsync(TEntity entity)
        {
            entity.CreatedOn = DateTime.UtcNow;
            return await this.DbSet.AddAsync(entity);
        }

        public virtual void AddRange(IEnumerable<TEntity> entities)
        {
            foreach (var entity in entities)
            {
                this.Add(entity);
            }
        }

        public virtual async Task AddRangeAsync(IEnumerable<TEntity> entities)
        {
            foreach (var entity in entities)
            {
                await this.AddAsync(entity);
            }
        }

        public virtual void Update(TEntity entity)
        {
            var entry = this.Context.Entry(entity);

            if (entry.State == EntityState.Detached)
            {
                this.DbSet.Attach(entity);
            }

            entry.Entity.ModifiedOn = DateTime.UtcNow;

            entry.State = EntityState.Modified;
        }

        public virtual async Task UpdateAsync(TKey id)
        {
            var entity = await this.FindAsync(id);

            if (entity == null)
            {
                throw new InvalidOperationException(string.Format(Errors.NoEntityWithPropertyFound, "entity", nameof(id)));
            }

            this.Update(entity);
        }

        public virtual void UpdateRange(IEnumerable<TEntity> entities)
        {
            foreach (var entity in entities)
            {
                this.Update(entity);
            }
        }

        public virtual void Delete(TEntity entity)
        {
            var entry = this.Context.Entry(entity);

            if (entry.State == EntityState.Detached)
            {
                this.DbSet.Attach(entity);
            }

            entry.State = EntityState.Deleted;
        }

        public virtual async Task DeleteAsync(TKey id)
        {
            var entity = await this.FindAsync(id);

            this.Delete(entity);
        }

        public virtual void DeleteRange(IEnumerable<TEntity> entities)
        {
            this.DbSet.RemoveRange(entities);
        }

        public virtual EntityEntry<TEntity> Detach(TEntity entity)
        {
            var entry = this.Context.Entry(entity);

            entry.State = EntityState.Detached;
            return entry;
        }

        public virtual async Task<int> SaveChangesAsync()
        {
            return await this.Context.SaveChangesAsync();
        }

        public async Task Truncate(string table)
        {
            await this.Context.Database.ExecuteSqlRawAsync($"TRUNCATE TABLE {table} RESTART IDENTITY");
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.Context?.Dispose();
            }
        }
    }
}
