using Microsoft.EntityFrameworkCore;
using Tip4Gen.Domain.Scoring;
using Tip4Gen.Domain.Tipping;
using Tip4Gen.Domain.Tournaments;
using Tip4Gen.Domain.Users;

namespace Tip4Gen.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<NationalTeam> NationalTeams => Set<NationalTeam>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<Tip> Tips => Set<Tip>();
    public DbSet<LongTermTip> LongTermTips => Set<LongTermTip>();
    public DbSet<ScoredTip> ScoredTips => Set<ScoredTip>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(b =>
        {
            b.ToTable("users");
            b.HasKey(u => u.Id);
            b.Property(u => u.Id).HasColumnName("id");
            b.Property(u => u.Auth0Sub).HasColumnName("auth0_sub").HasMaxLength(255).IsRequired();
            b.Property(u => u.DisplayName).HasColumnName("display_name").HasMaxLength(120).IsRequired();
            b.Property(u => u.CreatedAt).HasColumnName("created_at").IsRequired();
            b.HasIndex(u => u.Auth0Sub).IsUnique();
        });

        modelBuilder.Entity<Tournament>(b =>
        {
            b.ToTable("tournaments");
            b.HasKey(t => t.Id);
            b.Property(t => t.Id).HasColumnName("id");
            b.Property(t => t.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            b.Property(t => t.ExternalLeagueId).HasColumnName("external_league_id").HasMaxLength(64).IsRequired();
            b.Property(t => t.Season).HasColumnName("season").IsRequired();
            b.Property(t => t.StartsAtUtc).HasColumnName("starts_at_utc").IsRequired();
            b.Property(t => t.EndsAtUtc).HasColumnName("ends_at_utc");
            b.Property(t => t.CreatedAt).HasColumnName("created_at").IsRequired();
            b.HasIndex(t => new { t.ExternalLeagueId, t.Season }).IsUnique();
        });

        modelBuilder.Entity<NationalTeam>(b =>
        {
            b.ToTable("teams_national");
            b.HasKey(t => t.Id);
            b.Property(t => t.Id).HasColumnName("id");
            b.Property(t => t.ExternalId).HasColumnName("external_id").HasMaxLength(64).IsRequired();
            b.Property(t => t.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
            b.Property(t => t.Code).HasColumnName("code").HasMaxLength(8);
            b.Property(t => t.CreatedAt).HasColumnName("created_at").IsRequired();
            b.HasIndex(t => t.ExternalId).IsUnique();
        });

        modelBuilder.Entity<Match>(b =>
        {
            b.ToTable("matches", t =>
            {
                t.HasCheckConstraint("ck_matches_stage",
                    "stage IN ('Group','R32','R16','QF','SF','Bronze','Final')");
                t.HasCheckConstraint("ck_matches_status",
                    "status IN ('Scheduled','Live','Finished','Postponed','Cancelled','Abandoned','Awarded')");
                t.HasCheckConstraint("ck_matches_score_nullability",
                    "(home_goals IS NULL) = (away_goals IS NULL)");
                t.HasCheckConstraint("ck_matches_teams_distinct",
                    "home_team_id <> away_team_id");
            });
            b.HasKey(m => m.Id);
            b.Property(m => m.Id).HasColumnName("id");
            b.Property(m => m.TournamentId).HasColumnName("tournament_id").IsRequired();
            b.Property(m => m.ExternalId).HasColumnName("external_id").HasMaxLength(64).IsRequired();
            b.Property(m => m.Stage)
                .HasColumnName("stage")
                .HasConversion<string>()
                .HasMaxLength(16)
                .IsRequired();
            b.Property(m => m.GroupCode).HasColumnName("group_code").HasMaxLength(4);
            b.Property(m => m.RoundLabel).HasColumnName("round_label").HasMaxLength(120);
            b.Property(m => m.HomeTeamId).HasColumnName("home_team_id").IsRequired();
            b.Property(m => m.AwayTeamId).HasColumnName("away_team_id").IsRequired();
            b.Property(m => m.KickoffUtc).HasColumnName("kickoff_utc").IsRequired();
            b.Property(m => m.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(16)
                .IsRequired();
            b.Property(m => m.HomeGoals).HasColumnName("home_goals");
            b.Property(m => m.AwayGoals).HasColumnName("away_goals");
            b.Property(m => m.UpdatedAt).HasColumnName("updated_at").IsRequired();

            b.HasOne<Tournament>()
                .WithMany()
                .HasForeignKey(m => m.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne<NationalTeam>()
                .WithMany()
                .HasForeignKey(m => m.HomeTeamId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasOne<NationalTeam>()
                .WithMany()
                .HasForeignKey(m => m.AwayTeamId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(m => new { m.TournamentId, m.ExternalId }).IsUnique();
            b.HasIndex(m => m.KickoffUtc);
            b.HasIndex(m => m.Status);
        });

        modelBuilder.Entity<Tip>(b =>
        {
            b.ToTable("tips", t =>
            {
                t.HasCheckConstraint("ck_tips_home_goals_range",
                    "home_goals >= 0 AND home_goals <= 15");
                t.HasCheckConstraint("ck_tips_away_goals_range",
                    "away_goals >= 0 AND away_goals <= 15");
            });
            b.HasKey(t => t.Id);
            b.Property(t => t.Id).HasColumnName("id");
            b.Property(t => t.UserId).HasColumnName("user_id").IsRequired();
            b.Property(t => t.MatchId).HasColumnName("match_id").IsRequired();
            b.Property(t => t.HomeGoals).HasColumnName("home_goals").IsRequired();
            b.Property(t => t.AwayGoals).HasColumnName("away_goals").IsRequired();
            b.Property(t => t.Joker).HasColumnName("joker").IsRequired();
            b.Property(t => t.SubmittedAt).HasColumnName("submitted_at").IsRequired();
            b.Property(t => t.UpdatedAt).HasColumnName("updated_at").IsRequired();

            b.HasOne<User>()
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne<Match>()
                .WithMany()
                .HasForeignKey(t => t.MatchId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(t => new { t.UserId, t.MatchId }).IsUnique();
            b.HasIndex(t => t.MatchId);
            b.HasIndex(t => new { t.UserId, t.Joker })
                .HasFilter("joker = TRUE");  // partial index for the joker-count query
        });

        modelBuilder.Entity<LongTermTip>(b =>
        {
            b.ToTable("long_term_tips", t =>
            {
                t.HasCheckConstraint("ck_long_term_tips_type",
                    "type IN ('Winner','TopScorer')");
                t.HasCheckConstraint("ck_long_term_tips_target_shape",
                    "(type = 'Winner' AND target_team_id IS NOT NULL AND target_player_name IS NULL) " +
                    "OR (type = 'TopScorer' AND target_player_name IS NOT NULL AND target_team_id IS NULL)");
            });
            b.HasKey(t => t.Id);
            b.Property(t => t.Id).HasColumnName("id");
            b.Property(t => t.UserId).HasColumnName("user_id").IsRequired();
            b.Property(t => t.Type)
                .HasColumnName("type")
                .HasConversion<string>()
                .HasMaxLength(16)
                .IsRequired();
            b.Property(t => t.TargetTeamId).HasColumnName("target_team_id");
            b.Property(t => t.TargetPlayerName).HasColumnName("target_player_name").HasMaxLength(120);
            b.Property(t => t.SubmittedAt).HasColumnName("submitted_at").IsRequired();
            b.Property(t => t.UpdatedAt).HasColumnName("updated_at").IsRequired();

            b.HasOne<User>()
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne<NationalTeam>()
                .WithMany()
                .HasForeignKey(t => t.TargetTeamId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(t => new { t.UserId, t.Type }).IsUnique();
        });

        modelBuilder.Entity<ScoredTip>(b =>
        {
            b.ToTable("scored_tips", t =>
            {
                t.HasCheckConstraint("ck_scored_tips_category",
                    "category IN ('Nothing','OneTeamGoals','Winner','WinnerAndGoalDiff','Exact')");
                t.HasCheckConstraint("ck_scored_tips_multiplier_range",
                    "multiplier >= 1 AND multiplier <= 3");
                t.HasCheckConstraint("ck_scored_tips_points_non_negative",
                    "base_points >= 0 AND final_points >= 0");
            });
            b.HasKey(s => s.Id);
            b.Property(s => s.Id).HasColumnName("id");
            b.Property(s => s.TipId).HasColumnName("tip_id").IsRequired();
            b.Property(s => s.MatchId).HasColumnName("match_id").IsRequired();
            b.Property(s => s.UserId).HasColumnName("user_id").IsRequired();
            b.Property(s => s.Category)
                .HasColumnName("category")
                .HasConversion<string>()
                .HasMaxLength(24)
                .IsRequired();
            b.Property(s => s.BasePoints).HasColumnName("base_points").IsRequired();
            b.Property(s => s.Multiplier)
                .HasColumnName("multiplier")
                .HasColumnType("numeric(3,1)")
                .IsRequired();
            b.Property(s => s.JokerApplied).HasColumnName("joker_applied").IsRequired();
            b.Property(s => s.FinalPoints).HasColumnName("final_points").IsRequired();
            b.Property(s => s.ScoredAt).HasColumnName("scored_at").IsRequired();

            b.HasOne<Tip>()
                .WithMany()
                .HasForeignKey(s => s.TipId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne<Match>()
                .WithMany()
                .HasForeignKey(s => s.MatchId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne<User>()
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(s => s.TipId).IsUnique();
            b.HasIndex(s => s.MatchId);
            b.HasIndex(s => s.UserId);
        });
    }
}
