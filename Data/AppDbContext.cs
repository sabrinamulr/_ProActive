// Seite/Datei: Data/AppDbContext.cs
// Seite: AppDbContext

using Microsoft.EntityFrameworkCore;
using ProActive2508.Models.Entity.Anja;
using ProActive2508.Models.Entity.Anja.Kantine;
using ProActive2508.Service;

namespace ProActive2508.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // --- DbSets (Anja) ---
        public DbSet<Benutzer> Benutzer { get; set; } = default!;
        public DbSet<Projekt> Projekte { get; set; } = default!;
        public DbSet<Aufgabe> Aufgaben { get; set; } = default!;
        //public DbSet<AbhakVormerkung> Vormerkungen { get; set; } = default!;

        // --- DbSets (Kantine) ---
        public DbSet<Allergen> Allergene { get; set; } = default!;
        public DbSet<Gericht> Gerichte { get; set; } = default!;
        public DbSet<GerichtAllergen> GerichtAllergene { get; set; } = default!;
        public DbSet<Preisverlauf> Preisverlaeufe { get; set; } = default!;
        public DbSet<MenueplanTag> MenueplanTage { get; set; } = default!;
        public DbSet<Menueplan> Menueplaene { get; set; } = default!;
        public DbSet<Vorbestellung> Vorbestellungen { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // =========================
            // Anja: BENUTZER
            // =========================
            modelBuilder.Entity<Benutzer>(b =>
            {
                b.ToTable("Benutzer");
                b.Property(x => x.Id).ValueGeneratedOnAdd();
                b.Property(x => x.Personalnummer).IsRequired();
                b.Property(x => x.Email).HasMaxLength(200).IsRequired();
                b.Property(x => x.Stufe).HasMaxLength(50);
                b.Property(x => x.Abteilung).HasMaxLength(100);
                b.Property(x => x.Verfuegbarkeit).IsRequired();
                b.Property(x => x.PasswordHash).HasMaxLength(200).IsRequired();
                b.HasIndex(x => x.Personalnummer).IsUnique();
                b.HasIndex(x => x.Email).IsUnique();
            });

            // =========================
            // Anja: PROJEKT
            // =========================
            modelBuilder.Entity<Projekt>(p =>
            {
                p.ToTable("Projekte");
                p.Property(x => x.Id).ValueGeneratedOnAdd();

                p.Property(x => x.AufgabeId).IsRequired(false);
                p.Property(x => x.ProblemId).IsRequired(false);

                p.Property(x => x.BenutzerId).IsRequired();
                p.Property(x => x.ProjektleiterId).IsRequired();
                p.Property(x => x.AuftraggeberId).IsRequired();
                p.Property(x => x.Status).HasConversion<int>().IsRequired();
                p.Property(x => x.Phase).HasConversion<int>().IsRequired();
                p.Property(x => x.Projektbeschreibung).IsRequired();

                p.HasOne(x => x.Owner).WithMany(u => u.ProjekteAlsOwner).HasForeignKey(x => x.BenutzerId).OnDelete(DeleteBehavior.Restrict);
                p.HasOne(x => x.Projektleiter).WithMany(u => u.ProjekteAlsProjektleiter).HasForeignKey(x => x.ProjektleiterId).OnDelete(DeleteBehavior.Restrict);
                p.HasOne(x => x.Auftraggeber).WithMany(u => u.ProjekteAlsAuftraggeber).HasForeignKey(x => x.AuftraggeberId).OnDelete(DeleteBehavior.Restrict);

                p.HasMany(x => x.Aufgaben).WithOne(a => a.Projekt).HasForeignKey(a => a.ProjektId).OnDelete(DeleteBehavior.Cascade);

                p.HasIndex(x => x.BenutzerId);
                p.HasIndex(x => x.ProjektleiterId);
                p.HasIndex(x => x.AuftraggeberId);
                p.HasIndex(x => new { x.Status, x.Phase });
            });

            // =========================
            // Anja: AUFGABE
            // =========================
            // Seite: AppDbContext – Fluent-Konfiguration Aufgabe

            modelBuilder.Entity<Aufgabe>(a =>
            {
                a.ToTable("Aufgaben");

                a.Property(x => x.Id).ValueGeneratedOnAdd();

                // OPTIONALER FK → darf NULL sein
                a.Property(x => x.ProjektId)
                 .IsRequired(false);

                a.Property(x => x.BenutzerId).IsRequired();

                a.Property(x => x.Aufgabenbeschreibung)
                 .HasMaxLength(500)
                 .IsRequired();

              
                a.Property(x => x.Faellig)
                 .HasColumnType("date")
                 .IsRequired();

                a.Property(x => x.Phase).IsRequired();

                a.Property(x => x.Erledigt)
                 .HasConversion<int>()
                 .IsRequired();

                // Beziehung Aufgabe→Projekt ist OPTIONAL, beim Löschen des Projekts FK auf NULL setzen
                a.HasOne(x => x.Projekt)
                 .WithMany(p => p.Aufgaben)
                 .HasForeignKey(x => x.ProjektId)
                 .IsRequired(false)
                 .OnDelete(DeleteBehavior.SetNull);

                // Benutzer ist PFLICHT; Löschen verhindern, solange Aufgaben existieren
                a.HasOne(x => x.Benutzer)
                 .WithMany(u => u.Aufgaben)
                 .HasForeignKey(x => x.BenutzerId)
                 .OnDelete(DeleteBehavior.Restrict);

                a.HasIndex(x => x.ProjektId);
                a.HasIndex(x => x.BenutzerId);
                a.HasIndex(x => new { x.Phase, x.Faellig });
            });


            // =========================
            // Kantine: ALLERGEN
            // =========================
            modelBuilder.Entity<Allergen>().HasIndex(a => a.Kuerzel).IsUnique();

            // Kantine: GERICHT
            modelBuilder.Entity<Gericht>().HasIndex(g => g.Gerichtname).IsUnique();

            // m:n Gericht <-> Allergen via Join-Entity
            modelBuilder.Entity<GerichtAllergen>().HasKey(ga => new { ga.GerichtId, ga.AllergenId });
            modelBuilder.Entity<GerichtAllergen>()
                .HasOne(ga => ga.Gericht).WithMany(g => g.GerichtAllergene)
                .HasForeignKey(ga => ga.GerichtId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<GerichtAllergen>()
                .HasOne(ga => ga.Allergen).WithMany(a => a.GerichtAllergene)
                .HasForeignKey(ga => ga.AllergenId).OnDelete(DeleteBehavior.Cascade);

            // Kantine: PREISVERLAUF (1:n Gericht -> Preisverlauf)
            modelBuilder.Entity<Preisverlauf>(e =>
            {
                e.Property(p => p.Preis).HasPrecision(18, 2);
                e.HasOne(p => p.Gericht).WithMany(g => g.Preisverlaeufe)
                    .HasForeignKey(p => p.GerichtId).OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(p => new { p.GerichtId, p.GueltigAb }).IsUnique();
            });

            // Kantine: MENUEPLANTAG (ein Kalendertag, UNIQUE, nur Datum)
            modelBuilder.Entity<MenueplanTag>(t =>
            {
                t.Property(x => x.Tag).HasColumnType("date").IsRequired();
                t.HasIndex(x => x.Tag).IsUnique();
            });

            // Kantine: MENUEPLAN (zwei Positionen je Tag)
            modelBuilder.Entity<Menueplan>(m =>
            {
                m.HasOne(x => x.MenueplanTag).WithMany(t => t.Eintraege)
                    .HasForeignKey(x => x.MenueplanTagId).OnDelete(DeleteBehavior.Cascade);
                m.HasOne(x => x.Gericht).WithMany()
                    .HasForeignKey(x => x.GerichtId).OnDelete(DeleteBehavior.Restrict);
                m.HasIndex(x => new { x.MenueplanTagId, x.PositionNr }).IsUnique();
                m.ToTable(tb => tb.HasCheckConstraint("CK_Menueplan_Position", "[PositionNr] IN (1,2)"));
            });

            // Kantine: VORBESTELLUNG (1 Bestellung/Tag/Benutzer)
            modelBuilder.Entity<Vorbestellung>(v =>
            {
                v.HasOne(x => x.Benutzer).WithMany()
                    .HasForeignKey(x => x.BenutzerId).OnDelete(DeleteBehavior.Restrict);
                v.HasOne(x => x.MenueplanTag).WithMany()
                    .HasForeignKey(x => x.MenueplanTagId).OnDelete(DeleteBehavior.Cascade);
                v.HasOne(x => x.Eintrag).WithMany()
                    .HasForeignKey(x => x.EintragId).OnDelete(DeleteBehavior.Restrict);
                v.HasIndex(x => new { x.BenutzerId, x.MenueplanTagId }).IsUnique();
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
