// Imports
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Timers;

using GameModifiers.Modifiers;

// Namespace
namespace GameModifiers;

// Vote session class to manage voting state
public class VoteSession
{
    public bool IsActive { get; set; } = false;
    public DateTime StartTime { get; set; }
    public List<CCSPlayerController> VotedPlayers { get; set; } = new();
    public List<CCSPlayerController> YesVotes { get; set; } = new();
    public List<CCSPlayerController> NoVotes { get; set; } = new();
    public int RequestedRounds { get; set; } = 0;
    public CCSPlayerController? Initiator { get; set; }
    public Timer? TimeoutTimer { get; set; }
    
    public void Reset()
    {
        IsActive = false;
        VotedPlayers.Clear();
        YesVotes.Clear();
        NoVotes.Clear();
        RequestedRounds = 0;
        Initiator = null;
        TimeoutTimer?.Kill();
        TimeoutTimer = null;
    }
}

public class GameModifiersCore : BasePlugin, IPluginConfig<GameModifiersConfig>
{
    // General plugin information
    public override string ModuleName => "Game Modifiers - Reloaded";
    public override string ModuleAuthor => "1pseftis | Original by Shr00mDev";
    public override string ModuleDescription => "Apply game modifiers dynamically based of pre-defined classes or config files.";
    public override string ModuleVersion => "1.0.1";
    
    // Configuration
    public GameModifiersConfig Config { get; set; } = new();
    public bool RandomRoundsEnabled { get; private set; } = false;
    private List<GameModifierBase> RegisteredModifiers { get; } = new();
    private List<GameModifierBase> ActiveModifiers { get; } = new();
    private List<GameModifierBase> LastActiveModifiers { get; set; } = new();

    private int _minRandomRounds = 1;
    private int _maxRandomRounds = 1;

    // Allow Random instance injection for testability
    public Random RandomGenerator { get; set; } = new Random();
    /// <summary>
    /// Computes the number of rounds for a vote, using the configured min/max bounds.
    /// </summary>
    public int GetRandomRounds()
    {
        return RandomGenerator.Next(Config.VoteMinRounds, Config.VoteMaxRounds + 1);
    }
    
    // Voting system variables
    private int _voteEnabledRoundsRemaining = 0;
    private bool _voteEnabledRandomRounds = false;
    private VoteSession _currentVote = new();
    private DateTime _lastVoteEndTime = DateTime.MinValue;
    
    // Configuration parsed event
    public void OnConfigParsed(GameModifiersConfig config)
    {
        Config = config;

        // Set random modifiers rounds min/max from config
        _minRandomRounds = config.MinRandomRounds;
        _maxRandomRounds = config.MaxRandomRounds;

        // Validate vote configuration
        if (config.VotePercentageRequired < 10 || config.VotePercentageRequired > 100)
        {
            GameModifiersUtils.PrintToConsole($"[GameModifiers] Invalid VotePercentageRequired: {config.VotePercentageRequired}. Must be between 10 and 100. Using minimum value: 10");
            config.VotePercentageRequired = 10;
        }

        if (config.VoteMinRounds > config.VoteMaxRounds)
        {
            Console.WriteLine("[GameModifiers::OnConfigParsed] WARNING: VoteMinRounds cannot be greater than VoteMaxRounds, using default values.");
            Config.VoteMinRounds = 3;
            Config.VoteMaxRounds = 10;
        }
    }

    public override void Load(bool hotReload)
    {
        base.Load(hotReload);

        Initialise();

        Console.WriteLine("[GameModifiers::Load] Successfully loaded!");
    }

    public override void Unload(bool hotReload)
    {
        RemoveAllModifiers();

        foreach (GameModifierBase? modifier in RegisteredModifiers)
        {
            modifier.Unregistered(this);
        }

        LastActiveModifiers.Clear();
        RegisteredModifiers.Clear();

        base.Unload(hotReload);
    }

    private void Initialise()
    {
        InitialiseModifiers();
        InitialiseCvarModifiers();

        // Verify modifier names as they are treated as keys in some commands and should be unique!
        List<string> registeredModifierNames = new List<string>();
        foreach (GameModifierBase? modifier in RegisteredModifiers)
        {
            modifier.Registered(this);

            if (registeredModifierNames.Contains(modifier.Name))
            {
                Console.WriteLine($"[GameModifiers::Initialise] WARNING: Found duplicate modifier name entry for {modifier.Name} all modifier names should be unique!");
                continue;
            }

            registeredModifierNames.Add(modifier.Name);
        }
        
        if (Config.RandomRoundsEnabledByDefault)
        {
            if (RegisteredModifiers.Count <= 0)
            {
                Console.WriteLine("[GameModifiers::Initialise] WARNING: No modifiers are registered! Cannot activate random rounds by default!");
            }
            else
            {
                RandomRoundsEnabled = true;
            }
        }
    }

    private void InitialiseModifiers()
    {
        RegisteredModifiers.Clear();

        List<Type> modifierTypes = GameModifiersUtils.GetAllChildClasses<GameModifierBase>();
        if (!modifierTypes.Any())
        {
            Console.WriteLine("[GameModifiers::InitialiseModifiers] No implemented modifiers found!");
            return;
        }

        foreach (Type modifierType in modifierTypes)
        {
            GameModifierBase modifierInstance = (GameModifierBase)Activator.CreateInstance(modifierType)!;
            if (modifierInstance.IsRegistered && !Config.DisabledModifiers.Any(x => x.Equals(modifierInstance.Name, StringComparison.OrdinalIgnoreCase)))
            {
                RegisteredModifiers.Add(modifierInstance);
                Console.WriteLine($"[GameModifiersCore::InitialiseModifiers] Registered modifier: {modifierInstance.Name}");
            }
            else
            {
                Console.WriteLine($"[GameModifiersCore::InitialiseModifiers] Disabled modifier: {modifierInstance.Name}");
            }
        }
    }

    private void InitialiseCvarModifiers()
    {
        var configPath = Path.Combine(GameModifiersUtils.GetConfigPath(ModulePath), "ConVarModifiers");
        var pluginPath = Path.Combine(GameModifiersUtils.GetPluginPath(ModulePath), "ConVarModifiers");

        ParseFileForCvarModifiers(configPath);
        ParseFileForCvarModifiers(pluginPath);
    }

    public void ParseFileForCvarModifiers(string configFolderPath)
    {
        if (Directory.Exists(configFolderPath) == false)
        {
            Directory.CreateDirectory(configFolderPath);
            return;
        }

        var configFiles = Directory.GetFiles(configFolderPath, "*.cfg");

        foreach (var configFile in configFiles)
        {
            GameModifierCvar cvarModifierInstance = new GameModifierCvar();

            if (cvarModifierInstance.ParseConfigFile(configFile))
            {
                if (!Config.DisabledModifiers.Contains(cvarModifierInstance.Name))
                {
                    RegisteredModifiers.Add(cvarModifierInstance);
                    Console.WriteLine($"[GameModifiersCore::InitialiseCvarModifiers] Registered modifier config: {configFile}");
                }
                else
                {
                    Console.WriteLine($"[GameModifiersCore::InitialiseCvarModifiers] Disabled modifier config: {configFile}");
                }
            }
            else
            {
                Console.WriteLine($"[GameModifiersCore::InitialiseCvarModifiers] Failed to load config: {configFile}");
            }
        }
    }

    // !ReloadModifiers - Re-initialises all modifiers.
    [ConsoleCommand("css_reloadmodifiers", "Re-initialises all registered modifiers. (This will remove all active modifiers too)")]
    [RequiresPermissions("@css/root")]
    public void OnReloadModifiers(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null)
        {
            return;
        }

        GameModifiersUtils.PrintTitleToChat(player, "Reloading Modifiers...");

        RemoveAllModifiers();
        Initialise();
    }

    // !ListModifiers - List registered modifiers and descriptions.
    [ConsoleCommand("css_listmodifiers", "Prints the name and description for each registered modifier.")]
    public void OnListModifiers(CCSPlayerController? player, CommandInfo info)
    {
        GameModifiersUtils.PrintModifiersToChat(player, RegisteredModifiers, "Registered modifiers");
    }

    // !ListActiveModifiers - List active modifiers and descriptions.
    [ConsoleCommand("css_listactivemodifiers", "Prints the name and description for each active modifier.")]
    public void OnListActiveModifiers(CCSPlayerController? player, CommandInfo info)
    {
        GameModifiersUtils.PrintModifiersToChat(player, ActiveModifiers, "Active modifiers");
    }

    // !AddModifier - Add a modifier by name.
    [ConsoleCommand("css_addmodifier", "Add a modifier that will persist until the end of the game. \u2029" + "(If random rounds are enabled it will only act as a re-roll for the current round)")]
    [CommandHelper(1, "<modifier name>")]
    [RequiresPermissions("@css/root")]
    public void OnAddModifier(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null)
        {
            return;
        }

        var modifierName = info.GetArg(1);
        if (AddModifierByName(modifierName, out string addMessage))
        {
            GameModifiersUtils.PrintTitleToChat(player, $"Added {modifierName} modifier.");
        }
        else
        {
            GameModifiersUtils.PrintTitleToChat(player, addMessage);
        }
    }

    // !ToggleModifier - Enabled/Disables a given modifier by name.
    [ConsoleCommand("css_togglemodifier", "Enabled/Disables a given modifier by name. \u2029" + "(If random rounds are enabled it will only act as a re-roll for the current round)")]
    [CommandHelper(1, "<modifier name>")]
    [RequiresPermissions("@css/root")]
    public void OnToggleModifier(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null)
        {
            return;
        }

        var modifierName = info.GetArg(1);
        bool modifierWasActive = IsModifierActiveByName(modifierName);

        if (ToggleModifierByName(modifierName, out string toggleMessage))
        {
             string modifierAction = modifierWasActive ? "Removed" : "Added";
             GameModifiersUtils.PrintTitleToChat(player, $"{modifierAction} {modifierName} modifier.");
        }
        else
        {
            GameModifiersUtils.PrintTitleToChat(player, toggleMessage);
        }
    }

    // !AddRandomModifier - Add a random modifier.
    [ConsoleCommand("css_addrandommodifier", "Add a random modifier to be activated immediately.")]
    [RequiresPermissions("@css/root")]
    public void OnAddRandomModifier(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null)
        {
            return;
        }

        if (!AddRandomModifier(out GameModifierBase? addedModifier))
        {
            GameModifiersUtils.PrintTitleToChat(player, "Failed to add random modifier.");
        }
    }

    // !AddRandomModifiers - Add a random number of modifiers.
    [ConsoleCommand("css_addrandommodifiers", "Add a random number of modifiers to be activated immediately.")]
    [CommandHelper(1, "<modifier count>")]
    [RequiresPermissions("@css/root")]
    public void OnAddRandomModifiers(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null)
        {
            return;
        }

        if (int.TryParse(info.GetArg(1), out int modifierCount))
        {
            if (AddRandomModifiers(modifierCount, out List<GameModifierBase> addedModifiers))
            {
                if (modifierCount == addedModifiers.Count)
                {
                    GameModifiersUtils.PrintTitleToChat(player, $"Adding {modifierCount.ToString()} random modifiers.");
                }
                else
                {
                    GameModifiersUtils.PrintTitleToChat(player, $"Only added {addedModifiers.Count.ToString()} random modifiers.");
                }

                return;
            }
        }

        GameModifiersUtils.PrintTitleToChat(player, "Failed to add random modifiers.");
    }

    // !RemoveModifier - Remove a modifier by name.
    [ConsoleCommand("css_removemodifier", "Remove an active modifier.")]
    [CommandHelper(1, "<modifier name>")]
    [RequiresPermissions("@css/root")]
    public void OnRemoveModifier(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null)
        {
            return;
        }

        var modifierName = info.GetArg(1);
        RemoveModifierByName(modifierName, out string removeMessage);
        GameModifiersUtils.PrintTitleToChat(player, removeMessage);
    }

    // !RemoveModifiers - Clear / Remove all active modifiers.
    [ConsoleCommand("css_removemodifiers", "Clear / Remove all active random round modifiers.")]
    [RequiresPermissions("@css/root")]
    public void OnRemoveModifiers(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null)
        {
            return;
        }

        RemoveAllModifiers();

        GameModifiersUtils.PrintTitleToChat(player, "Removed all modifiers.");
    }

    // !RandomRounds - Toggle random rounds on/off.
    [ConsoleCommand("css_randomrounds", "Toggle random rounds on/off. This will add a random set of modifiers at the start of each round that persist till the end of the round.")]
    [RequiresPermissions("@css/root")]
    public void OnRandomRounds(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null)
        {
            return;
        }

        if (RandomRoundsEnabled == false && RegisteredModifiers.Count <= 0)
        {
            GameModifiersUtils.PrintTitleToChat(player, "No modifiers are registered! Cannot activate random rounds!");
            return;
        }

        ToggleRandomRounds();
    }

    // Vote for random rounds
    [ConsoleCommand("css_rr", "Vote for random rounds")]
    [ConsoleCommand("css_randomroundsvote", "Vote for random rounds")]
    public void OnRandomRoundsVote(CCSPlayerController? player, CommandInfo info)
    {
        if (!Config.EnableVotingSystem)
        {
            GameModifiersUtils.PrintTitleToChat(player, "Voting system is disabled.");
            return;
        }

        if (player == null) return;

        StartRandomRoundsVote(player);
    }

    // Vote yes
    [ConsoleCommand("css_yes", "Vote yes in current vote")]
    [ConsoleCommand("css_y", "Vote yes in current vote")]
    public void OnVoteYes(CCSPlayerController? player, CommandInfo info)
    {
        if (!Config.EnableVotingSystem || player == null) return;
        CastVote(player, true);
    }

    // Vote no
    [ConsoleCommand("css_no", "Vote no in current vote")]
    [ConsoleCommand("css_n", "Vote no in current vote")]
    public void OnVoteNo(CCSPlayerController? player, CommandInfo info)
    {
        if (!Config.EnableVotingSystem || player == null) return;
        CastVote(player, false);
    }

    // !MinRandomRounds - Set the min number of random round modifiers to be active each round.
    [ConsoleCommand("css_minrandomrounds", "Set the min number of random round modifiers to be active each round.")]
    [CommandHelper(1, "<min number>")]
    [RequiresPermissions("@css/root")]
    public void OnMinRandomRounds(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null)
        {
            return;
        }

        var minInput = info.GetArg(1);
        if (int.TryParse(minInput, out int result))
        {
            GameModifiersUtils.PrintTitleToChat(player, $"Min modifiers for random rounds set to {minInput}");
            _minRandomRounds = result;
        }
        else
        {
            GameModifiersUtils.PrintTitleToChat(player, $"Failed to set min modifiers for random rounds to {minInput}");
        }
    }

    // !MaxRandomRounds - Set the min number of random round modifiers to be active each round.
    [ConsoleCommand("css_maxrandomrounds", "Set the max number of random round modifiers to be active each round.")]
    [CommandHelper(1, "<max number>")]
    [RequiresPermissions("@css/root")]
    public void OnMaxRandomRounds(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null)
        {
            return;
        }

        var maxInput = info.GetArg(1);
        if (int.TryParse(maxInput, out int result))
        {
            GameModifiersUtils.PrintTitleToChat(player, $"Max modifiers for random rounds set to {maxInput}");
            _maxRandomRounds = result;
        }
        else
        {
            GameModifiersUtils.PrintTitleToChat(player, $"Failed to set max modifiers for random rounds to {maxInput}");
        }
    }

    // !RandomRoundsReRoll - Re-roll the current random round modifiers.
    [ConsoleCommand("css_randomroundsreroll", "Re-roll the current random round modifiers and apply them to the current round.")]
    [RequiresPermissions("@css/root")]
    public void OnRandomRoundsReRoll(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null)
        {
            return;
        }

        if (RandomRoundsEnabled == false)
        {
            GameModifiersUtils.PrintTitleToChat(player, "Random rounds are not enabled! Cannot re-roll modifiers.");
            return;
        }

        if (!RegisteredModifiers.Any())
        {
            GameModifiersUtils.PrintTitleToChat(player, "No registered modifiers found! Cannot re-roll modifiers.");
            return;
        }

        RemoveAllModifiers();
        ApplyRandomRoundsForRound();
    }

    // !bhop - Enable/Disable the bhop modifier for all players.
    [ConsoleCommand("css_bhop", "Enable/Disable the bhop modifier")]
    [RequiresPermissions("@css/root")]
    public void OnBunnyHop(CCSPlayerController? player, CommandInfo info)
    {
        if (player != null)
        {
            ToggleModifierByNameCommand(player, "Bhop");
        }
    }

    // !surf - Enable/Disable the surf modifier for all players.
    [ConsoleCommand("css_surf", "Enable/Disable the surf modifier")]
    [RequiresPermissions("@css/root")]
    public void OnSurf(CCSPlayerController? player, CommandInfo info)
    {
        if (player != null)
        {
            ToggleModifierByNameCommand(player, "Surf");
        }
    }

    // !xray - Enable/Disable the xray modifier for all players.
    [ConsoleCommand("css_xray", "Enable/Disable the xray modifier for all players")]
    [RequiresPermissions("@css/root")]
    public void OnXray(CCSPlayerController? player, CommandInfo info)
    {
        if (player != null)
        {
            ToggleModifierCommand(player, typeof(GameModifierXrayAll));
        }
    }

    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        if (RandomRoundsEnabled)
        {
            if (!RegisteredModifiers.Any())
            {
                GameModifiersUtils.PrintTitleToChatAll("No registered modifiers found! Skipping random round...");
                return HookResult.Continue;
            }
            
            RemoveAllModifiers();
            
            if (Config.DisableRandomRoundsInWarmup && GameModifiersUtils.IsWarmupActive())
            {
                GameModifiersUtils.PrintTitleToChatAll("Random rounds will start after warmup period...");
            }
            else
            {
                ApplyRandomRoundsForRound();
            }
        }
        else
        {
            // Re-apply all active modifiers on round start to ensure
            // everything is correctly set-up that could have been reset since last round.
            foreach (GameModifierBase? modifier in ActiveModifiers)
            {
                modifier.Disabled();
                modifier.Enabled();
            }
        }

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        if (RandomRoundsEnabled)
        {
            RemoveAllModifiers();
            
            // Check if this was a vote-enabled session with limited rounds
            if (_voteEnabledRandomRounds && _voteEnabledRoundsRemaining > 0)
            {
                _voteEnabledRoundsRemaining--;
                
                if (_voteEnabledRoundsRemaining <= 0)
                {
                    // Disable random rounds after specified rounds
                    _voteEnabledRandomRounds = false;
                    ToggleRandomRounds(); // This will disable random rounds
                    GameModifiersUtils.PrintTitleToChatAll("Vote-enabled random rounds session ended!");
                }
                else
                {
                    GameModifiersUtils.PrintTitleToChatAll($"Random rounds: {_voteEnabledRoundsRemaining} rounds remaining");
                }
            }
        }

        return HookResult.Continue;
    }

    public GameModifierBase? GetRegisteredModifierByName(string modifierName)
    {
        return RegisteredModifiers.FirstOrDefault(modifier => string.Equals(modifier.Name, modifierName, StringComparison.OrdinalIgnoreCase));
    }

    public GameModifierBase? GetActiveModifierByName(string modifierName)
    {
        return ActiveModifiers.FirstOrDefault(modifier => string.Equals(modifier.Name, modifierName, StringComparison.OrdinalIgnoreCase));
    }

    public bool AnyModifiersActive()
    {
        return ActiveModifiers.Any();
    }

    public bool IsModifierActive(GameModifierBase? modifier)
    {
        if (modifier != null)
        {
            return ActiveModifiers.Contains(modifier);
        }

        return false;
    }

    public bool IsModifierActiveByName(string modifierName)
    {
        GameModifierBase? activeModifier = GetActiveModifierByName(modifierName);
        if (activeModifier != null)
        {
            return true;
        }

        return false;
    }

    public bool IsModifierRegistered(GameModifierBase? modifier)
    {
        if (modifier != null)
        {
            return RegisteredModifiers.Contains(modifier);
        }

        return false;
    }

    public bool IsModifierRegisteredByName(string modifierName)
    {
        GameModifierBase? registeredModifier = GetRegisteredModifierByName(modifierName);
        if (registeredModifier != null)
        {
            return true;
        }

        return false;
    }

    public void ToggleRandomRounds()
    {
        RandomRoundsEnabled = !RandomRoundsEnabled;
        if (RandomRoundsEnabled == false)
        {
            RemoveAllModifiers();
        }
        
        GameModifiersUtils.PrintTitleToChatAll(RandomRoundsEnabled ? "Random rounds enabled for next round!" : "Random rounds disabled!");
        GameModifiersUtils.ShowMessageCentreAll($"Random Rounds {(RandomRoundsEnabled ? "Enabled" : "Disabled")}");
    }

    public void ApplyRandomRoundsForRound()
    {
        Random random = new Random();
        int randomModifiersCount = random.Next(_minRandomRounds, _maxRandomRounds);

        if (AddRandomModifiers(randomModifiersCount, out List<GameModifierBase> addedModifiers))
        {
            LastActiveModifiers = ActiveModifiers.ToList();
        }
        else
        {
            GameModifiersUtils.PrintTitleToChatAll($"Failed to apply random modifiers! Skipping random round...");
        }
    }

    public bool ToggleModifier(GameModifierBase? modifier, out string message)
    {
        if (modifier == null)
        {
            Console.WriteLine("[GameModifiers::ToggleModifier] WARNING: Trying to toggle null modifier!");
            message = "Modifier is null?";
            return false;
        }

        if (IsModifierActive(modifier))
        {
            return RemoveModifier(modifier, out message);
        }

        return AddModifier(modifier, out message);
    }

    public bool ToggleModifierByName(string modifierName, out string message)
    {
        if (IsModifierRegisteredByName(modifierName) == false)
        {
            Console.WriteLine($"[GameModifiers::ToggleModifierByName] Trying to toggle un-registered modifier {modifierName}!");
            message = $"{modifierName} modifier is not registered.";
            return false;
        }

        if (IsModifierActiveByName(modifierName))
        {
            RemoveModifierByName(modifierName, out message);
            return true;
        }

        return AddModifierByName(modifierName, out message);
    }

    private void ToggleModifierCommand(CCSPlayerController? player, Type modifierType)
    {
        if (player == null)
        {
            return;
        }

        GameModifierBase? modifierInstance = RegisteredModifiers.FirstOrDefault(modifier => modifier.GetType() == modifierType);
        if (modifierInstance == null)
        {
            Console.WriteLine($"[GameModifiers::ToggleModifierCommand] Trying to toggle un-registered modifier {modifierType.Name}!");
            return;
        }

        bool modifierWasActive = IsModifierActive(modifierInstance);

        if (ToggleModifier(modifierInstance, out string toggleMessage))
        {
            GameModifiersUtils.PrintTitleToChat(player, $"{modifierInstance.Name} {(modifierWasActive ? "Disabled" : "Enabled")}.");
        }
        else
        {
            GameModifiersUtils.PrintTitleToChat(player, toggleMessage);
        }
    }

    private void ToggleModifierByNameCommand(CCSPlayerController? player, string modifierName)
    {
        bool modifierWasActive = IsModifierActiveByName(modifierName);

        if (ToggleModifierByName(modifierName, out string toggleMessage))
        {
            GameModifiersUtils.PrintTitleToChat(player, $"{modifierName} {(modifierWasActive ? "Disabled" : "Enabled")}.");
        }
        else
        {
            GameModifiersUtils.PrintTitleToChat(player, toggleMessage);
        }
    }

    public bool AddModifierByName(string modifierName, out string message)
    {
        if (!RegisteredModifiers.Any())
        {
            message = "No modifiers are registered.";
            return false;
        }

        GameModifierBase? registeredModifier = GetRegisteredModifierByName(modifierName);
        if (registeredModifier != null)
        {
            return AddModifier(registeredModifier, out message);
        }

        message = $"{modifierName} modifier is not registered!";
        return false;
    }

    public bool AddModifier(GameModifierBase? modifier, out string message)
    {
        if (modifier == null)
        {
            Console.WriteLine("[GameModifiers::AddModifier] WARNING: Trying to add null modifier!");
            message = "Modifier is null?";
            return false;
        }

        List<string> blockingModifierNames = new List<string>();
        foreach (GameModifierBase activeModifier in ActiveModifiers)
        {
            if (activeModifier.CheckIfIncompatible(modifier) || modifier.CheckIfIncompatible(activeModifier))
            {
                blockingModifierNames.Add(activeModifier.Name);
            }
        }

        if (blockingModifierNames.Any())
        {
            message = $"{modifier.Name} modifier is blocked by:";
            foreach (var blockingModifierName in blockingModifierNames)
            {
                message += $"\u2029• {blockingModifierName}";
            }

            return false;
        }

        if (ActiveModifiers.Contains(modifier))
        {
            message = $"{modifier.Name} modifier is already active.";
            return false;
        }

        ActivateModifier(modifier);
        message = $"Successfully added {modifier.Name} modifier.";
        return true;
    }

    public void RemoveModifierByName(string modifierName, out string message)
    {
        if (!ActiveModifiers.Any())
        {
            message = "No modifiers are active.";
            return;
        }
        
        foreach (GameModifierBase? modifier in ActiveModifiers)
        {
            if (string.Equals(modifier.Name, modifierName, StringComparison.OrdinalIgnoreCase))
            {
                RemoveModifier(modifier, out message);
                return;
            }
        }

        message = $"{modifierName} modifier is not active.";
    }

    public bool RemoveModifier(GameModifierBase? modifier, out string message)
    {
        if (modifier == null)
        {
            Console.WriteLine("[GameModifiers::RemoveModifier] WARNING: Trying to remove null modifier!");
            message = "Modifier is null?";
            return false;
        }

        if (!ActiveModifiers.Contains(modifier))
        {
            message = $"{modifier.Name} modifier is not active.";
            return true;
        }

        modifier.Disabled();
        ActiveModifiers.Remove(modifier);
        message = $"Removed {modifier.Name} modifier.";
        return true;
    }

    public void RemoveAllModifiers()
    {
        if (!ActiveModifiers.Any())
        {
            return;
        }
        
        GameModifiersUtils.PrintTitleToChatAll("Removing modifiers:");

        // Undo modifiers in the order they were applied.
        for (var index = ActiveModifiers.Count - 1; index >= 0; index--)
        {
            var modifier = ActiveModifiers[index];
            modifier.Disabled();
            
            GameModifiersUtils.PrintToChatAll($"• {modifier.Name}");
        }

        ActiveModifiers.Clear();
    }

    public bool AddRandomModifier(out GameModifierBase? addedModifier)
    {
        if (AddRandomModifiers(1, out List<GameModifierBase> addedModifiers))
        {
            addedModifier = addedModifiers[0];
            return true;
        }

        addedModifier = null;
        return false;
    }

    public bool AddRandomModifiers(int modifierCount, out List<GameModifierBase> addedModifiers)
    {
        addedModifiers = new List<GameModifierBase>();

        if (modifierCount <= 0)
        {
            return true;
        }
        
        if (!RegisteredModifiers.Any())
        {
            Console.WriteLine("[GameModifiers::AddRandomModifiers] No registered modifiers available!");
            return false;
        }

        // Filter out modifiers not supporting random rounds and those currently active.
        List<GameModifierBase> randomModifiersPool = RegisteredModifiers
            .Where(modifier => modifier.SupportsRandomRounds && !ActiveModifiers.Contains(modifier) && (Config.CanRepeat || !LastActiveModifiers.Contains(modifier)))
            .ToList();

        // Randomly remove modifiers that are incompatible within the randomModifiersPool.
        List<GameModifierBase> possibleModifiersPool = randomModifiersPool.ToList();
        Random random = new Random();

        for (int a = 0; a < randomModifiersPool.Count; a++)
        {
            for (int b = a + 1; b < randomModifiersPool.Count; b++)
            {
                if (randomModifiersPool[a].CheckIfIncompatible(randomModifiersPool[b]) ||
                    randomModifiersPool[b].CheckIfIncompatible(randomModifiersPool[a]))
                {
                    possibleModifiersPool.Remove(random.Next(0, 2) == 0 ? randomModifiersPool[a] : randomModifiersPool[b]);
                }
            }
        }

        if (!possibleModifiersPool.Any())
        {
            Console.WriteLine("[GameModifiers::AddRandomModifiers] Modifier pool is empty!");
            return false;
        }

        // Adjust modifierCount if not enough candidates.
        if (modifierCount > possibleModifiersPool.Count)
        {
            Console.WriteLine($"[GameModifiers::AddRandomModifiers] Not enough modifiers in possible modifiers pool, reduced random modifier count from {modifierCount} to {possibleModifiersPool.Count}!");
            modifierCount = possibleModifiersPool.Count;
        }

        // Generate a list of random modifiers from possibleModifiersPool.
        for (int i = 0; i < modifierCount; i++)
        {
            int randomIndex = random.Next(possibleModifiersPool.Count);
            addedModifiers.Add(possibleModifiersPool[randomIndex]);
            possibleModifiersPool.RemoveAt(randomIndex);
        }

        if (!addedModifiers.Any())
        {
            return false;
        }

        ActivateModifiers(addedModifiers);
        return true;
    }

    private void ActivateModifier(GameModifierBase? modifier)
    {
        if (modifier == null)
        {
            Console.WriteLine("[RoundModifiers::ActivateModifier] WARNING: Attempting to activate a null modifier!");
            return;
        }

        ActivateModifiers([modifier]);
    }

    private void ActivateModifiers(List<GameModifierBase> modifiers)
    {
        if (!modifiers.Any())
        {
            return;
        }

        if (Config.ShowCentreMsg)
        {
            string centreActivationMsg = $"Activating Modifiers:\u2029";

            for (var index = 0; index < modifiers.Count; index++)
            {
                if (index != 0)
                {
                    centreActivationMsg += ", ";
                }

                centreActivationMsg += $"{modifiers[index].Name}";
            }

            GameModifiersUtils.ShowMessageCentreAll(centreActivationMsg);
        }

        GameModifiersUtils.PrintTitleToChatAll("Activating modifiers:");
        foreach (var modifier in modifiers)
        {
            GameModifiersUtils.PrintToChatAll($"• {modifier.Name} - {ChatColors.Grey}[{modifier.Description}]");
        }

        foreach (GameModifierBase? modifier in modifiers)
        {
            modifier.Enabled();
            ActiveModifiers.Add(modifier);
        }
    }

    private void StartRandomRoundsVote(CCSPlayerController initiator)
    {
        var eligiblePlayers = GetEligibleVotingPlayers();
        
        if (eligiblePlayers.Count < Config.MinPlayersForVote)
        {
            GameModifiersUtils.PrintTitleToChat(initiator, $"Not enough players to start a vote. Minimum: {Config.MinPlayersForVote}");
            return;
        }

        if (_currentVote.IsActive)
        {
            GameModifiersUtils.PrintTitleToChat(initiator, "A vote is already in progress.");
            return;
        }

        if (RandomRoundsEnabled)
        {
            GameModifiersUtils.PrintTitleToChat(initiator, "Random rounds are already active.");
            return;
        }

        // Check cooldown
        if (_lastVoteEndTime != DateTime.MinValue && 
            (DateTime.Now - _lastVoteEndTime).TotalSeconds < Config.CooldownBetweenVotesSeconds)
        {
            var remainingCooldown = Config.CooldownBetweenVotesSeconds - (DateTime.Now - _lastVoteEndTime).TotalSeconds;
            GameModifiersUtils.PrintTitleToChat(initiator, $"Vote cooldown active. Wait {remainingCooldown:F0} more seconds.");
            return;
        }

        if (RegisteredModifiers.Count <= 0)
        {
            GameModifiersUtils.PrintTitleToChat(initiator, "No modifiers are registered! Cannot start random rounds vote.");
            return;
        }

        // Generate random round count using dedicated method
        int randomRounds = GetRandomRounds();

        // Start the vote
        _currentVote.IsActive = true;
        _currentVote.StartTime = DateTime.Now;
        _currentVote.Initiator = initiator;
        _currentVote.RequestedRounds = randomRounds;

        // Timer fix: capture vote instance
        var voteSession = _currentVote;
        _currentVote.TimeoutTimer = AddTimer(Config.VoteTimeoutSeconds, () => {
            // Only end the vote if it's still active and matches the captured vote
            if (voteSession.IsActive && ReferenceEquals(_currentVote, voteSession))
            {
                EndVote(false, "Vote timed out.");
            }
        });

        // Announce the vote
        string voteMessage = $"{initiator.PlayerName} started a vote for random rounds ({randomRounds} rounds)!";

        GameModifiersUtils.PrintTitleToChatAll(voteMessage);
        GameModifiersUtils.PrintToChatAll($"Type !yes or !y to vote YES, !no or !n to vote NO");
        GameModifiersUtils.PrintToChatAll($"Vote ends in {Config.VoteTimeoutSeconds} seconds");

        int requiredVotes = (int)Math.Ceiling(eligiblePlayers.Count * Config.VotePercentageRequired / 100.0f);
        GameModifiersUtils.PrintToChatAll($"Requires {requiredVotes}/{eligiblePlayers.Count} players to vote YES");

        GameModifiersUtils.ShowMessageCentreAll($"Vote: Random Rounds ({randomRounds} rounds)\u2029Type !yes or !no\u2029{Config.VoteTimeoutSeconds}s remaining");
    }

    private void CastVote(CCSPlayerController player, bool voteYes)
    {
        if (!_currentVote.IsActive)
        {
            GameModifiersUtils.PrintTitleToChat(player, "No active vote.");
            return;
        }

        var eligiblePlayers = GetEligibleVotingPlayers();
        if (!eligiblePlayers.Contains(player))
        {
            GameModifiersUtils.PrintTitleToChat(player, "You are not eligible to vote.");
            return;
        }

        if (_currentVote.VotedPlayers.Contains(player))
        {
            GameModifiersUtils.PrintTitleToChat(player, "You have already voted.");
            return;
        }

        _currentVote.VotedPlayers.Add(player);
        if (voteYes)
        {
            _currentVote.YesVotes.Add(player);
        }
        else
        {
            _currentVote.NoVotes.Add(player);
        }

        GameModifiersUtils.PrintTitleToChat(player, $"You voted {(voteYes ? "YES" : "NO")}.");

        // Check if vote should end
        CheckVoteStatus(eligiblePlayers);
    }

    private void CheckVoteStatus(List<CCSPlayerController> eligiblePlayers)
    {
        int requiredVotes = (int)Math.Ceiling(eligiblePlayers.Count * Config.VotePercentageRequired / 100.0f);
        
        // Check if enough YES votes
        if (_currentVote.YesVotes.Count >= requiredVotes)
        {
            EndVote(true, $"Vote passed! ({_currentVote.YesVotes.Count}/{eligiblePlayers.Count} voted YES)");
            return;
        }

        // Check if impossible to pass (too many NO votes)
        int maxPossibleYes = _currentVote.YesVotes.Count + (eligiblePlayers.Count - _currentVote.VotedPlayers.Count);
        if (maxPossibleYes < requiredVotes)
        {
            EndVote(false, $"Vote failed! Not enough YES votes possible ({_currentVote.YesVotes.Count}/{eligiblePlayers.Count})");
            return;
        }

        // Update players on current status
        int remainingTime = Config.VoteTimeoutSeconds - (int)(DateTime.Now - _currentVote.StartTime).TotalSeconds;
        GameModifiersUtils.PrintToChatAll($"Vote status: {_currentVote.YesVotes.Count} YES, {_currentVote.NoVotes.Count} NO ({_currentVote.VotedPlayers.Count}/{eligiblePlayers.Count} voted) - {remainingTime}s left");
    }

    private void EndVote(bool passed, string message)
    {
        if (!_currentVote.IsActive) return;

        GameModifiersUtils.PrintTitleToChatAll(message);

        if (passed)
        {
            // Set up round counting system
            _voteEnabledRoundsRemaining = _currentVote.RequestedRounds;
            _voteEnabledRandomRounds = true;
            
            GameModifiersUtils.PrintTitleToChatAll($"Random rounds enabled for {_currentVote.RequestedRounds} rounds starting next round!");
            
            // Enable random rounds (will activate next round, not immediately)
            if (!RandomRoundsEnabled)
            {
                ToggleRandomRounds();
            }
        }

        _lastVoteEndTime = DateTime.Now;
        _currentVote.Reset();
    }

    private List<CCSPlayerController> GetEligibleVotingPlayers()
    {
        return Utilities.GetPlayers()
            .Where(p => p != null && p.IsValid && !p.IsBot && p.Connected == PlayerConnectedState.PlayerConnected)
            .ToList();
    }
}
