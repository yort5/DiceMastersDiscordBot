using System;
using Newtonsoft.Json;

namespace ChallongeSharp.Models.ChallongeModels
{
    public class MatchResponse
    {
        public Match Match { get; set; }
    }

    public class Match
    {
        [JsonProperty("attachment_count")] public int? AttachmentCount { get; set; }

        [JsonProperty("created_at")] public DateTime? CreatedAt { get; set; }

        [JsonProperty("group_id")] public long? GroupId { get; set; }

        [JsonProperty("has_attachment")] public bool? HasAttachment { get; set; }

        [JsonProperty("id")] public long? Id { get; set; }

        [JsonProperty("identifier")] public string Identifier { get; set; }

        [JsonProperty("location")] public string Location { get; set; }

        [JsonProperty("loser_id")] public long? LoserId { get; set; }

        [JsonProperty("player1_id")] public long? Player1Id { get; set; }

        [JsonProperty("player1_is_prereq_match_loser")]
        public bool? Player1IsPrereqMatchLoser { get; set; }

        [JsonProperty("player1_prereq_match_id")]
        public long? Player1PrereqMatchId { get; set; }

        [JsonProperty("player1_votes")] public int? Player1Votes { get; set; }

        [JsonProperty("player2_id")] public long? Player2Id { get; set; }

        [JsonProperty("player2_is_prereq_match_loser")]
        public bool? Player2IsPrereqMatchLoser { get; set; }

        [JsonProperty("player2_prereq_match_id")]
        public long? Player2PrereqMatchId { get; set; }

        [JsonProperty("player2_votes")] public int? Player2Votes { get; set; }

        [JsonProperty("round")] public long? Round { get; set; }

        [JsonProperty("scheduled_time")] public DateTime? ScheduledTime { get; set; }

        [JsonProperty("started_at")] public DateTime? StartedAt { get; set; }

        [JsonProperty("state")] public string State { get; set; }

        [JsonProperty("tournament_id")] public long? TournamentId { get; set; }

        [JsonProperty("underway_at")] public DateTime? UnderwayAt { get; set; }

        [JsonProperty("updated_at")] public DateTime? UpdatedAt { get; set; }

        [JsonProperty("winner_id")] public long? WinnerId { get; set; }

        [JsonProperty("prerequisite_match_ids_csv")]
        public string PrerequisiteMatchIdsCsv { get; set; }

        [JsonProperty("scores_csv")] public string ScoresCsv { get; set; }
    }
}
