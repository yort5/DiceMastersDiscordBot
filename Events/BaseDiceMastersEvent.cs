using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ChallongeSharp.Models.ViewModels;
using ChallongeSharp.Models.ViewModels.Types;
using DiceMastersDiscordBot.Entities;
using DiceMastersDiscordBot.Properties;
using DiceMastersDiscordBot.Services;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DiceMastersDiscordBot.Events
{
    public class BaseDiceMastersEvent : IDiceMastersEvent
    {

        protected readonly ILogger _logger;
        protected readonly IAppSettings _settings;
        protected readonly DMSheetService _sheetService;
        protected ChallongeEvent _challonge;

        protected HomeSheet _homeSheet = new HomeSheet();
        protected bool _useChallonge = false;
        protected string _channelCode;
        protected List<string> _eventOrganizerDiscordIds;

        public BaseDiceMastersEvent(ILoggerFactory loggerFactory,
                            IAppSettings appSettings,
                            DMSheetService dMSheetService,
                            ChallongeEvent challonge)
        {
            _logger = loggerFactory.CreateLogger<BaseDiceMastersEvent>();
            _settings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _sheetService = dMSheetService;
            _challonge = challonge;
        }

        public string ChannelName { get { return _homeSheet.EventName; } }
        public string ChannelCode { get { return _channelCode; } }
        public bool UsesChallonge { get { return _useChallonge; } }
        public string SheetId { get { return _homeSheet.SheetId; } }
        public string SheetName { get { return _homeSheet.SheetName; } }

        public virtual void Initialize(EventManifest manifest)
        {
            _homeSheet = _sheetService.GetHomeSheet(manifest.EventSheetId, manifest.EventName);
            _channelCode = manifest.EventCode;
            _eventOrganizerDiscordIds = manifest.EventOrganizerIDList;
        }

        public string GetFormat()
        {
            try
            {
                // TODO: check if there are any upcoming events and return those as well
                var nl = Environment.NewLine;
                var eventName = _homeSheet.EventName != null ? string.Format($"{_homeSheet.EventName}{nl}") : string.Empty;
                return $"{eventName}**{_homeSheet.EventDate}**{nl}__Format__ - {_homeSheet.FormatDescription}{nl}__Additional info:__{nl}{_homeSheet.Info}";
            }
            catch (Exception exc)
            {
                Console.WriteLine($"Exception in GetCurrentFormat: {exc.Message}");
                return $"Unable to determine format - most likely it hasn't been defined yet?";
            }
        }
        public virtual int GetCurrentPlayerCount()
        {
            return _sheetService.GetCurrentPlayerCount(_homeSheet, ChannelName);
        }

        public virtual async Task<List<UserInfo>> GetCurrentPlayerList()
        {
            return await Task.FromResult(_sheetService.GetCurrentPlayerList(_homeSheet, ChannelName));
        }

        public virtual string MarkPlayerDropped(EventUserInput eventUserInput)
        {
            if(_sheetService.MarkPlayerDropped(eventUserInput, _homeSheet))
            {
                return $"Player {eventUserInput.DiscordName} marked as DROPPED in the spreadsheet";
            }
            else
            {
                return $"Sorry, could not mark {eventUserInput.DiscordName} as DROPPED as they were not found in the spreadsheet for this event.";
            }
        }

        public virtual async Task<string> MarkPlayerHereAsync(EventUserInput eventUserInput)
        {
            if (_sheetService.MarkPlayerHere(eventUserInput, _homeSheet))
            {
                return await Task.FromResult($"Player {eventUserInput.DiscordName} marked as HERE in the spreadsheet");
            }
            else
            {
                return await Task.FromResult($"Sorry, could not mark {eventUserInput.DiscordName} as HERE as they were not found in the spreadsheet for this event.");
            }
        }

        public virtual string SubmitTeamLink(EventUserInput eventUserInput)
        {
            return _sheetService.SendLinkToGoogle(_homeSheet.SheetId, _homeSheet.SheetName, eventUserInput);
        }

        public virtual List<EventUserInput> GetTeamLists()
        {
            return _sheetService.GetTeams(_homeSheet);
        }
    }
}
