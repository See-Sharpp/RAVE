﻿using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfApp2.Models;

namespace WpfApp2.Context
{
    public class ApplicationDbContext :DbContext
    {
        public DbSet<SignUpDetail> SignUpDetails { get; set; }

        public DbSet<AllExes> AllExes { get; set; }

        public DbSet<LLM_Detail> LLM_Detail { get; set; }

        public DbSet<AllDocs> AllDocs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "database.db");
                Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
                optionsBuilder.UseSqlite($"Data Source={dbPath}");
            }
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<AllExes>()
                       .HasIndex(e => new { e.FilePath, e.UserId })
                       .IsUnique();

            modelBuilder.Entity<AllDocs>()
                       .HasIndex(e => new { e.FilePath, e.UserId })
                       .IsUnique();
        }
    }
}
