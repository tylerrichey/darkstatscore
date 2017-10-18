﻿// <auto-generated />
using DarkStatsCore.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using System;

namespace DarkStatsCore.Data.Migrations
{
    [DbContext(typeof(DarkStatsDbContext))]
    partial class DarkStatsDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.0.0-rtm-26452");

            modelBuilder.Entity("DarkStatsCore.Data.Settings", b =>
                {
                    b.Property<string>("Name")
                        .ValueGeneratedOnAdd();

                    b.Property<double>("DoubleValue");

                    b.Property<int>("IntValue");

                    b.Property<string>("StringValue");

                    b.HasKey("Name");

                    b.ToTable("Settings");
                });

            modelBuilder.Entity("DarkStatsCore.Data.TrafficStats", b =>
                {
                    b.Property<string>("Ip");

                    b.Property<DateTime>("Day");

                    b.Property<string>("Hostname");

                    b.Property<long>("In");

                    b.Property<string>("LastSeen");

                    b.Property<string>("Mac");

                    b.Property<long>("Out");

                    b.HasKey("Ip", "Day");

                    b.ToTable("TrafficStats");
                });
#pragma warning restore 612, 618
        }
    }
}