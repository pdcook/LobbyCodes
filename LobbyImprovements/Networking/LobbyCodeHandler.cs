﻿using Photon.Pun;
using LobbyImprovements.Utils;
using System;
using System.Linq;
using LobbyImprovements.Extensions;

namespace LobbyImprovements.Networking
{
    public static class LobbyCodeHandler
    {
        private const string INVALIDREGION = "INVALID";
        // to keep the prefix 2 chars or less, this array should stay < 62 elements long
        // this array encodes important information like:
        // - Was this code generated by the host?
        // - What region is the room in?
        public readonly static string[] CodePrefix = new string[] 
        {   "asia",
            "au",
            "cae",
            "cn",
            "eu",
            "in",
            "jp",
            "ru",
            "rue",
            "za",
            "sa",
            "kr",
            "tr",
            "us",
            "usw", 
            "host_asia",
            "host_au",
            "host_cae",
            "host_cn",
            "host_eu",
            "host_in",
            "host_jp",
            "host_ru",
            "host_rue",
            "host_za",
            "host_sa",
            "host_kr",
            "host_tr",
            "host_us",
            "host_usw",
            INVALIDREGION }; // invalid should always be last (at index -1)

        private static string GetPureCode()
        {
            if (PhotonNetwork.OfflineMode || PhotonNetwork.CurrentRoom == null) { return ""; }
            return $"{(PhotonNetwork.IsMasterClient ? "host_" : "")}{PhotonNetwork.CloudRegion}:{PhotonNetwork.CurrentRoom.Name}".Replace("/","").Replace("*","");
        }
        public static string GetCode()
        {
            return ObfuscateJoinCode.Obfuscate(GetPureCode());
        }
        private static ExitCode PureConnectToRoom(string pureCode)
        {
            bool fromHost = pureCode.StartsWith("host_");
            pureCode = pureCode.Replace("host_", "");

            if (string.IsNullOrEmpty(pureCode)) { return ExitCode.Empty; }

            ExitCode exitCode = ExitCode.Success;

            PhotonNetwork.LocalPlayer.SetWasInvitedByHost(fromHost);

            try
            {
                string[] reg_room = pureCode.Split(':');
                string region = reg_room[0];
                string room = reg_room[1];
                LobbyImprovements.Log($"Code sent from host?: {fromHost}");
                LobbyImprovements.Log($"Code: {region}:{room}");
                if (!CodePrefix.Contains(region) || !room.All(char.IsDigit) || region == INVALIDREGION || reg_room.Count() != 2)
                {
                    throw new FormatException();
                }
                NetworkConnectionHandler.instance.ForceRegionJoin(region, room);
            }
            catch (IndexOutOfRangeException)
            {
                exitCode = ExitCode.Invalid;
            }
            catch (FormatException)
            {
                exitCode = ExitCode.Invalid;
            }
            catch (Exception)
            {
                exitCode = ExitCode.UnknownError;
            }
            return exitCode;
        }
        public static ExitCode ConnectToRoom(string obfuscatedCode)
        {
            if (string.IsNullOrWhiteSpace(obfuscatedCode)) { return ExitCode.Empty; }
            try
            {
                // trim whitespace by splitting the string at every whitespace and taking the first piece that is non-whitespace
                string trimmed = obfuscatedCode.Split((char[])null, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                if (string.IsNullOrWhiteSpace(trimmed)) { return ExitCode.Empty; }

                return PureConnectToRoom(ObfuscateJoinCode.DeObfuscate(trimmed));
            }
            catch (IndexOutOfRangeException)
            {
                return ExitCode.Invalid;
            }
            catch (FormatException)
            {
                return ExitCode.Invalid;
            }
            catch (Exception)
            {
                return ExitCode.UnknownError;
            }
        }

        public enum ExitCode
        {
            Success,
            Invalid,
            UnknownError,
            Empty
        }

    }
}
