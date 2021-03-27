using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using DiceMastersDiscordBot.Properties;
using System.Reflection;
using System.ComponentModel;
using ChallongeSharp.Models.ViewModels.Types;
using ChallongeSharp.Models.ViewModels;
using ChallongeSharp.Helpers;
using ChallongeSharp.Models.ChallongeModels;

namespace DiceMastersDiscordBot.Entities
{
    public class ChallongeEvent
    {
        // Apologies to the one who made the ChallongeSharp package - I know I'm not using it right, but I'm trying to squeeze
        // getting this to work in-between life and other things, so I'm just hacking it together
        private readonly ILogger _logger;
        private readonly IAppSettings _settings;
        private readonly HttpClient _httpClient;
        private const string ChallongeUserName = "DiceMastersBot";

        public ChallongeEvent(HttpClient httpClient, ILoggerFactory loggerFactory, IAppSettings appSettings)
        {
            _logger = loggerFactory.CreateLogger<ChallongeEvent>();
            _settings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _httpClient = httpClient;
            var encodedToken = Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(ChallongeUserName + ":" + _settings.GetChallongeToken()));
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {encodedToken}");
        }

        public async Task<Participant> GetParticipantAsync(string tournamentName, string participantId)
        {
            var requestUrl = $"tournaments/{tournamentName}/participants/{participantId}.json";

            var participant = await GetAsync<ParticipantResponse>(requestUrl);
            return participant.Participant;
        }


        public async Task<List<Participant>> GetAllParticipantsAsync(string tournamentName)
        {
            var requestUrl = $"tournaments/{tournamentName}/participants.json";

            var participants = await GetAsync<List<ParticipantResponse>>(requestUrl);
            return participants.Select(p => p.Participant).ToList();
        }

        public async Task<Participant> AddParticipantAsync(ParticipantVm participantVm, string tournamentName)
        {
            var requestUrl = $"tournaments/{tournamentName}/participants.json";
            var requestContent = GetUrlEncodedContent(participantVm);

            var participant = await PostAsync<ParticipantResponse>(requestUrl, requestContent);
            return participant.Participant;
        }

        public async Task<Participant> CheckInParticipantAsync(string participantId, string tournamentName)
        {
            var requestUrl = $"tournaments/{tournamentName}/participants/{participantId}/check_in.json";

            var participant = await PostAsync<ParticipantResponse>(requestUrl);
            return participant.Participant;
        }

        public async Task<List<ChallongeSharp.Models.ChallongeModels.Match>> GetAllMatchesAsync(string tournamentName, TournamentState state = null, long? participantId = null)
        {
            var requestUrl = $"tournaments/{tournamentName}/matches.json";
            var options = new MatchOptions
            {
                State = state,
                ParticipantId = participantId
            };

            var matches = await GetAsync<List<ChallongeSharp.Models.ChallongeModels.MatchResponse>>(requestUrl, options);
            return matches.Select(m => m.Match).ToList();
        }

        public async Task<Match> GetMatchAsync(string tournamentName, long matchId,
            bool includeAttachments = false)
        {
            var requestUrl = $"tournaments/{tournamentName}/matches/{matchId}.json";
            var options = new MatchOptions
            {
                IncludeAttachments = includeAttachments
            };

            var match = await GetAsync<MatchResponse>(requestUrl, options);
            return match.Match;
        }


        public async Task<Match> UpdateMatchAsync(string tournamentName, long matchId, int player1Score,
            int player2Score, int? player1Votes = null, int? player2Votes = null)
        {
            var match = await GetMatchAsync(tournamentName, matchId);

            var requestUrl = $"tournaments/{tournamentName}/matches/{matchId}.json";
            var options = new MatchOptions
            {
                Player1Score = player1Score,
                Player2Score = player2Score,
                Player1Votes = player1Votes,
                Player2Votes = player2Votes,
                Player1Id = match.Player1Id,
                Player2Id = match.Player2Id
            };

            var updatedMatch = await PutAsync<MatchResponse>(requestUrl, options);
            return updatedMatch.Match;
        }

        public async Task<List<Tournament>> GetTournamentAsync(string name, bool includeParticipants = false,
            bool includeMatches = false)
        {
            var options = new TournamentOptions
            {
                IncludeParticipants = includeParticipants,
                IncludeMatches = includeMatches
            };

            var tournaments = await GetAsync<List<TournamentResponse>>($"tournaments/{name}.json", options);
            return tournaments.Select(t => t.Tournament).ToList();
        }








        public async Task<T> GetAsync<T>(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"/v1/{url}");

            HttpResponseMessage response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            return JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());
        }

        public async Task<T> GetAsync<T>(string url, MatchOptions options)
        {
            if (options != null)
            {
                var requestParams = ToChallongeRequestParams(options);
                url += $"?{requestParams}";
            }

            var request = new HttpRequestMessage(HttpMethod.Get, $"/v1/{url}");

            HttpResponseMessage response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            return JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());
        }

        public async Task<T> GetAsync<T>(string url, TournamentOptions options)
        {
            if (options != null)
            {
                var requestParams = ToChallongeRequestParams(options);
                url += $"?{requestParams}";
            }

            var request = new HttpRequestMessage(HttpMethod.Get, $"/v1/{url}");

            HttpResponseMessage response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            string body = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<T>(body);
        }


        public async Task<T> PostAsync<T>(string url, FormUrlEncodedContent content = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"/v1/{url}") { Content = content };

            HttpResponseMessage response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            string body = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<T>(body);
        }

        public async Task<T> PutAsync<T>(string url, MatchOptions content)
        {
            FormUrlEncodedContent formUrlEncodedContent = GetUrlEncodedContent(content);
            var request = new HttpRequestMessage(HttpMethod.Put, $"/v1/{url}") { Content = formUrlEncodedContent };

            HttpResponseMessage response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            return JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());
        }






        private FormUrlEncodedContent GetUrlEncodedContent(ParticipantVm model)
        {
            var properties = model.GetType().GetProperties();
            var requestContent = properties
                .Where(prop => prop.GetValue(model) != null && Attribute.IsDefined(prop, typeof(ChallongeNameAttribute)))
                .Select(prop => GetKvpRequestParam(prop, model)).ToList();

            return new FormUrlEncodedContent(requestContent);
        }

        private FormUrlEncodedContent GetUrlEncodedContent(MatchOptions options)
        {
            var properties = options.GetType().GetProperties();
            var requestContent = properties
                .Where(prop => prop.GetValue(options) != null && Attribute.IsDefined(prop, typeof(ChallongeNameAttribute)))
                .Select(prop => GetKvpRequestParam(prop, options)).ToList();

            return new FormUrlEncodedContent(requestContent);
        }

        private KeyValuePair<string, string> GetKvpRequestParam<T>(PropertyInfo property, T viewModel)
        {
            object propertyValue = property.GetValue(viewModel);
            AttributeCollection attributes = TypeDescriptor.GetProperties(viewModel)[property.Name].Attributes;
            string propertyDescription = ((ChallongeNameAttribute)attributes[typeof(ChallongeNameAttribute)]).Name;
            return new KeyValuePair<string, string>(propertyDescription, propertyValue.ToString());
        }

        private string ToChallongeRequestParams(MatchOptions options)
        {
            var properties = options.GetType().GetProperties();
            var requestParams = properties
                .Where(prop =>
                    prop.GetValue(options) != null && Attribute.IsDefined(prop, typeof(ChallongeNameAttribute)))
                .Select(prop => GetStringRequestParam(prop, options)).ToList();

            return $"{string.Join("&", requestParams)}";
        }

        private string ToChallongeRequestParams(TournamentOptions options)
        {
            var properties = options.GetType().GetProperties();
            var requestParams = properties
                .Where(prop =>
                    prop.GetValue(options) != null && Attribute.IsDefined(prop, typeof(ChallongeNameAttribute)))
                .Select(prop => GetStringRequestParam(prop, options)).ToList();

            return $"{string.Join("&", requestParams)}";
        }


        private string GetStringRequestParam<T>(PropertyInfo property, T viewModel)
        {
            object propertyValue = property.GetValue(viewModel);
            AttributeCollection attributes = TypeDescriptor.GetProperties(viewModel)[property.Name].Attributes;
            string propertyDescription = ((ChallongeNameAttribute)attributes[typeof(ChallongeNameAttribute)]).Name;
            if (propertyValue.GetType().Name.Equals(typeof(DateTime).Name))
                propertyValue = ((DateTime)propertyValue).ToString("yyyy-MM-dd");

            return $"{propertyDescription}={propertyValue}";
        }

    }
}
