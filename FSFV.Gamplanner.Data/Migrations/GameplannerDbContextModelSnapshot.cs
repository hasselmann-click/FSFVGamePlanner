﻿// <auto-generated />
using System;
using FSFV.Gamplanner.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FSFV.Gamplanner.Data.Migrations
{
    [DbContext(typeof(GameplannerDbContext))]
    partial class GameplannerDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.6")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("FSFV.Gamplanner.Data.Model.Competition", b =>
                {
                    b.Property<int>("CompetitionID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("MachineName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("CompetitionID");

                    b.ToTable("Competition");
                });

            modelBuilder.Entity("FSFV.Gamplanner.Data.Model.Contest", b =>
                {
                    b.Property<int>("ContestID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int?>("CompetitionID")
                        .HasColumnType("int");

                    b.Property<int?>("LeagueID")
                        .HasColumnType("int");

                    b.Property<int?>("SeasonID")
                        .HasColumnType("int");

                    b.HasKey("ContestID");

                    b.HasIndex("CompetitionID");

                    b.HasIndex("LeagueID");

                    b.HasIndex("SeasonID");

                    b.ToTable("Contest");
                });

            modelBuilder.Entity("FSFV.Gamplanner.Data.Model.Intermediary.ContestTeam", b =>
                {
                    b.Property<int>("TeamID")
                        .HasColumnType("int");

                    b.Property<int>("ContestID")
                        .HasColumnType("int");

                    b.Property<int>("Group")
                        .HasColumnType("int");

                    b.HasKey("TeamID", "ContestID");

                    b.HasIndex("ContestID");

                    b.ToTable("ContestTeam");
                });

            modelBuilder.Entity("FSFV.Gamplanner.Data.Model.League", b =>
                {
                    b.Property<int>("LeagueID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("MachineName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("LeagueID");

                    b.ToTable("League");
                });

            modelBuilder.Entity("FSFV.Gamplanner.Data.Model.Season", b =>
                {
                    b.Property<int>("SeasonID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("SeasonID");

                    b.ToTable("Season");
                });

            modelBuilder.Entity("FSFV.Gamplanner.Data.Model.Team", b =>
                {
                    b.Property<int>("TeamID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<bool>("HasZK")
                        .HasColumnType("bit");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("TeamID");

                    b.ToTable("Team");
                });

            modelBuilder.Entity("FSFV.Gamplanner.Data.Model.Contest", b =>
                {
                    b.HasOne("FSFV.Gamplanner.Data.Model.Competition", "Competition")
                        .WithMany()
                        .HasForeignKey("CompetitionID");

                    b.HasOne("FSFV.Gamplanner.Data.Model.League", "League")
                        .WithMany()
                        .HasForeignKey("LeagueID");

                    b.HasOne("FSFV.Gamplanner.Data.Model.Season", "Season")
                        .WithMany()
                        .HasForeignKey("SeasonID");
                });

            modelBuilder.Entity("FSFV.Gamplanner.Data.Model.Intermediary.ContestTeam", b =>
                {
                    b.HasOne("FSFV.Gamplanner.Data.Model.Contest", "Contest")
                        .WithMany("ContestTeams")
                        .HasForeignKey("ContestID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("FSFV.Gamplanner.Data.Model.Team", "Team")
                        .WithMany("ContestTeams")
                        .HasForeignKey("TeamID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
