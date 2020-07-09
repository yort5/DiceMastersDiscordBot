using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiceMastersDiscordBot.Entities
{
    public class Refs
    {
        internal const string BOT_NAME = "Dice Masters Bot";
        internal const string EVENT_WDA = "weekly-dice-arena";
        internal const string EVENT_DICE_FIGHT = "dice-fight";
        internal const string EVENT_TOTM = "team-of-the-month";
        internal const string EVENT_ONEOFF = "dice-fight-xl";
        internal const string EVENT_CRGR_M1S = "monthly-one-shot";
        internal const string EVENT_CRGR_TTTD = "two-team-take-down";

        #region Private Properties
        internal static string WdaSheetName
        {
            get
            {
                TimeZoneInfo localTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                DateTime today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, localTimeZone);
                DateTime nextDate;
                if (today.DayOfWeek == DayOfWeek.Tuesday)
                {
                    nextDate = today;
                }
                else
                {
                    int diff = (7 + (today.DayOfWeek - DayOfWeek.Tuesday)) % 7;
                    nextDate = today.Date.AddDays(-1 * diff).AddDays(7).Date;
                }
                return $"{nextDate.Year}-{nextDate.ToString("MMMM")}-{nextDate.Day}";
            }
        }

        internal static string DiceFightSheetName
        {
            get
            {
                TimeZoneInfo localTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central Europe Standard Time");
                DateTime today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, localTimeZone);
                DateTime nextDate;
                if (today.DayOfWeek == DayOfWeek.Thursday)
                {
                    nextDate = today;
                }
                else
                {
                    int diff = (7 + (today.DayOfWeek - DayOfWeek.Thursday)) % 7;
                    nextDate = today.Date.AddDays(-1 * diff).AddDays(7).Date;
                }
                return $"{nextDate.Year}-{nextDate.ToString("MMMM")}-{nextDate.Day}";
            }
        }

        internal static string TotMSheetName
        {
            get
            {
                DateTime today = DateTime.Now;
                return $"{today.Year}-{today.ToString("MMMM")}";
            }
        }
        internal static string CRGRM1SSheetName
        {
            get
            {
                DateTime today = DateTime.Now;
                return $"{today.Year}-{today.ToString("MMMM")}";
            }
        }
        internal static string CRGRTTTDSheetName
        {
            get
            {
                DateTime today = DateTime.Now;
                return $"{today.Year}-{today.ToString("MMMM")}";
            }
        }
        public static string DMBotSubmitTeamHint
        {
            get
            {
                return $"Please send a Direct Message to the {Refs.BOT_NAME} with the format \"submit [event] [teambuilder link]\" where [event] is{Environment.NewLine}Weekly Dice Arena: WDA{Environment.NewLine}Dice Fight: DF{Environment.NewLine}Team of the Month: TOTM{Environment.NewLine}CRGR Monthly One Shot: M1S{Environment.NewLine}CRGR Two Team Take Down: TTTD";
            }
        }

        public static string DMBotCommandHelpString
        {
            get
            {
                StringBuilder helpString = new StringBuilder();
                var nl = Environment.NewLine;
                helpString.Append($"{BOT_NAME} currently supports the following commands:");
                helpString.Append($"{nl}WITHIN A CHANNEL:");
                helpString.Append($"{nl}    .format - returns the current format for that channel's event");
                helpString.Append($"{nl}    .submit <teambuilder link> - submits your team for the event. Your link will be immediately deleted so others can't see it.");
                helpString.Append($"{nl}    .list - lists the current people signed up for the event.");
                helpString.Append($"{nl}VIA DIRECT MESSAGE - you can also send the {BOT_NAME} a direct message");
                helpString.Append($"{nl}    .submit <event> <teambuilder link> - current supported events are wda (Weekly Dice Arena), df (Dice Fight), totm (Team of the Month), m1s (CRGR - Monthly One Shot, and tttd(CRGR - Two Team Take Down)");
                helpString.Append($"{nl}Example: `.submit wda http://tb.dicecoalition.com/blahblah`");
                helpString.Append($"{nl}If you have any problems or just general feedback, please DM Yort.");
                return helpString.ToString();
            }
        }

        public static string DiceFightAskForWin
        {
            get
            {
                StringBuilder askString = new StringBuilder();
                var nl = Environment.NewLine;
                askString.Append($"Dice Fight uses the WizKids Info Network to run brackets for the events.");
                askString.Append($"{nl}Your team was recorded, {BOT_NAME} does not have your WIN recorded for Dice Fight");
                askString.Append($"{nl}Please reply with your WIN login name using the following format");
                askString.Append($"{nl}    !win MyWinLogin");
                askString.Append($"{nl}If you do not have a WIN login, you can register for a free account at https://win.wizkids.com/");
                askString.Append($"{nl}If you have any questions, please contact @jacquesblondes");
                return askString.ToString();
            }
        }

        #endregion
    }
}
