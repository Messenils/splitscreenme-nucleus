﻿using Microsoft.Win32;
using Nucleus.Gaming.Coop;
using Nucleus.Gaming.Forms.NucleusMessageBox;
using Nucleus.Gaming.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Nucleus.Gaming.Tools.Steam
{
    public static class SteamFunctions
    {
        private static string startingArgs;
        private static long random_steam_id = 76561199023125438;
        public static bool readToEnd = false;
        public static string lobbyConnectArg;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        static extern bool CreateProcess(
            string lpApplicationName,
            string lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            [In] ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct STARTUPINFO
        {
            public Int32 cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public Int32 dwX;
            public Int32 dwY;
            public Int32 dwXSize;
            public Int32 dwYSize;
            public Int32 dwXCountChars;
            public Int32 dwYCountChars;
            public Int32 dwFillAttribute;
            public Int32 dwFlags;
            public Int16 wShowWindow;
            public Int16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        public static void UseGoldberg(string rootFolder, string nucleusRootFolder, string linkFolder, int i, PlayerInfo player, List<PlayerInfo> players, bool setupDll)
        {
            var handlerInstance = GenericGameHandler.Instance;

            handlerInstance.Log("Starting Goldberg setup");
            string utilFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "utils\\GoldbergEmu");
            string steamDllrootFolder = string.Empty;
            string steamDllFolder = string.Empty;
            string instanceSteamDllFolder = string.Empty;
            string instanceSteamSettingsFolder = string.Empty;
            string prevSteamDllFilePath = string.Empty;


            string steam64Dll = string.Empty;
            string steamDll = string.Empty;

            if (handlerInstance.currentGameInfo.GoldbergExperimental)
            {
                handlerInstance.Log("Using experimental Goldberg");
                steam64Dll += "experimental\\";
                steamDll += "experimental\\";
            }
            steam64Dll += "steam_api64.dll";
            steamDll += "steam_api.dll";

            if (handlerInstance.currentGameInfo.GoldbergExperimentalSteamClient)
            {
                handlerInstance.Log("Using Goldberg Experimental Steam Client");
                utilFolder += "\\experimental_steamclient";

                string exeFolder = Path.GetDirectoryName(handlerInstance.exePath);

                FileUtil.FileCheck(Path.Combine(exeFolder, "steamclient.dll"));
                File.Copy(Path.Combine(utilFolder, "steamclient.dll"), Path.Combine(exeFolder, "steamclient.dll"));

                FileUtil.FileCheck(Path.Combine(exeFolder, "steamclient64.dll"));
                File.Copy(Path.Combine(utilFolder, "steamclient64.dll"), Path.Combine(exeFolder, "steamclient64.dll"));

                FileUtil.FileCheck(Path.Combine(exeFolder, "steamclient_loader.exe"));
                File.Copy(Path.Combine(utilFolder, "steamclient_loader.exe"), Path.Combine(exeFolder, "steamclient_loader.exe"));

                handlerInstance.currentGameInfo.ExecutableToLaunch = Path.Combine(exeFolder, "steamclient_loader.exe");
                handlerInstance.currentGameInfo.ForceProcessSearch = true;
                handlerInstance.currentGameInfo.GoldbergWriteSteamIDAndAccount = true;

                if (i == 0)
                {
                    startingArgs = handlerInstance.currentGameInfo.StartArguments;
                }

                var sb = new StringBuilder();
                string gblines = sb.Append("#My own modified version of ColdClientLoader originally by Rat431")
                                .AppendLine()
                                .Append("[SteamClient]")
                                .AppendLine()
                                .Append($"Exe={handlerInstance.currentGameInfo.ExecutableName}")
                                .AppendLine()
                                .Append($"ExeRunDir=.")
                                .AppendLine()
                                .Append($"ExeCommandLine={startingArgs}")
                                .AppendLine()
                                .Append($"AppId={handlerInstance.currentGameInfo.SteamID}")
                                .AppendLine()
                                .AppendLine()
                                .Append("SteamClientDll=steamclient.dll")
                                .AppendLine()
                                .Append("SteamClient64Dll=steamclient64.dll")
                                .ToString();
                File.WriteAllText(Path.Combine(exeFolder, "ColdClientLoader.ini"), gblines);
                handlerInstance.addedFiles.Add(Path.Combine(exeFolder, "ColdClientLoader.ini"));

                //swalloing the launch arguments for the game here as we will be launching steamclient loader
                handlerInstance.currentGameInfo.StartArguments = string.Empty;
                handlerInstance.context.StartArguments = string.Empty;

                string settingsFolder = exeFolder + "\\settings";
                if (handlerInstance.currentGameInfo.GoldbergNoLocalSave)
                {
                    if (handlerInstance.currentGameInfo.UseNucleusEnvironment)
                    {
                        settingsFolder = $@"{handlerInstance.NucleusEnvironmentRoot}\NucleusCoop\{player.Nickname}\AppData\Roaming\Goldberg SteamEmu Saves\settings\";
                    }
                    else
                    {
                        settingsFolder = $@"{handlerInstance.NucleusEnvironmentRoot}\AppData\Roaming\Goldberg SteamEmu Saves\settings\";
                    }
                }
                else
                {
                    File.WriteAllText(Path.Combine(exeFolder, "local_save.txt"), "");
                    handlerInstance.addedFiles.Add(Path.Combine(exeFolder, "local_save.txt"));
                }

                if (!Directory.Exists(settingsFolder))
                {
                    Directory.CreateDirectory(settingsFolder);
                }

                File.WriteAllText(Path.Combine(settingsFolder, "user_steam_id.txt"), "");
                handlerInstance.addedFiles.Add(Path.Combine(settingsFolder, "user_steam_id.txt"));

                File.WriteAllText(Path.Combine(settingsFolder, "account_name.txt"), "");
                handlerInstance.addedFiles.Add(Path.Combine(settingsFolder, "account_name.txt"));

                string lang = "english";

                if (handlerInstance.ini.IniReadValue("Misc", "SteamLang") != "" && handlerInstance.ini.IniReadValue("Misc", "SteamLang") != "Automatic")
                {
                    handlerInstance.currentGameInfo.GoldbergLanguage = handlerInstance.ini.IniReadValue("Misc", "SteamLang").ToLower();
                }

                if (handlerInstance.currentGameInfo.GoldbergLanguage?.Length > 0)
                {
                    lang = handlerInstance.currentGameInfo.GoldbergLanguage;
                }
                else
                {
                    lang = handlerInstance.currentGameInfo.GetSteamLanguage();
                }

                File.WriteAllText(Path.Combine(settingsFolder, "language.txt"), lang);
                handlerInstance.addedFiles.Add(Path.Combine(settingsFolder, "language.txt"));
            }
            else
            {
                string[] steamDllFiles = Directory.GetFiles(rootFolder, "steam_api*.dll", SearchOption.AllDirectories);

                foreach (string nameFile in steamDllFiles)
                {
                    handlerInstance.Log("Found " + nameFile);
                    steamDllrootFolder = Path.GetDirectoryName(nameFile);

                    string tempRootFolder = rootFolder;
                    if (tempRootFolder.EndsWith("\\"))
                    {
                        tempRootFolder = tempRootFolder.Substring(0, tempRootFolder.Length - 1);
                    }

                    steamDllFolder = steamDllrootFolder.Remove(0, (tempRootFolder.Length));

                    instanceSteamDllFolder = linkFolder.TrimEnd('\\') + "\\" + steamDllFolder.TrimStart('\\');

                    if (handlerInstance.currentGameInfo.UseNucleusEnvironment && handlerInstance.currentGameInfo.GoldbergNoLocalSave)
                    {
                        instanceSteamSettingsFolder = $@"{handlerInstance.NucleusEnvironmentRoot}\NucleusCoop\{player.Nickname}\AppData\Roaming\Goldberg SteamEmu Saves\settings\";
                    }
                    else
                    {
                        instanceSteamSettingsFolder = Path.Combine(instanceSteamDllFolder, "settings");
                    }

                    Directory.CreateDirectory(instanceSteamSettingsFolder);
                    //Directory.CreateDirectory(instanceSteam_SettingsFolder);

                    if (setupDll)
                    {
                        if (nameFile.EndsWith("steam_api64.dll", true, null))
                        {
                            try
                            {
                                if (File.Exists(Path.Combine(instanceSteamDllFolder, "steam_api64.dll")))
                                {
                                    if (handlerInstance.currentGameInfo.GoldbergExperimental && handlerInstance.currentGameInfo.GoldbergExperimentalRename)
                                    {
                                        FileUtil.FileCheck(Path.Combine(instanceSteamDllFolder, "cracksteam_api64.dll"));
                                        handlerInstance.Log("Renaming steam_api64.dll to cracksteam_api64.dll");
                                        File.Move(Path.Combine(instanceSteamDllFolder, "steam_api64.dll"), Path.Combine(instanceSteamDllFolder, "cracksteam_api64.dll"));
                                    }
                                    else
                                    {
                                        FileUtil.FileCheck(Path.Combine(instanceSteamDllFolder, "steam_api64.dll"));
                                    }
                                }
                                handlerInstance.Log("Placing Goldberg steam_api64.dll in instance steam dll folder");
                                File.Copy(Path.Combine(utilFolder, steam64Dll), Path.Combine(instanceSteamDllFolder, "steam_api64.dll"), true);
                            }
                            catch (Exception ex)
                            {
                                handlerInstance.Log("ERROR - " + ex.Message);
                                handlerInstance.Log("Using alternative copy method for steam_api64.dll");
                                CmdUtil.ExecuteCommand(utilFolder, out int exitCode, "copy \"" + Path.Combine(utilFolder, steam64Dll) + "\" \"" + Path.Combine(instanceSteamDllFolder, "steam_api64.dll") + "\"");
                            }
                        }

                        if (nameFile.EndsWith("steam_api.dll", true, null))
                        {
                            try
                            {
                                if (File.Exists(Path.Combine(instanceSteamDllFolder, "steam_api.dll")))
                                {
                                    if (handlerInstance.currentGameInfo.GoldbergExperimental && handlerInstance.currentGameInfo.GoldbergExperimentalRename)
                                    {
                                        FileUtil.FileCheck(Path.Combine(instanceSteamDllFolder, "cracksteam_api.dll"));
                                        handlerInstance.Log("Renaming steam_api.dll to cracksteam_api.dll");
                                        File.Move(Path.Combine(instanceSteamDllFolder, "steam_api.dll"), Path.Combine(instanceSteamDllFolder, "cracksteam_api.dll"));
                                    }
                                    else
                                    {
                                        FileUtil.FileCheck(Path.Combine(instanceSteamDllFolder, "steam_api.dll"));
                                    }
                                }
                                handlerInstance.Log("Placing Goldberg steam_api.dll in instance steam dll folder");
                                File.Copy(Path.Combine(utilFolder, steamDll), Path.Combine(instanceSteamDllFolder, "steam_api.dll"), true);
                            }
                            catch (Exception ex)
                            {
                                handlerInstance.Log("ERROR - " + ex.Message);
                                handlerInstance.Log("Using alternative copy method for steam_api.dll");
                                CmdUtil.ExecuteCommand(utilFolder, out int exitCode, "copy \"" + Path.Combine(utilFolder, steamDll) + "\" \"" + Path.Combine(instanceSteamDllFolder, "steam_api.dll") + "\"");
                            }
                        }

                        if (handlerInstance.currentGameInfo.GoldbergExperimental)
                        {
                            FileUtil.FileCheck(Path.Combine(instanceSteamDllFolder, "steamclient.dll"));
                            handlerInstance.Log("Placing Goldberg steamclient.dll in instance steam dll folder");
                            File.Copy(Path.Combine(utilFolder, "experimental\\steamclient.dll"), Path.Combine(instanceSteamDllFolder, "steamclient.dll"), true);

                            FileUtil.FileCheck(Path.Combine(instanceSteamDllFolder, "steamclient64.dll"));
                            handlerInstance.Log("Placing Goldberg steamclient64.dll in instance steam dll folder");
                            File.Copy(Path.Combine(utilFolder, "experimental\\steamclient64.dll"), Path.Combine(instanceSteamDllFolder, "steamclient64.dll"), true);
                        }
                    }

                    if (!string.IsNullOrEmpty(prevSteamDllFilePath))
                    {
                        if (prevSteamDllFilePath == Path.GetDirectoryName(nameFile))
                        {
                            continue;
                        }
                    }
                    handlerInstance.Log("New steam api folder found");
                    prevSteamDllFilePath = Path.GetDirectoryName(nameFile);

                    if (handlerInstance.ini.IniReadValue("Misc", "UseNicksInGame") == "True" && !string.IsNullOrEmpty(player.Nickname))
                    {
                        if (setupDll)
                        {
                            handlerInstance.addedFiles.Add(Path.Combine(instanceSteamSettingsFolder, "account_name.txt"));
                        }

                        File.WriteAllText(Path.Combine(instanceSteamSettingsFolder, "account_name.txt"), player.Nickname);
                        handlerInstance.Log("Generating account_name.txt with nickname " + player.Nickname);
                    }
                    else
                    {
                        if (setupDll)
                        {
                            handlerInstance.addedFiles.Add(Path.Combine(instanceSteamSettingsFolder, "account_name.txt"));
                        }

                        if (handlerInstance.ini.IniReadValue("Misc", "UseNicksInGame") == "True" && handlerInstance.ini.IniReadValue("ControllerMapping", "Player_" + (i + 1)) != "")
                        {
                            File.WriteAllText(Path.Combine(instanceSteamSettingsFolder, "account_name.txt"), player.Nickname);
                            handlerInstance.Log("Generating account_name.txt with nickname " + player.Nickname);
                        }
                        else
                        {
                            File.WriteAllText(Path.Combine(instanceSteamSettingsFolder, "account_name.txt"), "Player" + (i + 1));
                            handlerInstance.Log("Generating account_name.txt with nickname Player " + (i + 1));
                        }
                    }

                    long steamID;

                    if (player.SteamID == -1)
                    {
                        steamID = SteamFunctions.random_steam_id + i;

                        while (handlerInstance.profile.DevicesList.Any(p => p.SteamID == steamID))
                        {
                            steamID = SteamFunctions.random_steam_id++;
                        }

                        player.SteamID = steamID;

                        if (handlerInstance.currentGameInfo.PlayerSteamIDs != null)
                        {
                            if (i < handlerInstance.currentGameInfo.PlayerSteamIDs.Length && !string.IsNullOrEmpty(handlerInstance.currentGameInfo.PlayerSteamIDs[i]))
                            {
                                handlerInstance.Log("Using steam ID from handler");
                                steamID = long.Parse(handlerInstance.currentGameInfo.PlayerSteamIDs[i]);
                                player.SteamID = steamID;
                            }
                        }
                    }
                    else
                    {
                        steamID = player.SteamID;
                    }

                    handlerInstance.Log("Generating user_steam_id.txt with user steam ID " + (steamID).ToString());

                    if (setupDll)
                    {
                        handlerInstance.addedFiles.Add(Path.Combine(instanceSteamSettingsFolder, "user_steam_id.txt"));
                    }

                    File.WriteAllText(Path.Combine(instanceSteamSettingsFolder, "user_steam_id.txt"), (steamID).ToString());

                    if (setupDll)
                    {
                        string lang = "english";
                        if (handlerInstance.ini.IniReadValue("Misc", "SteamLang") != "" && handlerInstance.ini.IniReadValue("Misc", "SteamLang") != "Automatic")
                        {
                            handlerInstance.currentGameInfo.GoldbergLanguage = handlerInstance.ini.IniReadValue("Misc", "SteamLang").ToLower();
                        }
                        if (handlerInstance.currentGameInfo.GoldbergLanguage?.Length > 0)
                        {
                            lang = handlerInstance.currentGameInfo.GoldbergLanguage;
                        }
                        else
                        {
                            lang = handlerInstance.currentGameInfo.GetSteamLanguage();
                        }

                        handlerInstance.Log("Generating language.txt with language set to " + lang);
                        handlerInstance.addedFiles.Add(Path.Combine(instanceSteamSettingsFolder, "language.txt"));
                        File.WriteAllText(Path.Combine(instanceSteamSettingsFolder, "language.txt"), lang);

                        if (handlerInstance.currentGameInfo.GoldbergIgnoreSteamAppId)
                        {
                            handlerInstance.Log("Skipping steam_appid.txt creation");
                        }
                        else
                        {
                            handlerInstance.Log("Generating steam_appid.txt using game steam ID " + handlerInstance.currentGameInfo.SteamID);
                            handlerInstance.addedFiles.Add(Path.Combine(instanceSteamDllFolder, "steam_appid.txt"));
                            File.WriteAllText(Path.Combine(instanceSteamDllFolder, "steam_appid.txt"), handlerInstance.currentGameInfo.SteamID);
                        }

                        handlerInstance.addedFiles.Add(Path.Combine(instanceSteamDllFolder, "local_save.txt"));
                        if (!handlerInstance.currentGameInfo.GoldbergNoLocalSave)
                        {
                            File.WriteAllText(Path.Combine(instanceSteamDllFolder, "local_save.txt"), "");
                        }

                        if (handlerInstance.currentGameInfo.GoldbergNeedSteamInterface)
                        {
                            handlerInstance.addedFiles.Add(Path.Combine(instanceSteamDllFolder, "steam_interfaces.txt"));
                            if (File.Exists(Path.Combine(utilFolder, "tools\\steam_interfaces.txt")))
                            {
                                handlerInstance.Log("Found generated steam_interfaces.txt file in Nucleus util folder, copying this file");

                                File.Copy(Path.Combine(utilFolder, "tools\\steam_interfaces.txt"), Path.Combine(instanceSteamDllFolder, "steam_interfaces.txt"), true);
                            }
                            else if (File.Exists(Path.Combine(steamDllrootFolder, "steam_interfaces.txt")))
                            {
                                handlerInstance.Log("Found steam_interfaces.txt in original game folder, copying this file");
                                File.Copy(Path.Combine(steamDllrootFolder, "steam_interfaces.txt"), Path.Combine(instanceSteamDllFolder, "steam_interfaces.txt"), true);
                            }
                            else if (File.Exists(nucleusRootFolder.TrimEnd('\\') + "\\handlers\\" + handlerInstance.currentGameInfo.JsFileName.Substring(0, handlerInstance.currentGameInfo.JsFileName.Length - 3) + "\\steam_interfaces.txt"))
                            {
                                handlerInstance.Log("Found steam_interfaces.txt in Nucleus game folder");
                                File.Copy(nucleusRootFolder.TrimEnd('\\') + "\\handlers\\" + handlerInstance.currentGameInfo.JsFileName.Substring(0, handlerInstance.currentGameInfo.JsFileName.Length - 3) + "\\steam_interfaces.txt", Path.Combine(instanceSteamDllFolder, "steam_interfaces.txt"), true);
                            }
                            else if (handlerInstance.currentGameInfo.OrigSteamDllPath?.Length > 0 && File.Exists(handlerInstance.currentGameInfo.OrigSteamDllPath))
                            {
                                handlerInstance.Log("Attempting to create steam_interfaces.txt with the steam api dll located at: " + handlerInstance.currentGameInfo.OrigSteamDllPath);
                                Process cmd = new Process();
                                cmd.StartInfo.FileName = "cmd.exe";
                                cmd.StartInfo.RedirectStandardInput = true;
                                cmd.StartInfo.RedirectStandardOutput = true;
                                cmd.StartInfo.UseShellExecute = false;
                                cmd.StartInfo.WorkingDirectory = Path.Combine(utilFolder, "tools");
                                cmd.Start();

                                cmd.StandardInput.WriteLine("generate_interfaces_file.exe \"" + handlerInstance.currentGameInfo.OrigSteamDllPath + "\"");
                                cmd.StandardInput.Flush();
                                cmd.StandardInput.Close();
                                cmd.WaitForExit();
                                handlerInstance.addedFiles.Add(Path.Combine(instanceSteamDllFolder, "steam_intterfaces.txt"));
                                handlerInstance.Log("Copying over generated steam_interfaces.txt");
                                File.Copy(Path.Combine(Path.Combine(utilFolder, "tools"), "steam_interfaces.txt"), Path.Combine(instanceSteamDllFolder, "steam_interfaces.txt"), true);

                                if ((i + 1) == players.Count)
                                {
                                    handlerInstance.Log("Deleting generated steam_interfaces.txt file");
                                    File.Delete(Path.Combine(utilFolder, "tools\\steam_interfaces.txt"));
                                }
                            }
                            else
                            {
                                handlerInstance.Log("Unable to locate steam_interfaces.txt or create one, skipping this file");

                                if (i == 0)
                                {
                                    MessageBox.Show("Goldberg was unable to locate steam_interfaces.txt or create one. Process will continue without using this file.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                }
                            }
                        }
                    }
                }

                if (steamDllFiles == null || steamDllFiles.Length < 1)
                {
                    if (!handlerInstance.currentGameInfo.GoldbergNoWarning)
                    {
                        handlerInstance.Log("Unable to locate a steam_api(64).dll file, Goldberg will not be used");
                        NucleusMessageBox.Show("Warning", "Goldberg was unable to locate a steam_api(64).dll file.\nThe built-in Goldberg will not be used.", false);
                    }
                }
            }

            handlerInstance.Log("Goldberg setup complete");
        }

        public static void GoldbergWriteSteamIDAndAccount(string linkFolder, int i, PlayerInfo player)
        {
            var handlerInstance = GenericGameHandler.Instance;

            string[] saFiles = Directory.GetFiles(linkFolder, "account_name.txt", SearchOption.AllDirectories);
            List<string> files = saFiles.ToList();

            if (saFiles.Length > 0)
            {
                handlerInstance.Log("account_name.txt file(s) were found in folder. Will update nicknames");
            }

            string goldbergNoLocal = $@"{handlerInstance.NucleusEnvironmentRoot}\AppData\Roaming\Goldberg SteamEmu Saves\settings\account_name.txt";


            if (File.Exists(goldbergNoLocal))
            {
                files.Add(goldbergNoLocal);
            }

            if (handlerInstance.currentGameInfo.UseNucleusEnvironment)
            {
                goldbergNoLocal = $@"{handlerInstance.NucleusEnvironmentRoot}\NucleusCoop\{player.Nickname}\AppData\Roaming\Goldberg SteamEmu Saves\settings\account_name.txt";
                if (File.Exists(goldbergNoLocal))
                {
                    files.Add(goldbergNoLocal);
                }
            }

            foreach (string nameFile in files)
            {
                if (!string.IsNullOrEmpty(player.Nickname))
                {
                    handlerInstance.Log(string.Format("Writing nickname {0} in account_name.txt", player.Nickname));
                    File.Delete(nameFile);
                    File.WriteAllText(nameFile, player.Nickname);
                }
                else
                {
                    handlerInstance.Log("Writing nickname {0} in account_name.txt " + "Player" + (i + 1));
                    File.Delete(nameFile);
                    File.WriteAllText(nameFile, "Player" + (i + 1));
                }
            }

            saFiles = Directory.GetFiles(linkFolder, "user_steam_id.txt", SearchOption.AllDirectories);
            files = saFiles.ToList();

            if (saFiles.Length > 0)
            {
                handlerInstance.Log("user_steam_id.txt file(s) were found in folder. Will update nicknames");
            }

            goldbergNoLocal = $@"{handlerInstance.NucleusEnvironmentRoot}\AppData\Roaming\Goldberg SteamEmu Saves\settings\user_steam_id.txt";

            if (File.Exists(goldbergNoLocal))
            {
                files.Add(goldbergNoLocal);
            }

            if (handlerInstance.currentGameInfo.UseNucleusEnvironment)
            {
                goldbergNoLocal = $@"{handlerInstance.NucleusEnvironmentRoot}\NucleusCoop\{player.Nickname}\AppData\Roaming\Goldberg SteamEmu Saves\settings\user_steam_id.txt";

                if (File.Exists(goldbergNoLocal))
                {
                    files.Add(goldbergNoLocal);
                }
            }

            foreach (string nameFile in files)
            {
                long steamID;

                if (player.SteamID == -1)
                {
                    steamID = SteamFunctions.random_steam_id + i;

                    while (handlerInstance.profile.DevicesList.Any(p => (p != player) && p.SteamID == steamID))
                    {
                        steamID = SteamFunctions.random_steam_id++;
                    }

                    player.SteamID = steamID;

                    if (handlerInstance.currentGameInfo.PlayerSteamIDs != null)
                    {
                        if (i < handlerInstance.currentGameInfo.PlayerSteamIDs.Length && !string.IsNullOrEmpty(handlerInstance.currentGameInfo.PlayerSteamIDs[i]))
                        {
                            handlerInstance.Log("Using steam ID from handler");
                            steamID = long.Parse(handlerInstance.currentGameInfo.PlayerSteamIDs[i]);
                            player.SteamID = steamID;
                        }
                    }
                }
                else
                {
                    steamID = player.SteamID;
                }

                handlerInstance.Log("Generating user_steam_id.txt with user steam ID " + (steamID).ToString());

                File.Delete(nameFile);
                File.WriteAllText(nameFile, (steamID).ToString());
            }
        }

        public static void GoldbergLobbyConnect()
        {
            var handlerInstance = GenericGameHandler.Instance;

            Forms.Prompt prompt = new Forms.Prompt("Goldberg Lobby Connect: Press OK after you are hosting a game.");
            prompt.ShowDialog();

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "utils\\GoldbergEmu\\lobby_connect\\lobby_connect.exe");

            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            Process p = Process.Start(startInfo);
            p.OutputDataReceived += ProcessUtil.proc_OutputDataReceived;
            p.BeginOutputReadLine();

            while (readToEnd == false)
            {
                if (GenericGameHandler.Instance.HasEnded)
                {
                    break;
                }

                Thread.Sleep(25);
            }

            try
            {
                Thread.Sleep(2500);
                p.Kill();
            }
            catch (Exception ex)
            { MessageBox.Show(ex.Message); }

            if (!string.IsNullOrEmpty(lobbyConnectArg))
            {
                handlerInstance.Log("Goldberg Lobby Connect: Setting lobby ID to " + lobbyConnectArg.Substring(15));
            }
            else
            {
                handlerInstance.Log("Goldberg Lobby Connect: Could not find lobby ID");
                MessageBox.Show("Goldberg Lobby Connect: Could not find lobby ID.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static void CreateSteamAppIdByExe(bool setupDll)
        {
            var handlerInstance = GenericGameHandler.Instance;

            handlerInstance.Log("Creating steam_appid.txt with steam ID " + handlerInstance.currentGameInfo.SteamID + " at " + handlerInstance.instanceExeFolder);

            if (File.Exists(Path.Combine(handlerInstance.instanceExeFolder, "steam_appid.txt")))
            {
                File.Delete(Path.Combine(handlerInstance.instanceExeFolder, "steam_appid.txt"));
            }

            File.WriteAllText(Path.Combine(handlerInstance.instanceExeFolder, "steam_appid.txt"), handlerInstance.currentGameInfo.SteamID);

            if (setupDll)
            {
                handlerInstance.addedFiles.Add(Path.Combine(handlerInstance.instanceExeFolder, "steam_appid.txt"));
            }
        }

        public static void SmartSteamEmu(PlayerInfo player, int i, string linkFolder, string startArgs, bool setupDll)
        {
            var handlerInstance = GenericGameHandler.Instance;

            string steamEmu = Path.Combine(linkFolder, "SmartSteamLoader");
            handlerInstance.ssePath = steamEmu;
            string sourcePath = Path.Combine(GameManager.Instance.GetUtilsPath(), "SmartSteamEmu");

            if (setupDll)
            {
                handlerInstance.Log("Setting up SmartSteamEmu");
                handlerInstance.Log(string.Format("Copying SmartSteamEmu files to {0}", steamEmu));

                try
                {
                    //Create all of the directories
                    foreach (string dirPath in Directory.GetDirectories(sourcePath, "*",
                        SearchOption.AllDirectories))
                    {
                        Directory.CreateDirectory(dirPath.Replace(sourcePath, steamEmu));
                    }

                    //Copy all the files & Replaces any files with the same name
                    foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
                    {
                        File.Copy(newPath, newPath.Replace(sourcePath, steamEmu), true);
                    }
                }
                catch
                {
                    handlerInstance.Log(string.Format("ERROR - Copying of SmartSteamEmu files failed!"));
                    //return "Extraction of SmartSteamEmu failed!";
                }
            }

            string sseLoader = string.Empty;
            if (handlerInstance.gameIs64)
            {
                sseLoader = "SmartSteamLoader_x64.exe";
            }
            else
            {
                sseLoader = "SmartSteamLoader.exe";
            }

            string emuExe = Path.Combine(steamEmu, sseLoader);
            string emuIni = Path.Combine(steamEmu, "SmartSteamEmu.ini");
            IniFile emu = new IniFile(emuIni);

            handlerInstance.Log("Writing SmartSteamEmu.ini");
            emu.IniWriteValue("Launcher", "Target", handlerInstance.exePath);
            emu.IniWriteValue("Launcher", "StartIn", Path.GetDirectoryName(handlerInstance.exePath));
            emu.IniWriteValue("Launcher", "CommandLine", startArgs);
            emu.IniWriteValue("Launcher", "SteamClientPath", Path.Combine(steamEmu, "SmartSteamEmu.dll"));
            emu.IniWriteValue("Launcher", "SteamClientPath64", Path.Combine(steamEmu, "SmartSteamEmu64.dll"));
            emu.IniWriteValue("Launcher", "InjectDll", "1");

            emu.IniWriteValue("SmartSteamEmu", "AppId", handlerInstance.context.SteamID);
            emu.IniWriteValue("SmartSteamEmu", "SteamIdGeneration", "Manual");

            long steamID;

            if (player.SteamID == -1)
            {
                steamID = SteamFunctions.random_steam_id + i;

                while (handlerInstance.profile.DevicesList.Any(p => (p != player) && p.SteamID == steamID))
                {
                    steamID = SteamFunctions.random_steam_id++;
                }

                emu.IniWriteValue("SmartSteamEmu", "ManualSteamId", (steamID).ToString());
                player.SteamID = steamID;

                if (handlerInstance.currentGameInfo.PlayerSteamIDs != null)
                {
                    if (i < handlerInstance.currentGameInfo.PlayerSteamIDs.Length && !string.IsNullOrEmpty(handlerInstance.currentGameInfo.PlayerSteamIDs[i]))
                    {
                        handlerInstance.Log("Using steam ID from handler");
                        emu.IniWriteValue("SmartSteamEmu", "ManualSteamId", handlerInstance.currentGameInfo.PlayerSteamIDs[i].ToString());
                        player.SteamID = long.Parse(handlerInstance.currentGameInfo.PlayerSteamIDs[i]);
                    }
                }
            }
            else
            {
                emu.IniWriteValue("SmartSteamEmu", "ManualSteamId", player.SteamID.ToString());
            }

            string lang = "english";

            if (handlerInstance.ini.IniReadValue("Misc", "SteamLang") != "" && handlerInstance.ini.IniReadValue("Misc", "SteamLang") != "Automatic")
            {
                handlerInstance.currentGameInfo.GoldbergLanguage = handlerInstance.ini.IniReadValue("Misc", "SteamLang").ToLower();
            }

            if (handlerInstance.currentGameInfo.GoldbergLanguage?.Length > 0)
            {
                lang = handlerInstance.currentGameInfo.GoldbergLanguage;
            }
            else
            {
                lang = handlerInstance.currentGameInfo.GetSteamLanguage();
            }

            emu.IniWriteValue("SmartSteamEmu", "Language", lang);

            if (handlerInstance.ini.IniReadValue("Misc", "UseNicksInGame") == "True" && !string.IsNullOrEmpty(player.Nickname))
            {
                emu.IniWriteValue("SmartSteamEmu", "PersonaName", player.Nickname);
            }
            else
            {
                emu.IniWriteValue("SmartSteamEmu", "PersonaName", "Player" + (i + 1));
            }

            emu.IniWriteValue("SmartSteamEmu", "DisableOverlay", "1");
            emu.IniWriteValue("SmartSteamEmu", "SeparateStorageByName", "1");

            if (handlerInstance.currentGameInfo.SSEAdditionalLines?.Length > 0)
            {
                foreach (string line in handlerInstance.currentGameInfo.SSEAdditionalLines)
                {
                    if (line.Contains("|") && line.Contains("="))
                    {
                        string[] lineSplit = line.Split('|', '=');
                        if (lineSplit?.Length == 3)
                        {
                            string section = lineSplit[0];
                            string key = lineSplit[1];
                            string value = lineSplit[2];
                            handlerInstance.Log(string.Format("Writing custom line in SSE, section: {0}, key: {1}, value: {2}", section, key, value));
                            emu.IniWriteValue(section, key, value);
                        }
                    }
                }
            }

            if (handlerInstance.currentGameInfo.ForceEnvironmentUse && handlerInstance.currentGameInfo.ThirdPartyLaunch)
            {
                handlerInstance.Log("Force Nucleus environment use");

                IntPtr envPtr = IntPtr.Zero;

                if (handlerInstance.currentGameInfo.UseNucleusEnvironment)
                {
                    handlerInstance.Log("Setting up Nucleus environment");
                    var sb = new StringBuilder();
                    System.Collections.IDictionary envVars = Environment.GetEnvironmentVariables();
                    string username = WindowsIdentity.GetCurrent().Name.Split('\\')[1];
                    envVars["USERPROFILE"] = $@"{handlerInstance.NucleusEnvironmentRoot}\NucleusCoop\{player.Nickname}";
                    envVars["HOMEPATH"] = $@"\Users\{username}\NucleusCoop\{player.Nickname}";
                    envVars["APPDATA"] = $@"{handlerInstance.NucleusEnvironmentRoot}\NucleusCoop\{player.Nickname}\AppData\Roaming";
                    envVars["LOCALAPPDATA"] = $@"{handlerInstance.NucleusEnvironmentRoot}\NucleusCoop\{player.Nickname}\AppData\Local";

                    //Some games will crash if the directories don't exist
                    Directory.CreateDirectory($@"{handlerInstance.NucleusEnvironmentRoot}\NucleusCoop");
                    Directory.CreateDirectory(envVars["USERPROFILE"].ToString());
                    Directory.CreateDirectory(Path.Combine(envVars["USERPROFILE"].ToString(), "Documents"));
                    Directory.CreateDirectory(envVars["APPDATA"].ToString());
                    Directory.CreateDirectory(envVars["LOCALAPPDATA"].ToString());
                    Directory.CreateDirectory(Path.GetDirectoryName(handlerInstance.DocumentsRoot) + $@"\NucleusCoop\{player.Nickname}\Documents");

                    if (handlerInstance.currentGameInfo.DocumentsConfigPath?.Length > 0 || handlerInstance.currentGameInfo.DocumentsSavePath?.Length > 0)
                    {
                        if (!File.Exists(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), @"utils\backup\User Shell Folders.reg")))
                        {
                            RegistryUtil.ExportRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders", Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), @"utils\backup\User Shell Folders.reg"));
                        }

                        RegistryKey dkey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders", true);
                        dkey.SetValue("Personal", Path.GetDirectoryName(handlerInstance.DocumentsRoot) + $@"\NucleusCoop\{player.Nickname}\Documents", (RegistryValueKind)(int)RegType.ExpandString);
                    }

                    foreach (object envVarKey in envVars.Keys)
                    {
                        if (envVarKey != null)
                        {
                            string key = envVarKey.ToString();
                            string value = envVars[envVarKey].ToString();

                            sb.Append(key);
                            sb.Append("=");
                            sb.Append(value);
                            sb.Append("\0");
                        }
                    }

                    sb.Append("\0");

                    byte[] envBytes = Encoding.Unicode.GetBytes(sb.ToString());
                    envPtr = Marshal.AllocHGlobal(envBytes.Length);
                    Marshal.Copy(envBytes, 0, envPtr, envBytes.Length);
                }
            }

            if (!handlerInstance.currentGameInfo.ThirdPartyLaunch)
            {
                if (handlerInstance.context.KillMutex?.Length > 0)
                {
                    // to kill the mutexes we need to orphanize the process
                    while (!ProcessUtil.RunOrphanProcess(emuExe, handlerInstance.currentGameInfo.UseNucleusEnvironment, player.Nickname))
                    {
                        if (GenericGameHandler.Instance.HasEnded)
                        {
                            break;
                        }

                        Thread.Sleep(25);
                    }
                    handlerInstance.Log("Terminal session launched SmartSteamEmu as an orphan in order to kill mutexes in future");
                }
                else
                {
                    IntPtr envPtr = IntPtr.Zero;

                    if (handlerInstance.currentGameInfo.UseNucleusEnvironment)
                    {
                        handlerInstance.Log("Setting up Nucleus environment");
                        var sb = new StringBuilder();
                        System.Collections.IDictionary envVars = Environment.GetEnvironmentVariables();
                        string username = WindowsIdentity.GetCurrent().Name.Split('\\')[1];
                        envVars["USERPROFILE"] = $@"{handlerInstance.NucleusEnvironmentRoot}\NucleusCoop\{player.Nickname}";
                        envVars["HOMEPATH"] = $@"\Users\{username}\NucleusCoop\{player.Nickname}";
                        envVars["APPDATA"] = $@"{handlerInstance.NucleusEnvironmentRoot}\NucleusCoop\{player.Nickname}\AppData\Roaming";
                        envVars["LOCALAPPDATA"] = $@"{handlerInstance.NucleusEnvironmentRoot}\NucleusCoop\{player.Nickname}\AppData\Local";

                        //Some games will crash if the directories don't exist
                        Directory.CreateDirectory($@"{handlerInstance.NucleusEnvironmentRoot}\NucleusCoop");
                        Directory.CreateDirectory(envVars["USERPROFILE"].ToString());
                        Directory.CreateDirectory(Path.Combine(envVars["USERPROFILE"].ToString(), "Documents"));
                        Directory.CreateDirectory(envVars["APPDATA"].ToString());
                        Directory.CreateDirectory(envVars["LOCALAPPDATA"].ToString());
                        Directory.CreateDirectory(Path.GetDirectoryName(handlerInstance.DocumentsRoot) + $@"\NucleusCoop\{player.Nickname}\Documents");

                        if (handlerInstance.currentGameInfo.DocumentsConfigPath?.Length > 0 || handlerInstance.currentGameInfo.DocumentsSavePath?.Length > 0)
                        {
                            if (!File.Exists(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), @"utils\backup\User Shell Folders.reg")))
                            {
                                RegistryUtil.ExportRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders", Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), @"utils\backup\User Shell Folders.reg"));
                            }

                            RegistryKey dkey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders", true);
                            dkey.SetValue("Personal", Path.GetDirectoryName(handlerInstance.DocumentsRoot) + $@"\NucleusCoop\{player.Nickname}\Documents", (RegistryValueKind)(int)RegType.ExpandString);
                        }

                        foreach (object envVarKey in envVars.Keys)
                        {
                            if (envVarKey != null)
                            {
                                string key = envVarKey.ToString();
                                string value = envVars[envVarKey].ToString();

                                sb.Append(key);
                                sb.Append("=");
                                sb.Append(value);
                                sb.Append("\0");
                            }
                        }

                        sb.Append("\0");

                        byte[] envBytes = Encoding.Unicode.GetBytes(sb.ToString());
                        envPtr = Marshal.AllocHGlobal(envBytes.Length);
                        Marshal.Copy(envBytes, 0, envPtr, envBytes.Length);

                    }

                    ProcessUtil.STARTUPINFO startup = new ProcessUtil.STARTUPINFO();
                    startup.cb = Marshal.SizeOf(startup);

                    bool success = ProcessUtil.CreateProcess(null, emuExe, IntPtr.Zero, IntPtr.Zero, false, (uint)ProcessUtil.ProcessCreationFlags.CREATE_UNICODE_ENVIRONMENT, envPtr, Path.GetDirectoryName(handlerInstance.exePath), ref startup, out ProcessUtil.PROCESS_INFORMATION processInformation);

                    if (!success)
                    {
                        int error = Marshal.GetLastWin32Error();
                        handlerInstance.Log(string.Format("ERROR {0} - CreateProcess failed - startGamePath: {1}, startArgs: {2}, dirpath: {3}", error, handlerInstance.exePath, startArgs, Path.GetDirectoryName(handlerInstance.exePath)));
                    }

                    Process proc = Process.GetProcessById(processInformation.dwProcessId);
                    handlerInstance.Log(string.Format("Started process {0} (pid {1})", proc.ProcessName, proc.Id));
                }
            }
            else
            {
                handlerInstance.Log("Skipping launching of game via Nucleus for third party launch");
            }

            handlerInstance.Log("SmartSteamEmu setup complete");
        }

        public static void UseSteamStubDRMPatcher(bool setupDll)
        {
            var handlerInstance = GenericGameHandler.Instance;

            if (setupDll)
            {
                string utilFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "utils\\Steam Stub DRM Patcher");

                string archToUse = handlerInstance.garch;
                if (handlerInstance.currentGameInfo.SteamStubDRMPatcherArch?.Length > 0)
                {
                    archToUse = "x" + handlerInstance.currentGameInfo.SteamStubDRMPatcherArch;
                }

                FileUtil.FileCheck(Path.Combine(handlerInstance.instanceExeFolder, "winmm.dll"));
                try
                {
                    handlerInstance.Log(string.Format("Copying over winmm.dll ({0})", archToUse));
                    File.Copy(Path.Combine(utilFolder, archToUse + "\\winmm.dll"), Path.Combine(handlerInstance.instanceExeFolder, "winmm.dll"), true);
                }

                catch (Exception ex)
                {
                    handlerInstance.Log("ERROR - " + ex.Message);
                    handlerInstance.Log("Using alternative copy method for winmm.dll");
                    CmdUtil.ExecuteCommand(utilFolder, out int exitCode, "copy \"" + Path.Combine(utilFolder, archToUse + "\\winmm.dll") + "\" \"" + Path.Combine(handlerInstance.instanceExeFolder, "winmm.dll") + "\"");
                }
            }
        }

        public static void SteamlessProc(string linkBinFolder, string executableName, string args, int timing)
        {
            try
            {
                string steamlessExePath = Path.Combine($@"{Directory.GetCurrentDirectory()}\utils\Steamless\Steamless.CLI.exe");

                string steamlessArgs = $@"{args} {linkBinFolder} \ {executableName}";

                ProcessStartInfo sl = new ProcessStartInfo(steamlessExePath);
                sl.WorkingDirectory = linkBinFolder;
                sl.UseShellExecute = true;
                sl.WindowStyle = ProcessWindowStyle.Hidden;
                sl.Arguments = steamlessArgs;
                Process.Start(sl);
                Thread.Sleep(timing);

                if (File.Exists($@"{linkBinFolder}\{executableName}.unpacked.exe"))
                {
                    File.Delete($@"{linkBinFolder}\{executableName}");
                    File.Move($@"{linkBinFolder}\{executableName}.unpacked.exe", $@"{linkBinFolder}\{executableName}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            };
        }

        public static void StartSteamClient()
        {
            if (Process.GetProcessesByName("steam").Length == 0)
            {
                Console.WriteLine("Steam Client Not Initialized");

                string steamExePath = string.Empty;
                RegistryKey localKey = RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.CurrentUser, RegistryView.Registry32);

                localKey = localKey.OpenSubKey(@"Software\Valve\Steam");

                if (localKey != null)
                {
                    steamExePath = localKey.GetValue("SteamExe").ToString();
                    localKey.Close();
                }

                ProcessStartInfo sc = new ProcessStartInfo(steamExePath);
                sc.UseShellExecute = true;
                sc.Arguments = "-silent";

                Process.Start(sc);
            }

            while (Process.GetProcessesByName("steamwebhelper").Length < 6) { }

            Console.WriteLine("Steam Client Initialized");
        }
    }
}
