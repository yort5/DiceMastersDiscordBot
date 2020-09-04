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
using ChallongeSharp.Models.ViewModels;
using ChallongeSharp.Helpers;
using System.Reflection;
using System.ComponentModel;

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


        public async Task<T> GetAsync<T>(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"/v1/{url}");

            HttpResponseMessage response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            return JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());
        }

        public async Task<T> PostAsync<T>(string url, FormUrlEncodedContent content)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"/v1/{url}") { Content = content };

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

        private static KeyValuePair<string, string> GetKvpRequestParam<T>(PropertyInfo property, T viewModel)
        {
            object propertyValue = property.GetValue(viewModel);
            AttributeCollection attributes = TypeDescriptor.GetProperties(viewModel)[property.Name].Attributes;
            string propertyDescription = ((ChallongeNameAttribute)attributes[typeof(ChallongeNameAttribute)]).Name;
            return new KeyValuePair<string, string>(propertyDescription, propertyValue.ToString());
        }


    }
}
