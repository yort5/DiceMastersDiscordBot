﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DiceMastersDiscordBot.Entities;

namespace DiceMastersDiscordBot.Events
{
    public interface IDiceMastersEvent
    {
        public string ChannelName { get; }
        public string SheetId { get; }
        public string SheetName { get; }
        public bool UsesChallonge { get; }

        public void Initialize(EventManifest manifest);

        public string GetFormat(int numberEvents);
        public string SubmitTeamLink(EventUserInput eventUserInput);
        public int GetCurrentPlayerCount();
        public List<EventUserInput> GetTeamLists();
        public Task<List<UserInfo>> GetCurrentPlayerList();
        public Task<string> MarkPlayerHereAsync(EventUserInput eventUserInput);
        public string MarkPlayerDropped(EventUserInput eventUserInput);
    }
}
