using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace ConsoleApplication5
{
    class GoogleTest
    {
        // If modifying these scopes, delete your previously saved credentials
        // at ~/.credentials/sheets.googleapis.com-dotnet-quickstart.json
        static string[] Scopes = { SheetsService.Scope.SpreadsheetsReadonly };
        static string ApplicationName = "DiceSheet";

        private static IList<IList<Object>> values;

        public static IList<IList<Object>> _values
        {
            get { return values; }
            set { _values = value; }
        }

        public static void Main(string sheet)
        {
           // IList<IList<Object>> values;
 
            //private IList<IList<Object>> values;

            UserCredential credential;

            using (var stream =
                new FileStream("client_secret.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = System.Environment.GetFolderPath(
                    System.Environment.SpecialFolder.Personal);
                credPath = Path.Combine(credPath, ".credentials/sheets.googleapis.com-dotnet-DiceSheet.json");

                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
            //    Console.WriteLine("Credential file saved to: " + credPath);
            }

            // Create Google Sheets API service.
            var service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            // Define request parameters.
            String spreadsheetId = "1jozQLrTrp89FfNEwoUEfKR_rTQbuJHN-35Exs3-tnu8";
            String range = sheet + "A2:M200";
            SpreadsheetsResource.ValuesResource.GetRequest request =
                    service.Spreadsheets.Values.Get(spreadsheetId, range);

            // Prints the names and majors of students in a sample spreadsheet:
            // https://docs.google.com/spreadsheets/d/1jozQLrTrp89FfNEwoUEfKR_rTQbuJHN-35Exs3-tnu8/edit
            ValueRange response = request.Execute();
            values = response.Values;
            //if (values != null && values.Count > 0)
            //{
            //    //Console.WriteLine("Card Name, Subtitle, Cost, Energy, Affinity, Effect, Stat Line, Image");
            //    foreach (var row in values)
            //    {

            //        if ((string)row[0] == name)
            //        {
            //            // Print columns A and E, which correspond to indices 0 and 4.
            //            Console.WriteLine("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}", row[1], row[2], row[3], row[4], row[5], row[6], row[7], row[8], row[9], row[10]);

            //        }
            //    }
        //}
        //    else
        //    {
        //        Console.WriteLine("No data found.");
        //    }

        }

            
    }
}
