using System.Text.Json.Serialization;

using CounterStrikeSharp.API.Core;

namespace GameModifiers;

public class GameModifiersConfig : BasePluginConfig
{
    [JsonPropertyName("RandomRoundsEnabledByDefault")] public bool RandomRoundsEnabledByDefault { get; set; } = false;
    [JsonPropertyName("DisableRandomRoundsInWarmup")] public bool DisableRandomRoundsInWarmup { get; set; } = false;
    [JsonPropertyName("ShowCentreMsg")] public bool ShowCentreMsg { get; set; } = true;
    [JsonPropertyName("CanRepeat")] public bool CanRepeat { get; set; } = false;
    [JsonPropertyName("MinRandomRounds")] public int MinRandomRounds { get; set; } = 1;
    [JsonPropertyName("MaxRandomRounds")] public int MaxRandomRounds { get; set; } = 1;
    [JsonPropertyName("DisabledModifiers")] public string[] DisabledModifiers { get; set; } = new string[] { };
    // Voting System Configuration
    [JsonPropertyName("EnableVotingSystem")] public bool EnableVotingSystem { get; set; } = true;
    [JsonPropertyName("VoteCommand")] public string VoteCommand { get; set; } = "rr";
    [JsonPropertyName("VotePercentageRequired")] public float VotePercentageRequired { get; set; } = 60.0f;
    [JsonPropertyName("VoteTimeoutSeconds")] public int VoteTimeoutSeconds { get; set; } = 30;
    [JsonPropertyName("MinPlayersForVote")] public int MinPlayersForVote { get; set; } = 3;
    // These control how many ROUNDS random rounds will run for after a successful vote
    [JsonPropertyName("VoteMinRounds")] public int VoteMinRounds { get; set; } = 3;
    [JsonPropertyName("VoteMaxRounds")] public int VoteMaxRounds { get; set; } = 10;
    [JsonPropertyName("CooldownBetweenVotesSeconds")] public int CooldownBetweenVotesSeconds { get; set; } = 120;
}