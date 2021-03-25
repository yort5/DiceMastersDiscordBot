using System;
using System.Collections.Generic;
using System.Linq;
using DiceMastersDiscordBot.Entities;
using DiceMastersDiscordBot.Properties;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Microsoft.Extensions.Logging;
using TwitchLib.PubSub.Models.Responses.Messages;

namespace DiceMastersDiscordBot.Services
{
    public class YouTubeMonitorService
    {
        private readonly ILogger _logger;
        private readonly IAppSettings _settings;
        private readonly DMSheetService _sheetService;

        public YouTubeMonitorService(ILoggerFactory loggerFactory,
                            IAppSettings appSettings,
                            DMSheetService dMSheetService)
        {
            _logger = loggerFactory.CreateLogger<YouTubeMonitorService>();
            _settings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _sheetService = dMSheetService;
        }

        public List<YouTubeResponse> CheckForNewVideos()
        {
            List<YouTubeResponse> youTubeResponses = new List<YouTubeResponse>();

            try
            {

                string[] Scopes = { YouTubeService.Scope.YoutubeReadonly };
                string ApplicationName = _settings.GetBotName();
                string googleCredentialJson = _settings.GetGoogleToken();

                GoogleCredential credential;
                credential = GoogleCredential.FromJson(googleCredentialJson).CreateScoped(Scopes);

                var ytService = new YouTubeService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName,
                });


                var subs = _sheetService.LoadYouTubeInfo();

                foreach (var sub in subs)
                {
                    try
                    {

                        if (string.IsNullOrEmpty(sub.ChannelId))
                        {
                            Console.WriteLine($"Skipping Channel (no ID): {sub.ChannelName}");
                            continue;
                        }
                        Console.WriteLine($"Processing sub {sub.ChannelName}");

                        var channelRequest = ytService.Channels.List("contentDetails");
                        channelRequest.Id = sub.ChannelId;
                        var channelResponse = channelRequest.Execute();
                        var channelInfo = channelResponse.Items.FirstOrDefault();

                        var playlistRequest = ytService.PlaylistItems.List("snippet,contentDetails");
                        playlistRequest.PlaylistId = channelInfo.ContentDetails.RelatedPlaylists.Uploads;
                        playlistRequest.MaxResults = 10;
                        var playlistResponse = playlistRequest.Execute();
                        var diceMastersVideos = playlistResponse.Items.Where(p => p.Snippet.PublishedAt > sub.DateLastChecked).ToList();
                         foreach (var playlistItem in diceMastersVideos)
                        {
                            Console.WriteLine($"{playlistItem.Snippet.Title}");
                            var videoRequest = ytService.Videos.List("snippet");
                            videoRequest.Id = playlistItem.Snippet.ResourceId.VideoId;
                            var videoResponse = videoRequest.Execute();
                            if (videoResponse.Items.FirstOrDefault().Snippet.Tags is null) continue;
                            var taggedItems = videoResponse.Items.FirstOrDefault().Snippet.Tags.Where(t => t.ToLower() == "dicemasters" || t.ToLower().Contains("dice masters")).ToList();
                            if (taggedItems.Any())
                            {
                                Console.WriteLine("YES");
                                YouTubeResponse ytr = new YouTubeResponse()
                                {
                                    ChannelName = sub.ChannelName,
                                    VideoId = playlistItem.Snippet.ResourceId.VideoId,
                                    VideoTitle = playlistItem.Snippet.Title,
                                    VideoDescription = playlistItem.Snippet.Description
                                };
                                youTubeResponses.Add(ytr);
                            }
                            else
                            {
                                Console.WriteLine("NO");
                            }
                        }
                    }
                    catch (Exception exc)
                    {
                        _logger.LogError($"Exception processing sub: {exc.Message}");
                    }
                }

                _sheetService.UpdateYouTubeInfo();

            }
            catch (Exception exc)
            {
                _logger.LogError(exc.Message);
            }

            return youTubeResponses;
        }
    }
}
