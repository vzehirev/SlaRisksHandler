﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using XlProcessor.Db;

namespace XlProcessor.Db.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    partial class ApplicationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.8")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("XlProcessor.Models.RiskRecord", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("DxcStatus")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("LastStatusChange")
                        .HasColumnType("datetime2");

                    b.Property<double>("TotalHoldHours")
                        .HasColumnType("float");

                    b.Property<string>("VLookupName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("RiskRecords");
                });

            modelBuilder.Entity("XlProcessor.Models.RiskStatusChange", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTime>("ChangedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("NewStatus")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("OldStatus")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("RiskRecordId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("RiskRecordId");

                    b.ToTable("StatusChanges");
                });

            modelBuilder.Entity("XlProcessor.Models.RiskStatusChange", b =>
                {
                    b.HasOne("XlProcessor.Models.RiskRecord", "RiskRecord")
                        .WithMany("StatusChanges")
                        .HasForeignKey("RiskRecordId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
