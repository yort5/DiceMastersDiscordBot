using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ChallongeSharp.Clients;
using ChallongeSharp.Models.ChallongeModels;
using DiceMastersDiscordBot.Properties;

namespace DiceMastersDiscordBot.Entities
{
    public class ChallongeEvent
    {
        private readonly ILogger _logger;
        private readonly IAppSettings _settings;
        private readonly HttpClient _httpClient;
        private readonly string _challongeToken;
        private const string ChallongeUserName = "DiceMastersBot";

        public ChallongeEvent(HttpClient httpClient, ILoggerFactory loggerFactory, IAppSettings appSettings)
        {
            _logger = loggerFactory.CreateLogger<ChallongeEvent>();
            _settings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _httpClient = httpClient;
            var encodedToken = Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(ChallongeUserName + ":" + _settings.GetChallongeToken()));
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {encodedToken}");
        }

        public async Task<List<Participant>> GetAllParticipantsAsync(string tournamentName)
        {
            var requestUrl = $"tournaments/{tournamentName}/participants.json";

            var participants = await GetAsync<List<ParticipantResponse>>(requestUrl);
            return participants.Select(p => p.Participant).ToList();
        }


        public async Task<T> GetAsync<T>(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"/v1/{url}");

            HttpResponseMessage response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            return JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());
        }


    }
}
