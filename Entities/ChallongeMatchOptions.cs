using System;
using ChallongeSharp.Helpers;
using ChallongeSharp.Models.ViewModels.BaseModels;
using ChallongeSharp.Models.ViewModels.Types;

namespace ChallongeSharp.Models.ViewModels
{
    public class MatchOptions
    {
        public TournamentState State { get; set; }
        public long? ParticipantId { get; set; }
        public bool IncludeAttachments { get; set; }
        [ChallongeName("include_participants")]
        public int IncludeParticipantsInt => Convert.ToInt32(IncludeAttachments);
        public long? Player1Id { get; set; }
        public long? Player2Id { get; set; }
        public int? Player1Score { get; set; }
        public int? Player2Score { get; set; }
        [ChallongeName("match[winner_id")]
        public string WinnerId
        {
            get
            {
                if (Player1Score == Player2Score)
                    return "tie";
                return Player1Score > Player2Score ? Player1Id.ToString() : Player2Id.ToString();
            }
        }

        [ChallongeName("match[player1_votes")]
        public int? Player1Votes { get; set; }
        [ChallongeName("match[player2_votes")]
        public int? Player2Votes { get; set; }
        [ChallongeName("match[scores_csv]")]
        public string Scores => $"{Player1Score}-{Player2Score}";
    }
}
