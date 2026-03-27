#nullable disable
using System;
using AppointmentService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace AppointmentService.Data.Migrations;

[DbContext(typeof(AppointmentDbContext))]
public partial class AppointmentDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "8.0.8");

        modelBuilder.Entity<Appointment>(b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedNever()
                .HasColumnType("uuid");

            b.Property<DateTime>("CreatedAt")
                .HasColumnType("timestamp with time zone");

            b.Property<Guid>("DoctorId")
                .HasColumnType("uuid");

            b.Property<Guid>("PatientId")
                .HasColumnType("uuid");

            b.Property<DateTime>("SlotTime")
                .HasColumnType("timestamp with time zone");

            b.Property<string>("Status")
                .IsRequired()
                .HasColumnType("text");

            b.HasKey("Id");

            b.HasIndex("DoctorId", "SlotTime")
                .IsUnique();

            b.ToTable("Appointments");
        });

        modelBuilder.Entity<IdempotencyRecord>(b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedNever()
                .HasColumnType("uuid");

            b.Property<Guid>("AppointmentId")
                .HasColumnType("uuid");

            b.Property<DateTime>("CreatedAt")
                .HasColumnType("timestamp with time zone");

            b.Property<string>("Key")
                .IsRequired()
                .HasColumnType("text");

            b.HasKey("Id");

            b.HasIndex("Key")
                .IsUnique();

            b.ToTable("IdempotencyRecords");
        });

        modelBuilder.Entity<OutboxMessage>(b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedNever()
                .HasColumnType("uuid");

            b.Property<string>("CorrelationId")
                .IsRequired()
                .HasColumnType("text");

            b.Property<DateTime>("CreatedAt")
                .HasColumnType("timestamp with time zone");

            b.Property<string>("EventName")
                .IsRequired()
                .HasColumnType("text");

            b.Property<string>("Payload")
                .IsRequired()
                .HasColumnType("text");

            b.Property<DateTime?>("ProcessedAt")
                .HasColumnType("timestamp with time zone");

            b.HasKey("Id");

            b.HasIndex("ProcessedAt");

            b.ToTable("OutboxMessages");
        });
    }
}
