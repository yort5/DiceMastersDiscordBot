using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Google.Apis.Sheets.v4.Data;
using System.Text;
using System.IO;
using System.Threading;
using Google.Apis.Util.Store;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;
using DiceMastersDiscordBot.Properties;
using DiceMastersDiscordBot.Entities;

namespace DiceMastersDiscordBot.Services
{
    public class TCCSheetService
    {
        private const string ReferralSheetName = "Referrals";

        private readonly ILogger _logger;
        private readonly IAppSettings _settings;
        private readonly SheetsService _sheetService;

        private const string _LastCheckedString = "Last Checked";
        private const string _LastRecordedString = "Last Recorded";

        public TCCSheetService(ILoggerFactory loggerFactory, IAppSettings appSettings)
        {
            _logger = loggerFactory.CreateLogger<TCCSheetService>(); ;
            _settings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));

            _sheetService = AuthorizeGoogleSheets();
        }

        private SheetsService AuthorizeGoogleSheets()
        {
            try
            {
                string[] Scopes = { SheetsService.Scope.Spreadsheets };
                string ApplicationName = _settings.GetBotName();
                string googleCredentialJson = _settings.GetGoogleToken();

                GoogleCredential credential;
                credential = GoogleCredential.FromJson(googleCredentialJson).CreateScoped(Scopes);
                //Reading Credentials File...
                //using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
                //{
                //    credential = GoogleCredential.FromStream(stream)
                //        .CreateScoped(Scopes);
                //}


                var service = new SheetsService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName,
                });

                return service;
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
                return null;
            }
        }


        internal async Task<string> AddReferral(ReferralInfo referralInfo)
        {
            try
            {
                var range = $"{ReferralSheetName}!{_settings.GetColumnSpan()}";

                // first check to see if this person already has a submission
                var checkExistingRequest = _sheetService.Spreadsheets.Values.Get(_settings.GetCrimeSheetId(), range);
                var existingRecords = await checkExistingRequest.ExecuteAsync();
                bool existingEntryFound = false;
                if (existingRecords.Values != null)
                {
                    foreach (var record in existingRecords.Values)
                    {
                        if (record.Contains(referralInfo.ReferralDiscordName))
                        {
                            if (record.Count >= 2 && record[1].ToString().Equals(referralInfo.ReferralBrand, StringComparison.CurrentCultureIgnoreCase))
                            {
                                var index = existingRecords.Values.IndexOf(record);
                                range = $"{ReferralSheetName}!A{index + 1}";
                                existingEntryFound = true;
                                break;
                            }
                        }
                    }
                }

                var oblist = new List<object>()
                    { referralInfo.ReferralDiscordName, referralInfo.ReferralBrand, referralInfo.ReferralCode, referralInfo.ReferralLink };
                var valueRange = new ValueRange();
                valueRange.Values = new List<IList<object>> { oblist };

                if (existingEntryFound)
                {
                    // Performing Update Operation...
                    var updateRequest = _sheetService.Spreadsheets.Values.Update(valueRange, _settings.GetCrimeSheetId(), range);
                    updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                    var appendReponse = await updateRequest.ExecuteAsync();
                    return $"Thanks {referralInfo.ReferralDiscordName}, your referral for {referralInfo.ReferralBrand} was updated!";
                }
                else
                {
                    // Append the above record...
                    var appendRequest = _sheetService.Spreadsheets.Values.Append(valueRange, _settings.GetCrimeSheetId(), range);
                    appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
                    var appendReponse = await appendRequest.ExecuteAsync();
                    return $"Thanks {referralInfo.ReferralDiscordName}, your referral for {referralInfo.ReferralBrand} was added!";
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
                return "There was an error trying to add your referral";
            }
        }

        internal async Task<List<ReferralInfo>> GetAllReferrals()
        {
            List<ReferralInfo> referralList = new List<ReferralInfo>();
            try
            {
                // Define request parameters.
                var range = $"{ReferralSheetName}!{_settings.GetColumnSpan()}";

                // load the data
                var sheetRequest = _sheetService.Spreadsheets.Values.Get(_settings.GetCrimeSheetId(), range);
                var sheetResponse = await sheetRequest.ExecuteAsync();
                var values = sheetResponse.Values;


                for (int i = 0; i < values.Count; i++)
                {
                    try
                    {
                        ReferralInfo ri = new ReferralInfo();
                        ri.ReferralDiscordName = values[i][0].ToString();
                        ri.ReferralBrand = values[i][1].ToString();
                        ri.ReferralCode = (values[i].Count >= 3) ? values[i][2].ToString() : string.Empty;
                        ri.ReferralLink = (values[i].Count >= 4) ? values[i][3].ToString() : string.Empty;
                        referralList.Add(ri);
                    }
                    catch (Exception exc)
                    {
                        _logger.LogError($"Exception loading referral #{i} ({exc.Message})");
                    }
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
            }
            return referralList;
        }

    }
}
