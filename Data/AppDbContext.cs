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
        public DbSet<ProjektBenutzer> ProjektBenutzer { get; set; } = default!;
        public DbSet<ProjektPhase> ProjektPhasen { get; set; } = default!;
        public DbSet<Phase> Phasen { get; set; } = default!;
        public DbSet<Meilenstein> Meilensteine { get; set; } = default!;
        public DbSet<PhaseMeilenstein> PhaseMeilensteine { get; set; } = default!;
        public DbSet<ProjektPhasenMA> ProjektPhasenMitarbeiter { get; set; } = default!;
        public DbSet<UmfrageKategorie> UmfrageKategorien { get; set; } = default!;
        public DbSet<Frage> Fragen { get; set; } = default!;
        public DbSet<Antwort> Antworten { get; set; } = default!;




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
            // Anja: PROJEKT_BENUTZER (m:n)
            // =========================
            modelBuilder.Entity<ProjektBenutzer>(pb =>
            {
                pb.ToTable("ProjektBenutzer");
                pb.HasKey(x => new { x.ProjektId, x.BenutzerId });
                pb.HasOne(x => x.Projekt)
                    .WithMany(p => p.ProjektBenutzer)
                    .HasForeignKey(x => x.ProjektId)
                    .OnDelete(DeleteBehavior.Cascade);
                pb.HasOne(x => x.Benutzer)
                    .WithMany(b => b.ProjektBenutzer)
                    .HasForeignKey(x => x.BenutzerId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // =========================
            // Anja: PHASE
            // =========================
            modelBuilder.Entity<Phase>(p =>
            {
                p.ToTable("Phase");
                p.Property(x => x.Id).ValueGeneratedOnAdd();
                p.Property(x => x.Bezeichnung).IsRequired();
                p.Property(x => x.Kurzbezeichnung).IsRequired();

                p.HasData(
                    new Phase { Id = 1, Bezeichnung = "Quotation", Kurzbezeichnung = "P0" },
                    new Phase { Id = 2, Bezeichnung = "Program Preparation and Kick-Off", Kurzbezeichnung = "P1" },
                    new Phase { Id = 3, Bezeichnung = "Prototype Design", Kurzbezeichnung = "P2" },
                    new Phase { Id = 4, Bezeichnung = "Production Design", Kurzbezeichnung = "P3" },
                    new Phase { Id = 5, Bezeichnung = "Off Process Samples", Kurzbezeichnung = "P4" },
                    new Phase { Id = 6, Bezeichnung = "Customer PPAP Preparation", Kurzbezeichnung = "P5" },
                    new Phase { Id = 7, Bezeichnung = "Production Launch", Kurzbezeichnung = "P6" },
                    new Phase { Id = 8, Bezeichnung = "End of Regular Production & Transition to service", Kurzbezeichnung = "P7" },
                    new Phase { Id = 9, Bezeichnung = "Product Close-Out", Kurzbezeichnung = "P8" }
                );
            });

            // =========================
            // Anja: PROJEKTPHASE
            // =========================
            modelBuilder.Entity<ProjektPhase>(pp =>
            {
                pp.ToTable("ProjektPhase");
                pp.Property(x => x.Id).ValueGeneratedOnAdd();

                pp.Property(x => x.StartDate).IsRequired();
                pp.Property(x => x.DueDate).IsRequired();
                pp.Property(x => x.Abschlussdatum).IsRequired(false);
                pp.Property(x => x.Status).IsRequired(false);
                pp.Property(x => x.Notizen).IsRequired(false);

                pp.HasOne(x => x.Projekt)
                    .WithMany(p => p.ProjektPhasen)
                    .HasForeignKey(x => x.ProjekteId)
                    .OnDelete(DeleteBehavior.Cascade);

                pp.HasOne(x => x.Phase)
                    .WithMany(p => p.ProjektPhasen)
                    .HasForeignKey(x => x.PhasenId)
                    .OnDelete(DeleteBehavior.Restrict);

                pp.HasOne(x => x.VerantwortlicherBenutzer)
                    .WithMany(b => b.ProjektPhasenAlsVerantwortlicher)
                    .HasForeignKey(x => x.VerantwortlicherbenutzerId)
                    .OnDelete(DeleteBehavior.Restrict);

                pp.HasIndex(x => x.ProjekteId);
                pp.HasIndex(x => x.PhasenId);
                pp.HasIndex(x => x.VerantwortlicherbenutzerId);
            });

            // =========================
            // Anja: MEILENSTEIN
            // =========================
            modelBuilder.Entity<Meilenstein>(m =>
            {
                m.ToTable("Meilenstein");
                m.Property(x => x.Id).ValueGeneratedOnAdd();
                m.Property(x => x.Bezeichnung).IsRequired();

                m.HasData(
                    new Meilenstein { Id = 1, Bezeichnung = "Quotation" },
                    new Meilenstein { Id = 2, Bezeichnung = "Program Kick-Off" },
                    new Meilenstein { Id = 3, Bezeichnung = "Prototype Design" },
                    new Meilenstein { Id = 4, Bezeichnung = "Production Design" },
                    new Meilenstein { Id = 5, Bezeichnung = "Off Process Tools" },
                    new Meilenstein { Id = 6, Bezeichnung = "Customer PPAP" },
                    new Meilenstein { Id = 7, Bezeichnung = "Production Launch" },
                    new Meilenstein { Id = 8, Bezeichnung = "Productio Transition" },
                    new Meilenstein { Id = 9, Bezeichnung = "End" }
                );
            });

            // =========================
            // Anja: PHASE_MEILENSTEIN
            // =========================
            modelBuilder.Entity<PhaseMeilenstein>(pm =>
            {
                pm.ToTable("PhaseMeilenstein");
                pm.Property(x => x.Id).ValueGeneratedOnAdd();
                pm.Property(x => x.Status).IsRequired(false);
                pm.Property(x => x.Erreichtdatum).IsRequired(false);

                pm.HasOne(x => x.ProjektPhase)
                    .WithMany(p => p.PhaseMeilensteine)
                    .HasForeignKey(x => x.ProjektphasenId)
                    .OnDelete(DeleteBehavior.Cascade);

                pm.HasOne(x => x.Meilenstein)
                    .WithMany(m => m.PhaseMeilensteine)
                    .HasForeignKey(x => x.MeilensteinId)
                    .OnDelete(DeleteBehavior.Cascade);

                pm.HasOne(x => x.GenehmigerBenutzer)
                    .WithMany(b => b.GenehmigtePhaseMeilensteine)
                    .HasForeignKey(x => x.GenehmigerbenutzerId)
                    .OnDelete(DeleteBehavior.Restrict);

                pm.HasIndex(x => x.ProjektphasenId);
                pm.HasIndex(x => x.MeilensteinId);
                pm.HasIndex(x => x.GenehmigerbenutzerId);
            });

            // =========================
            // Anja: PROJEKTPHASEN_MA
            // =========================
            modelBuilder.Entity<ProjektPhasenMA>(ppma =>
            {
                ppma.ToTable("ProjektPhasenMA");
                ppma.Property(x => x.Id).ValueGeneratedOnAdd();
                ppma.Property(x => x.Rolle).IsRequired(false);
                ppma.Property(x => x.Zustandigkeit).IsRequired(false);

                ppma.HasOne(x => x.Benutzer)
                    .WithMany(b => b.ProjektPhasenMitarbeiter)
                    .HasForeignKey(x => x.BenutzerId)
                    .OnDelete(DeleteBehavior.Cascade);

                ppma.HasOne(x => x.ProjektPhase)
                    .WithMany(p => p.ProjektPhasenMitarbeiter)
                    .HasForeignKey(x => x.ProjektphasenId)
                    .OnDelete(DeleteBehavior.Cascade);

                ppma.HasIndex(x => x.BenutzerId);
                ppma.HasIndex(x => x.ProjektphasenId);
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
            // Anja: UMFRAGEKATEGORIE
            // =========================
            modelBuilder.Entity<UmfrageKategorie>(k =>
            {
                k.ToTable("UmfrageKategorie");
                k.Property(x => x.Id).ValueGeneratedOnAdd();
                k.Property(x => x.Name)
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .IsRequired();
                k.ToTable(tb => tb.HasCheckConstraint("CK_UmfrageKategorie_Name_NotEmpty", "LEN([Name]) > 0"));
            });

            // =========================
            // Anja: FRAGE
            // =========================
            modelBuilder.Entity<Frage>(f =>
            {
                f.ToTable("Frage");
                f.Property(x => x.Id).ValueGeneratedOnAdd();
                f.Property(x => x.Text).IsUnicode(false);

                f.HasOne(x => x.Kategorie)
                    .WithMany(k => k.Fragen)
                    .HasForeignKey(x => x.KategorieId)
                    .OnDelete(DeleteBehavior.Cascade);

                f.HasIndex(x => x.KategorieId);
            });

            // =========================
            // Anja: ANTWORT
            // =========================
            modelBuilder.Entity<Antwort>(a =>
            {
                a.ToTable("Antwort");
                a.Property(x => x.Id).ValueGeneratedOnAdd();
                a.Property(x => x.Datum).HasColumnType("date");

                a.HasOne(x => x.Frage)
                    .WithMany(f => f.Antworten)
                    .HasForeignKey(x => x.FrageId)
                    .OnDelete(DeleteBehavior.Cascade);

                a.HasOne(x => x.Projekt)
                    .WithMany(p => p.Antworten)
                    .HasForeignKey(x => x.ProjektId)
                    .OnDelete(DeleteBehavior.Restrict);

                a.HasIndex(x => x.FrageId);
                a.HasIndex(x => x.ProjektId);
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
