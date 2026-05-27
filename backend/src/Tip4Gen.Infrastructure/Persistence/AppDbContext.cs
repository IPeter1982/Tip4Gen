using Microsoft.EntityFrameworkCore;
using Tip4Gen.Domain.Ai;
using Tip4Gen.Domain.Scoring;
using Tip4Gen.Domain.Teams;
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
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<TeamInvite> TeamInvites => Set<TeamInvite>();
    public DbSet<AiTipAttempt> AiTipAttempts => Set<AiTipAttempt>();

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
                // Exactly one of (user_id, team_member_id) is set. AI tips key on the
                // team member; human tips key on the user. Mirror constraint on scored_tips.
                t.HasCheckConstraint("ck_tips_owner_xor",
                    "(user_id IS NOT NULL AND team_member_id IS NULL) "
                    + "OR (user_id IS NULL AND team_member_id IS NOT NULL)");
                // Jokers are a human-only mechanic (guide §6); AI tips never play one.
                t.HasCheckConstraint("ck_tips_ai_no_joker",
                    "team_member_id IS NULL OR joker = FALSE");
            });
            b.HasKey(t => t.Id);
            b.Property(t => t.Id).HasColumnName("id");
            b.Property(t => t.UserId).HasColumnName("user_id");
            b.Property(t => t.TeamMemberId).HasColumnName("team_member_id");
            b.Property(t => t.MatchId).HasColumnName("match_id").IsRequired();
            b.Property(t => t.HomeGoals).HasColumnName("home_goals").IsRequired();
            b.Property(t => t.AwayGoals).HasColumnName("away_goals").IsRequired();
            b.Property(t => t.Joker).HasColumnName("joker").IsRequired();
            b.Property(t => t.IsAiFallback).HasColumnName("is_ai_fallback").HasDefaultValue(false).IsRequired();
            b.Property(t => t.Reasoning).HasColumnName("reasoning").HasMaxLength(500);
            b.Property(t => t.SubmittedAt).HasColumnName("submitted_at").IsRequired();
            b.Property(t => t.UpdatedAt).HasColumnName("updated_at").IsRequired();

            b.HasOne<User>()
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne<TeamMember>()
                .WithMany()
                .HasForeignKey(t => t.TeamMemberId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne<Match>()
                .WithMany()
                .HasForeignKey(t => t.MatchId)
                .OnDelete(DeleteBehavior.Cascade);

            // Uniqueness becomes per-owner: one human tip per (user, match), and one AI
            // tip per (team_member, match). Both are partial indexes filtering out the
            // nullable side, so the "other" owner column never participates.
            b.HasIndex(t => new { t.UserId, t.MatchId })
                .IsUnique()
                .HasDatabaseName("ux_tips_user_match")
                .HasFilter("user_id IS NOT NULL");
            b.HasIndex(t => new { t.TeamMemberId, t.MatchId })
                .IsUnique()
                .HasDatabaseName("ux_tips_member_match")
                .HasFilter("team_member_id IS NOT NULL");
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
                t.HasCheckConstraint("ck_scored_tips_owner_xor",
                    "(user_id IS NOT NULL AND team_member_id IS NULL) "
                    + "OR (user_id IS NULL AND team_member_id IS NOT NULL)");
            });
            b.HasKey(s => s.Id);
            b.Property(s => s.Id).HasColumnName("id");
            b.Property(s => s.TipId).HasColumnName("tip_id").IsRequired();
            b.Property(s => s.MatchId).HasColumnName("match_id").IsRequired();
            b.Property(s => s.UserId).HasColumnName("user_id");
            b.Property(s => s.TeamMemberId).HasColumnName("team_member_id");
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
            b.HasOne<TeamMember>()
                .WithMany()
                .HasForeignKey(s => s.TeamMemberId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(s => s.TipId).IsUnique();
            b.HasIndex(s => s.MatchId);
            b.HasIndex(s => s.UserId);
            b.HasIndex(s => s.TeamMemberId);
        });

        modelBuilder.Entity<Team>(b =>
        {
            b.ToTable("teams", t =>
            {
                t.HasCheckConstraint("ck_teams_status",
                    "status IN ('Forming','Locked','Disqualified')");
                t.HasCheckConstraint("ck_teams_ai_mode",
                    "ai_mode IS NULL OR ai_mode IN ('Conservative','Balanced','Bold')");
            });
            b.HasKey(t => t.Id);
            b.Property(t => t.Id).HasColumnName("id");
            b.Property(t => t.Name).HasColumnName("name").HasMaxLength(Team.MaxNameLength).IsRequired();
            b.Property(t => t.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(16)
                .IsRequired();
            b.Property(t => t.AiMode)
                .HasColumnName("ai_mode")
                .HasConversion<string>()
                .HasMaxLength(16);
            b.Property(t => t.CreatedAt).HasColumnName("created_at").IsRequired();
            b.Property(t => t.UpdatedAt).HasColumnName("updated_at").IsRequired();
        });

        modelBuilder.Entity<TeamMember>(b =>
        {
            b.ToTable("team_members", t =>
            {
                t.HasCheckConstraint("ck_team_members_ai_shape",
                    "(is_ai = TRUE AND user_id IS NULL AND ai_display_name IS NOT NULL) "
                    + "OR (is_ai = FALSE AND user_id IS NOT NULL AND ai_display_name IS NULL)");
            });
            b.HasKey(m => m.Id);
            b.Property(m => m.Id).HasColumnName("id");
            b.Property(m => m.TeamId).HasColumnName("team_id").IsRequired();
            b.Property(m => m.UserId).HasColumnName("user_id");
            b.Property(m => m.IsAi).HasColumnName("is_ai").IsRequired();
            b.Property(m => m.AiDisplayName).HasColumnName("ai_display_name").HasMaxLength(80);
            b.Property(m => m.JoinedAt).HasColumnName("joined_at").IsRequired();

            b.HasOne<Team>()
                .WithMany()
                .HasForeignKey(m => m.TeamId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne<User>()
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // One human user can belong to at most one team. NULLs (AI rows) are
            // allowed to repeat — PostgreSQL treats NULLs as distinct in unique indexes.
            b.HasIndex(m => m.UserId).IsUnique();
            b.HasIndex(m => m.TeamId);
            // Partial unique index: at most one AI member per team.
            b.HasIndex(m => m.TeamId)
                .IsUnique()
                .HasDatabaseName("ux_team_members_one_ai_per_team")
                .HasFilter("is_ai = TRUE");
        });

        modelBuilder.Entity<TeamInvite>(b =>
        {
            b.ToTable("team_invites");
            b.HasKey(i => i.Id);
            b.Property(i => i.Id).HasColumnName("id");
            b.Property(i => i.TeamId).HasColumnName("team_id").IsRequired();
            b.Property(i => i.Token).HasColumnName("token").HasMaxLength(64).IsRequired();
            b.Property(i => i.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();
            b.Property(i => i.CreatedAt).HasColumnName("created_at").IsRequired();
            b.Property(i => i.ExpiresAt).HasColumnName("expires_at").IsRequired();
            b.Property(i => i.UsedAt).HasColumnName("used_at");
            b.Property(i => i.UsedByUserId).HasColumnName("used_by_user_id");

            b.HasOne<Team>()
                .WithMany()
                .HasForeignKey(i => i.TeamId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne<User>()
                .WithMany()
                .HasForeignKey(i => i.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(i => i.Token).IsUnique();
            b.HasIndex(i => i.TeamId);
        });

        modelBuilder.Entity<AiTipAttempt>(b =>
        {
            b.ToTable("ai_tip_attempts");
            b.HasKey(a => a.Id);
            b.Property(a => a.Id).HasColumnName("id");
            b.Property(a => a.TeamMemberId).HasColumnName("team_member_id").IsRequired();
            b.Property(a => a.MatchId).HasColumnName("match_id").IsRequired();
            b.Property(a => a.AttemptedAt).HasColumnName("attempted_at").IsRequired();
            b.Property(a => a.Success).HasColumnName("success").IsRequired();
            b.Property(a => a.ErrorMessage).HasColumnName("error_message").HasMaxLength(500);

            b.HasOne<TeamMember>()
                .WithMany()
                .HasForeignKey(a => a.TeamMemberId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne<Match>()
                .WithMany()
                .HasForeignKey(a => a.MatchId)
                .OnDelete(DeleteBehavior.Cascade);

            // Lookup pattern: "how many attempts for (member, match) before now?" The
            // schedule policy reads this count and decides AttemptAi / WriteFallback / Skip.
            b.HasIndex(a => new { a.TeamMemberId, a.MatchId });
        });
    }
}
