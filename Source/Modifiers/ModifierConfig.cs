using System;
using System.Collections.Generic;
using System.IO;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;

namespace GameModifiers.Modifiers;

public class ModifierConfig
{
    // List of commands applied for the duration of this modifier.
    private readonly List<string> _conVarsServer = new();
    private readonly List<string> _conVarsClient = new();

    // List of commands to revert this config modifier.
    private readonly List<string> _conVarsServerRollback = new();

    // List of client commands to rollback mapped to the client slot. This is removed on disconnect.
    private readonly Dictionary<int, List<string>> _conVarsClientRollback = new();

    public virtual void ApplyConfig()
    {
        foreach (string conVar in _conVarsServer)
        {
            Console.WriteLine($"[ModifierConfig::Enabled] Reading line: ({conVar})");

            string[] conVarParts = conVar.Split(new char[] {' '}, 2, StringSplitOptions.RemoveEmptyEntries);
            if (conVarParts.Length == 2)
            {
                ConVar? foundConVar = ConVar.Find(conVarParts[0]);
                if (foundConVar == null)
                {
                    Console.WriteLine($"[ModifierConfig::Enabled] Cannot find server command: {conVar}");
                    continue;
                }

                string conVarValue = GameModifiersUtils.GetConVarStringValue(foundConVar);
                _conVarsServerRollback.Add($"{conVarParts[0]} {conVarValue}");

                NativeAPI.IssueServerCommand(conVar);

                Console.WriteLine($"[ModifierConfig::Enabled] Executing server command: {conVar}");
            }
        }

        Utilities.GetPlayers().ForEach(ApplyClientConfig);
    }

    public virtual void RemoveConfig()
    {
        Utilities.GetPlayers().ForEach(RemoveClientConfig);

        foreach (string conVar in _conVarsServerRollback)
        {
            Console.WriteLine($"[ModifierConfig::Disabled] Executing rollback command: {conVar}");
            NativeAPI.IssueServerCommand(conVar);
        }

        _conVarsServerRollback.Clear();
        _conVarsClientRollback.Clear();
    }

    public virtual bool ParseConfigFile(string filePath)
    {
        _conVarsServer.Clear();
        _conVarsClient.Clear();

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"[ModifierConfig::ParseConfigFile] File not found: {filePath}");
            return false;
        }

        string[] lines = File.ReadAllLines(filePath);

        bool isClientCommand = false;

        foreach (string line in lines)
        {
            if (line.StartsWith("//"))
            {
                continue;
            }

            if (line.Contains("Client:"))
            {
                isClientCommand = true;
                continue;
            }

            int commentStartIndex = line.IndexOf("//", StringComparison.Ordinal);
            string configLine = commentStartIndex >= 0 ? line.Substring(0, commentStartIndex).Trim() : line;
            if (isClientCommand == false)
            {
                if (ParseConfigLine(configLine, isClientCommand))
                {
                    continue;
                }
            }

            string noWhitespaceLine = configLine.Trim();
            if (noWhitespaceLine.Length > 0)
            {
                if (isClientCommand)
                {
                    _conVarsClient.Add(configLine);
                }
                else
                {
                    _conVarsServer.Add(configLine);
                }
            }
        }

        return true;
    }

    public virtual bool ParseConfigLine(string line, bool isClientCommand)
    {
        return false;
    }

    public void ApplyClientConfig(CCSPlayerController? player)
    {
        if (player == null)
        {
            Console.WriteLine("[ModifierConfig::ApplyClientConfig] Failed to apply client config. Player is null?!");
            return;
        }

        if (_conVarsClientRollback.ContainsKey(player.Slot) == false)
        {
            _conVarsClientRollback.Add(player.Slot, new List<string>());
        }

        List<string> clientConVarRollbackList = _conVarsClientRollback[player.Slot];

        foreach (string conVar in _conVarsClient)
        {
            string[] conVarParts = conVar.Split(new char[] {' '}, 2, StringSplitOptions.RemoveEmptyEntries);
            if (conVarParts.Length == 2)
            {
                ConVar? foundConVar = ConVar.Find(conVarParts[0]);
                if (foundConVar == null)
                {
                    Console.WriteLine($"[ModifierConfig::ApplyClientConfig] Cvar not found! Cannot modify! ({conVarParts[0]})");
                    return;
                }

                // Removed foundConVar.Flags checks - try replication first, fallback to server command
                try
                {
                    string clientValue = player.GetConVarValue(conVarParts[0]);
                    clientConVarRollbackList.Add($"{conVarParts[0]} {clientValue}");
                    player.ReplicateConVar(conVarParts[0], conVarParts[1]);
                }
                catch
                {
                    // If replication fails, try server command
                    string defaultValue = GameModifiersUtils.GetConVarStringValue(foundConVar);
                    clientConVarRollbackList.Add($"{conVarParts[0]} {defaultValue}");
                    player.ExecuteClientCommandFromServer(conVar);
                }
            }
        }
    }

    public void RemoveClientConfig(CCSPlayerController? player)
    {
        if (player == null)
        {
            Console.WriteLine("[ModifierConfig::RemoveClientConfig] Failed to remove client config. Player is null?!");
            return;
        }

        if (_conVarsClientRollback.ContainsKey(player.Slot) == false)
        {
            return;
        }

        List<string> clientConVarRollbackList = _conVarsClientRollback[player.Slot];

        foreach (string conVar in clientConVarRollbackList)
        {
            string[] conVarParts = conVar.Split(new char[] {' '}, 2, StringSplitOptions.RemoveEmptyEntries);
            if (conVarParts.Length == 2)
            {
                ConVar? foundConVar = ConVar.Find(conVarParts[0]);
                if (foundConVar == null)
                {
                    Console.WriteLine($"[ModifierConfig::RemoveClientConfig] Cvar not found! ({conVarParts[0]})");
                    return;
                }

                // Removed foundConVar.Flags checks - try replication first, fallback to server command
                try
                {
                    player.ReplicateConVar(conVarParts[0], conVarParts[1]);
                }
                catch
                {
                    player.ExecuteClientCommandFromServer(conVar);
                }
            }
        }
    }
}