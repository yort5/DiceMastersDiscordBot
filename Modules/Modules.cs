using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Net;
using System.IO;

namespace ConsoleApplication5.modules.Public
{
    public class Modules : ModuleBase
    {
        int sheets;
        string number;
        string Tempname = "";
        string Tempnick = "";
        string Tempset = "";
        static string CName = "";
        string CSub = "";
        string CCost = "";
        string CEnergy = "";
        string CRarity = "";
        string CAffiliation = "";
        string CEffect1 = "";
        string CEffect = "";
        string CStat = "";
        string CImage = "";
        string CFImage = "";
        string CCode = "";
        string CGlobal = "";
        string holder = "";
        bool global = false;

        List<string> valid = new List<string>(); 
        Color rarity = new Color(0, 255, 0);
        GoogleTest go = new GoogleTest();
        static string[] sets = new string[]
        {
            "AI","AOU", "ASM", "AVX", "BAT", "BFF", "BFU", "CW", "DEF", "DOOM" ,"DP", "DRS", "FUS", "GAF", "GOTG", "HHS", "HQ" , "IMW", "JL", "JLL", "JUS", "KI", "MYST" , "ORK" ,"SMC", "SW" ,"SWW", "THOR", "TMNT", "TOA", "UXM", "WF", "WOL", "XFC" ,"YGO", "PROMO"
        };
        static string[] setname = new string[]
        {
            "Avengers Infinity Campaign Box","Age of Ultron", "The Amazing Spider-Man", "Avengers vs. X-Men", "Batman", "Battle for Faerûn", "Battle for Ultramar", "Civil War", "Defenders Team Pack", "Doom Patrol Team Pack" ,"Deadpool", "Doctor Strange Team Pack", "Faerûn Under Seige",
            "Green Arrow and The Flash", "Guardians of the Galaxy", "Heroes in a Half Shell", "Harley Quinn Team Pack", "Iron Man and War Machine Starter Set", "Justice Legue","Just Like Lighting Team Pack", "Justice Campaign Box" ,"Kree Invasion Team Pack", "Mystics Team Pack",
            "Orks - WAAAGH! Team Pack", "Spider-Man Maximum Carnage Team Pack", "Space Wolves - Sons of Russ Team Pack" ,"Superman and Wonder Woman Starter Set","The Mighty Thor", "Tomb of Annihilation", "Teenage Mutant Ninja Turtles", "Uncanny X-Men", "World's Finest", "War of Light", "X-men First Class",
            "Yu-Gi-Oh : Series 1","Promo Cards"
        };
        static string[] Affiliations = new string[]
        {
            "Avengers", "Fantasic 4", "Phoenix Force", "X-Men", "Guardians of the Galaxy", "S.H.I.E.L.D.", "Zombies", "Sinister Six", "Spider-Friends", "New Warriors", "Thunderbolts", "Mystic", "Deadpool Affiliation", "Inhumans", "Stark Industries", "Defenders", "Exiles", "Brotherhood of Mutants",
            "Justice League", "Justice Society of America", "Legion of Doom", "Black Lanterns", "Blue Lanterns", "Green Lanterns", "Indigo Tribe", "Orange Lanterns", "Red Lanterns", "Sinestro Corps", "Star Sapphore Corps", "Teen Titans", "Batman Family", "Crime Syndicate", "Team Superman", "Suicide Squad", "Team Arrow", "New Gods", "Doom Patrol",
            "Emerald Enclave", "The Harpers", "Lords Alliance", "Order of the Gauntlet", "The Zhentarim", "Monster", "Evil", "Good", "Neutral", "Equip",
            "Egyptian God", "Thousand Dragon Fusion",
            "Teenage Mutant Ninja Turtle",
            "Orks", "Chaos", "Imperium", "Death Guard", "Space Wolves", "Ultramarine"
        };
        static string[] AffiliationEmoji = new string[]
        {
            "<:Avengers:336237385547513856>", "<:FantasticFour:336237395491946498>", "<:PhoenixForce:336237500676964352>", "<:XMen:336237518758346752>", "<:Guardians:336237604552835075>", "<:SHIELD:336237704469807104>", "<:zombies:336237931184259075>", "<:SinisterSix:336237944224612361>", "<:SpiderFriends:336237954533949441>", "<:NewWarriors:336238259552387075>", "<:Thunderbolt:366684577248313346>", "<:Mystic:336237211269988353>", "<:DeadpoolAffiliation:336238283573035008>", "<:Inhumans:336238294280962059>", "<:Stark:336238784540704768>", "<:DefendersLogo:336238817231110152>", "<:ExilesLogoColor:367044623199109122>", "<:BrotherhoodLogoColor:367044595537805314>",
            "<:justiceleague:336233343500681219>", "<:JusticeSociety:336236161896808449>", "<:LegionOfDoom:336237139605979138>", "<:blacklantern:336233903511830529>", "<:bluelantern:336233980766584837>", "<:greenlantern:336234622197301258>", "<:indigocorps:336234608448503819>", "<:orangelantern:336235128387010560>", "<:redlantern:336234214196248577>", "<:sinestrocorps:336234203954020374>", "<:starsapphire:336234592904151050>", "<:teentitans:336233142450913291>", "<:batmanfamily:336232448390201345>", "<:crimesyndicate:336233444709236740>", "<:teamsuperman:336233039950774275>", "<:SuicideSquad:336236626235883520>", "<:teamarrow:336236483633741835>", "<:newgods:336232897507754014>",
            "<:emeraldenclavesmall:366897706410704897>", "<:theharperssmall:366914382984118272>", "<:lordsalliancesmall:366915672602574849>", "<:orderofthegauntletsmall:366914477171408897>", "<:zhentarimsmall:366914513376903169>", "<:monstersmall:366914405872435201>", "<:evilsmall:367045943046045708>", "<:goodsmall:367045971877560320>", "<:neutralsmall:367046001552261131>", "<:equipxsmall:367295956963557397>",
            "<:EgyptianGodSmall:366914426391101450>", "<:ThousandDragonSmall:366914543420440576>",
            "<:TNMT:336238759194525716>",
            "<:orks:481102530781249536>", "<:chaos:481102402309849099>", "<:imperium:481102506747756554>", "<:deathguard:481102473277210625>", "<:spacewolves:481102551593385994>", "<:ultramarines:481102579187712004>"

        };
        static string[] keywords = new string[]
        {
            "Adventurer", "Aftershock", "Ally", "Amplify", "Anti-Breath Weapon", "Attune", "Awaken", "Back for More", "Boomerang", "Breath Weapon", "Call Out", "Cleave", "Crossover", "Crosspulse", "Deadly", "Effect", "Energy Drain", "Enlistment"
            ,"Equip", "Experience", "Fabricate", "Fast", "Flip", "Frag", "Fusion", "Gadgeteer", "Heroic", "Immortal", "Impulse", "Iron Will", "Infiltrate", "Intimidate", "Overcrush", "Range", "Regenerate", "Resistance", "Retaliation", "Ritual",
            "Strike" ,"Suit Up", "Swarm", "Synergy", "Teamwatch", "Teamwork", "Trap", "Trigger", "Turtle Power", "Underdog", "Vengeance"
        };
        static string[] Rarities = new string[]
        {
            "c", "ch", "uc", "u", "r", "sr", "s", "p", "common", "uncommon", "rare", "super", "super rare", "chase", "promo"
        };

        
        [Command("sets")]
        public async Task setnames()
        {
            string s = "Sets available in the bot :  ```";
            int check = setname.Length;

            for (int i = 0; i < sets.Length; i++)
            {
                s += String.Format("{0, -4 }", sets[i]);
                s += " | ";
                s += String.Format("{0,-30}", setname[i]);
                s += "\n";
            }

            s += "```";
            await Context.User.SendMessageAsync(s);
        }

        [Command("setshere")]
        public async Task setnameschannel()
        {
            string s = "Sets available in the bot :  ```";

            for (int i = 0; i < sets.Length; i++)
            {
                s += String.Format("{0, -4 }", sets[i]);
                s += " | ";
                s += String.Format("{0,-30}", setname[i]);
                s += "\n";

            }

            s += "```";
            await Context.Channel.SendMessageAsync(s);
        }

        [Command("ping")]
        public async Task ping()
        {
            await Context.Channel.SendMessageAsync("```prolog" + "\n#test \ntest" + "```");
        }

        [Command("help")]
        public async Task DM()
        {
            
            string s = "Okay, so here is a list of my functions: \n\n" +
                ">card \n" +
                ">global \n" +
                ">nick \n" +
                ">keyword \n" +
                ">myteam \n" +
                ">myteamgif \n" +
                ">promo \n" +
                ">fullpic \n" +
                ">fulltext \n" +
                ">sets \n" +
                ">setshere \n" +
                ">super \n" +
                ">about \n\n\nIf you want to see an example please type >help and then the function name for a more detailed explanation.";
            await ReplyAsync(s);

        }

        [Command("help")]
        public async Task DMHelpDetails(string function)
        {
            string s;

            if ((function == ">card") || (function == "card"))
            {
                s = "There are several versions of this command. Here is how to use them. We will look at Rip Hunter from the BAT set for demonstration" + "```" +
                 ">card (cardset and number combined)\n \t\t Example: >card BAT030 <= This returns Rip Hunter: Navigating the Sands of Time.\n\n" +
                 ">card (cardset) (card name)\n \t\t Example: >card BAT \"Rip Hunter\" <= This returns all Rip Hunter cards in the set.\n\n" +
                 ">card (cardset) (card name) (rarity)\n \t\t Example: >card BAT \"Rip Hunter\" R <= This returns the Rare Rip Hunter.```" +
                 "\n\nPlease be aware that if the card name has more than 1 word, it will need to be encased in \"\". Example \"The Bifrost\" ";
            }
            else if ((function == ">global") || (function == "global"))
            {
                s = "To use this function you just need to include the set and the character/card name. Here is a demonstration using THOR and Samantha Wilson" + "```" +
                    ">global (card name)\n \t\t>global \"Samantha Wilson\" <= This will return all the global abilites found on all the rarites. If there are 2 or more different ones then they will be printed seperately.```" +
                 "\n\nPlease be aware that if the card name has more than 1 word, it will need to be encased in \"\". Example \"The Bifrost\" ";
            }
            else if ((function == ">nick") || (function == "nick"))
            {
                s = "There are several versions of this command. Here is how to use them. We will look at Lantern Ring from the WOL set for demonstration, which is called \"Ring\" for short. (As in a Bolt Ring team)" + "```" +
                ">card (nickname)\n \t\t Example: >nick Ring <= This returns the Rare Lantern Ring.\n\n" +
                ">card (cardset) (card name)\n \t\t Example: >card Wol Ring <= This returns Rare Lantern Ring.```";
            }
            else if ((function == ">promo") || (function == "promo"))
            {
                s = "There are several versions of this command. Here is how to use them. For this demonstration we will look at Thor: The Mighty from AVX" + "```" +
                ">card (cardset and number combined)\n \t\t Example: >promo AVXOP#007 <= This returns the promo Thor card.\n\n" +
                ">card (card name)\n \t\t Example: >promo Thor <= This returns all Thor promos.```" +
                 "\n\nPlease be aware that if the card name has more than 1 word, it will need to be encased in \"\". Example \"Wonder Woman\" ";
            }
            else if ((function == ">keyword") || (function == "keyword"))
            {
                s = "To use this function, you need only state the keyword you are after. For this demonstration we will look at Breath Weapon" + "```" +
                ">keyword (keyword you are after)\n \t\t Example: >keyword \"Breath Weapon\" <= This returns the text from the wizkids keywords page, in regards to the ruling.```\n\n" +
                 "\n\nPlease be aware that if the card name has more than 1 word, it will need to be encased in \"\". Example \"Call Out\" \n\n" +
                 "This command is known to be broken and may not work, it is being worked on.";
            }
            else if ((function == ">myteam") || (function == "myteam"))
            {
                s = "To use this function, you just need to paste a team link from one of these 3 sites. http://tb.dicecoalition.com/, http://dm.frankenstein.com/ or http://dm.retrobox.eu/ " + "```" +
                ">myteam (url)\n \t\t Example: >myteam http://tb.dicecoalition.com/?view&cards=1x16smc <= I will build a team image with these cards, inserting blanks if needed.```";
            }
            else if ((function == ">myteam") || (function == "myteam"))
            {
                s = "To use this function, you just need to state if you want to have dice quantities printed and paste a team link from one of these 3 sites. http://tb.dicecoalition.com/, http://dm.frankenstein.com/ or http://dm.retrobox.eu/ " + "```" +
                ">myteam (url)\n \t\t Example: >myteam dice http://tb.dicecoalition.com/?view&cards=1x16smc <= I will build a team gif with these cards, printing the dice quantities on them and inserting blanks if needed." +
                "\nExample: >myteam nodice http://tb.dicecoalition.com/?view&cards=1x16smc <= I will build a team gif with these cards, inserting blanks if needed.```";
            }
            else if ((function == ">fullpic") || (function == "fullpic"))
            {
                s = "This function will pm you all the cards in a set, with their images if available." + "```" +
                ">fullpic (set)\n \t\t Example: >fullpic THOR <= This sends all the THOR cards to you in private to avoid spam.```";
            }
            else if ((function == ">fulltext") || (function == "fulltext"))
            {
                s = "This function will pm you all the cards in a set, without their images." + "```" +
                ">fullpic (set)\n \t\t Example: >fullpic THOR <= This sends all the THOR cards to you in private to avoid spam.```";
            }
            else if ((function == ">sets") || (function == "sets"))
            {
                s = "This function will pm you all the sets available in the bot." + "```" +
                "Example: >sets <= This sends all the sets and their abbreviations to you in private.```";
            }
            else if ((function == ">setshere") || (function == "setshere"))
            {
                s = "This function will print all the sets available in the bot in the active channel." + "```" +
                "Example: >setshere <= This prints all the sets and their abbreviations in the active channel.```";
            }
            else if ((function == ">super") || (function == "super"))
            {
                s = "This function will print out all the super rare cards in a set, with their images if available." + "```" +
                ">super (set)\n \t\t Example: >super THOR <= This prints all the Super Rare THOR cards to the active channe.```";
            }
            else if ((function == ">about") || (function == "about"))
            {
                s = "To use this command, just type >about. Its pretty simple!";
            }
            else
            {
                s = "I cant find that function!";
            }

            await ReplyAsync(s);
        }

        [Command("about")]
        public async Task About()
        {
            string s = "This bot was made in C# using the .Net framework and api's from Google and Discord. The programmer is Stevomuck, who can be found on the server or on facebook as Steven McEwan." +
                "\nIf you find any errors or have a function idea, please let him know! (also if you want to tip him, he will be more than happy to send you paypal information!)";
            await ReplyAsync(s);
        }


        [Command("card"), Summary("Echos a message.")]
        [Alias("carte")]
        public async Task say([Remainder, Summary("The text to echo")] string echo)
        {

            echo = echo.Replace('“', '"');
            echo = echo.Replace('”', '"');

            var parameter = Regex.Matches(echo, @"[\""].+?[\""]|[^ ]+")
                            .Cast<Match>()
                            .Select(m => m.Value)
                            .ToList();

            for (int i = 0; i < parameter.Count(); i++)
            {
                if(parameter[i].Contains("\""))
                {
                    parameter[i] = parameter[i].Replace("\"", "");
                }
            }

            if(parameter.Count == 3)
            {
                await FindCard(parameter[0], parameter[1], parameter[2]);
            }
            else if (parameter.Count == 1)
            {
                await DisplayCards(parameter[0]);
            }
            else if (parameter.Count == 2)
            {
                await FindCard(parameter[0], parameter[1]);
            }

        }

        public async Task DisplayCards(string value)
        {
            bool found = false;
            string card = value;

            value = value.ToUpper();

            string[] promonames = new string[]
            {
                "AVXOP", "BFFOP", "BFFPR", "D2016","DC2017", "D2017", "JLOP", "M2015", "M2016", "M2017", "UXMOP", "UXMOP2", "WKO16D", "WKO16M"
            };

            bool contains = false;

            for(int i = 0; i < promonames.Length; i++)
            {
                if (value.Contains(promonames[i]))
                {
                    contains = true;
                }
            }

            string sheet = "";
            if(contains != true)
            {
                sheet = Regex.Replace(value, @"[\d-]", string.Empty);
            }
            else
            {
                sheet = "PROMO";
            }
            


            card = card.ToUpper();
            if (!sets.Contains(sheet.ToUpper()))
            {
                await Error("set", sheet);
                found = true;
            }
            else
            {

                if (card == "random")
                {
                    Random rnd = new Random();
                    sheets = rnd.Next(0, sets.Length);

                    sheet = await randomSheet(sheets);
                    number = randomNumber(sheet);

                    card = sheet.ToUpper() + number;
                }

                string sheetref = sheet + "!";
                GoogleTest.Main(sheetref);
                var test = GoogleTest._values;

                if(contains == true)
                {
                    if (!Char.IsLetter(card.FirstOrDefault()))
                    {
                        
                        sheet = Regex.Replace(value, @"^[\d-]*\s*", string.Empty);
                        string cardtemp = card.Replace(sheet, "");
                        int digits = cardtemp.Count();
                        for (int j = digits; j < 3; j++)
                        {
                            cardtemp = "0" + cardtemp;
                        }

                        card = sheet + "#" + cardtemp;

                    }
                    else
                    {
                        if (card.Contains("#"))
                        {
                            string cardno = card.Substring(card.IndexOf('#') + 1);
                            string cardset = card.Replace(cardno, "");
                            int digits = cardno.Count();
                            for (int j = digits; j < 3; j++)
                            {
                                cardno = "0" + cardno;
                            }

                            card = cardset + cardno;
                        }
                        else
                        {
                            string code = card.Substring(card.Length - 3);
                            string set = card.Replace(code, "");
                            card = set + "#" + code;
                        }
                    }
                }

                foreach (var row in test)
                {
                    //Check for named cell
                    if ((string)row[0] == card)
                    {
                        
                        //Assign the cells to varibales
                        CCode = (string)row[0];
                        CName = (string)row[1];
                        CSub = (string)row[2];
                        CCost = (string)row[3];
                        CEnergy = (string)row[4];
                        CRarity = (string)row[5];
                        CAffiliation = (string)row[6];
                        CEffect1 = (string)row[7];
                        CEffect = CEffect1;
                        CStat = (string)row[8];
                        CImage = (string)row[9];
                        CFImage = (string)row[10];
                        found = true;
                    }
                }

                if (CEnergy == "x")
                {
                    CEnergy = " ";
                }

                if (CAffiliation == "x")
                {
                    CAffiliation = " ";
                }
                await contextbuilder(2);
            }

            if (found == false)
            {
                await Context.Channel.SendFileAsync("hall.png");
                await Context.Channel.SendMessageAsync("Oh dear," + Context.User.Mention + "\n\n I don't appear to have that one recoreded.");
            }
        }

        public async Task FindCard(string sheet, string name)
        {
            bool found = false;
            sheet = sheet.ToUpper();

            if (sheet == "TMT")
                sheet = "THOR";


            if (!sets.Contains(sheet.ToUpper()))
            {
                await Error("set", sheet);
                found = true;
            }
            else
            {
                string sheetref = sheet + "!";
                GoogleTest.Main(sheetref);
                var test = GoogleTest._values;

                name = name.ToLower();

                foreach (var row in test)
                {
                    Tempname = (string)row[1];
                    Tempname = Tempname.ToLower();

                    holder = (string)row[1];
                    if ((!valid.Contains(holder)) && (!valid.Contains("\"" + holder + "\"")))
                    {
                        if (holder.Contains(' '))
                        {
                            holder = "\"" + holder + "\"";

                        }
                        valid.Add(holder);
                    }

                    //Check for named cell
                    if (Tempname == name)
                    {
               
                        //Assign the cells to varibales
                        CCode = (string)row[0];
                        CName = (string)row[1];
                        CSub = (string)row[2];
                        CCost = (string)row[3];
                        CEnergy = (string)row[4];
                        CRarity = (string)row[5];
                        CAffiliation = (string)row[6];
                        CEffect = (string)row[7];
                        CStat = (string)row[8];
                        CImage = (string)row[9];
                        CFImage = (string)row[10];

                        found = true;

                        if ((CSub != "Basic Action Card") || (CRarity != "Promo"))
                        {
                            await contextbuilder(1);
                        }
                        else
                        {
                            await contextbuilder(2);
                        }
                    }
                }
            }
            if (found == false)
            {
                await Error("card", name);
            }
        }

        public async Task FindCard(string sheet, string name, string rarity)
        {
            bool found = false;
            sheet = sheet.ToUpper();

            if (sheet == "TMT")
                sheet = "THOR";

            if (!sets.Contains(sheet.ToUpper()) || (!Rarities.Contains(rarity.ToLower())))
            {
                if(!sets.Contains(sheet.ToUpper()))
                {
                    await Error("set", sheet);
                    found = true;
                }
                if(!Rarities.Contains(rarity.ToLower()))
                {
                    await Error("rarity", "blank");
                    found = true;
                }
            }
            else
            {
                string sheetref = sheet + "!";
                GoogleTest.Main(sheetref);
                var test = GoogleTest._values;

                rarity = rarity.ToLower();

                if ((rarity == "c") || (rarity == "common"))
                {
                    rarity = "Common";
                }
                else if ((rarity == "uc") || (rarity == "u") || (rarity == "uncommon")) 
                {
                    rarity = "Uncommon";
                }
                else if ((rarity == "r") || (rarity == "rare"))
                {
                    rarity = "Rare";
                }
                else if ((rarity == "sr") || (rarity == "s") || (rarity == "super") || (rarity == "super rare"))
                {
                    rarity = "Super";
                }
                else if ((rarity == "p") || (rarity == "promo"))
                {
                    rarity = "Promo";
                }
                else if ((rarity == "ch") || (rarity == "chase"))
                {
                    rarity = "Chase";
                }
                else
                {
                   
                }

                name = name.ToLower();
                int count = 0;



                foreach (var row in test)
                {
                    Tempname = (string)row[1];
                    Tempname = Tempname.ToLower();

                    //Check for named cell
                    if ((Tempname == name) && ((string)row[5] == rarity))
                    {
                        count++;
                    }
                }

                    foreach (var row in test)
                {
                    Tempname = (string)row[1];
                    Tempname = Tempname.ToLower();

                    holder = (string)row[1];
                    if ((!valid.Contains(holder)) && (!valid.Contains("\"" + holder + "\"")))
                    {
                        if (holder.Contains(' '))
                        {
                            holder = "\"" + holder + "\"";

                        }
                        valid.Add(holder);
                    }

                    //Check for named cell
                    if ((Tempname == name) && ((string)row[5] == rarity))
                    {
                        //Assign the cells to varibales
                        CCode = (string)row[0];
                        CName = (string)row[1];
                        CSub = (string)row[2];
                        CCost = (string)row[3];
                        CEnergy = (string)row[4];
                        CRarity = (string)row[5];
                        CAffiliation = (string)row[6];
                        CEffect = (string)row[7];
                        CStat = (string)row[8];
                        CImage = (string)row[9];
                        CFImage = (string)row[10];
                        found = true;
                        if (count > 1)
                        {
                            await contextbuilder(1);
                        }
                        else
                        {
                            await contextbuilder(2);
                        }
                    }
                }
                
            }
            if (found == false)
            {
                await Error("card", name);
            }
        
        }

        [Command ("global")]
        public async Task FindGlobals(string sheet, string name)
        {
            List<string> effectholder = new List<string>();

            bool foundglobal = false;
            bool foundcharacter = false;
            sheet = sheet.ToUpper();

            if (sheet == "TMT")
                sheet = "THOR";


            if (!sets.Contains(sheet.ToUpper()))
            {
                await Error("set", sheet);
                foundglobal = true;
            }
            else
            {
                string sheetref = sheet + "!";
                GoogleTest.Main(sheetref);
                var test = GoogleTest._values;

                name = name.ToLower();

                foreach (var row in test)
                {
                    Tempname = (string)row[1];
                    Tempname = Tempname.ToLower();

                    holder = (string)row[1];
                    if ((!valid.Contains(holder)) && (!valid.Contains("\"" + holder + "\"")))
                    {
                        if (holder.Contains(' '))
                        {
                            holder = "\"" + holder + "\"";

                        }
                        valid.Add(holder);
                    }

                    

                    //Check for named cell
                    if (Tempname == name)
                    {
                        foundcharacter = true;

                        string fullEffect = (string)row[7];
                        string find = "Global: ";

                        if (fullEffect.Contains("Global: "))
                        {
                            foundglobal = true;
                            string global = fullEffect.Substring(fullEffect.IndexOf(find) + find.Length);

                            if (!effectholder.Contains(global))
                            {
                                effectholder.Add(global);
                                CGlobal = global;
                                await globalprint(name);
                            }
                        }
                    }
                }
            }
            if (foundglobal == false)
            {
                if (foundcharacter == false)
                {
                    await Error("card", name);
                }
                else
                {
                    await Error("global", name);
                }
            }
        }
    
        [Command("nick")]
        public async Task FindNick(string sheet, string nick)
        {
            sheet = sheet.ToUpper();

            if (sheet == "TMT")
                sheet = "THOR";

            bool found = false;
            if (!sets.Contains(sheet.ToUpper()))
            {
                await Error("set", sheet);
                found = true;
            }
            else
            {
                string sheetref = sheet + "!";
                GoogleTest.Main(sheetref);
                var test = GoogleTest._values;

                nick = nick.ToLower();

                foreach (var row in test)
                {
                    Tempnick = (string)row[11];
                    Tempnick = Tempnick.ToLower();

                    holder = (string)row[1];
                    if ((!valid.Contains(holder)) && (!valid.Contains("\"" + holder + "\"")))
                    {
                        if (holder.Contains(' '))
                        {
                            holder = "\"" + holder + "\"";

                        }
                        valid.Add(holder);
                    }

                    //Check for named cell
                    if (Tempnick == nick)
                    {
                        //Assign the cells to varibales
                        CCode = (string)row[0];
                        CName = (string)row[1];
                        CSub = (string)row[2];
                        CCost = (string)row[3];
                        CEnergy = (string)row[4];
                        CRarity = (string)row[5];
                        CAffiliation = (string)row[6];
                        CEffect = (string)row[7];
                        CStat = (string)row[8];
                        CImage = (string)row[9];
                        CFImage = (string)row[10];
                        found = true;
                        await contextbuilder(2);
                    }
                }
            }
            if (found == false)
            {
                await Error("card","blank");
            }
        }

        [Command("nick")]
        public async Task FindNick(string nick)
        {
            bool found = false;
            string sheetref = "NICKNAMES!";
            GoogleTest.Main(sheetref);
            var test = GoogleTest._values;

            nick = nick.ToLower();

            foreach (var row in test)
            {
                Tempnick = (string)row[11];
                Tempnick = Tempnick.ToLower();

                holder = (string)row[1];
                if ((!valid.Contains(holder)) && (!valid.Contains("\"" + holder + "\"")))
                {
                    if (holder.Contains(' '))
                    {
                        holder = "\"" + holder + "\"";

                    }
                    valid.Add(holder);
                }

                //Check for named cell
                if (Tempnick == nick)
                {
                    //Assign the cells to varibales
                    CCode = (string)row[0];
                    CName = (string)row[1];
                    CSub = (string)row[2];
                    CCost = (string)row[3];
                    CEnergy = (string)row[4];
                    CRarity = (string)row[5];
                    CAffiliation = (string)row[6];
                    CEffect = (string)row[7];
                    CStat = (string)row[8];
                    CImage = (string)row[9];
                    CFImage = (string)row[10];
                    found = true;
                    await contextbuilder(2);
                }
            }
            if (found == false)
            {
                await Error("card", nick);
            }
        }

        [Command("promo")]
        public async Task Promo(string name)
        {
            bool found = false;
            string sheetref = "PROMO!";
            GoogleTest.Main(sheetref);
            var test = GoogleTest._values;

            name = name.ToLower();

            foreach (var row in test)
            {
                Tempname = (string)row[1];
                Tempname = Tempname.ToLower();

                holder = (string)row[1];
                if ((!valid.Contains(holder)) && (!valid.Contains("\"" + holder + "\"")))
                {
                    if (holder.Contains(' '))
                    {
                        holder = "\"" + holder + "\"";

                    }
                    valid.Add(holder);
                }

                //Check for named cell
                if ((Tempname == name))
                {
                    //Assign the cells to varibales
                    CCode = (string)row[0];
                    CName = (string)row[1];
                    CSub = (string)row[2];
                    CCost = (string)row[3];
                    CEnergy = (string)row[4];
                    CRarity = (string)row[5];
                    CAffiliation = (string)row[6];
                    CEffect = (string)row[7];
                    CStat = (string)row[8];
                    CImage = (string)row[9];
                    CFImage = (string)row[10];
                    found = true;
                    await contextbuilder(2);
                }
            }
            if (found == false)
            {
                await Error("card", name);
            }
        }

        [Command("promo")]
        public async Task FindPromo(string set, string name)
        {
            bool found = false;
            string sheetref = "PROMO!";
            GoogleTest.Main(sheetref);
            var test = GoogleTest._values;

            set = set.ToLower();
            name = name.ToLower();

            foreach (var row in test)
            {
                Tempset = (string)row[0];
                Tempset = Tempset.ToLower();

                Tempname = (string)row[1];
                Tempname = Tempname.ToLower();

                holder = (string)row[1];
                if ((!valid.Contains(holder)) && (!valid.Contains("\"" + holder + "\"")))
                {
                    if (holder.Contains(' '))
                    {
                        holder = "\"" + holder + "\"";

                    }
                    valid.Add(holder);
                }

                //Check for named cell
                if ((Tempset == set) && (Tempname == name))
                {
                    //Assign the cells to varibales
                    CCode = (string)row[0];
                    CName = (string)row[1];
                    CSub = (string)row[2];
                    CCost = (string)row[3];
                    CEnergy = (string)row[4];
                    CRarity = (string)row[5];
                    CAffiliation = (string)row[6];
                    CEffect = (string)row[7];
                    CStat = (string)row[8];
                    CImage = (string)row[9];
                    CFImage = (string)row[10];
                    found = true;
                    await contextbuilder(2);
                }
            }
            if (found == false)
            {
                await Error("card", name);
            }
        }

        [Command("keyword")]
        public async Task keyword(string word)
        {
            
            WebClient client = new WebClient();
            string downloadString = client.DownloadString("https://wizkids.com/dicemasters/keywords/");
            File.WriteAllText("TextFiles/keywordscrape.txt", downloadString);
            string fixedtext = "";
            string loadtext = File.ReadAllText("TextFiles/keywordscrape.txt");
            word = Upper(word);
            int indexoffirst = loadtext.IndexOf(word);
            if (indexoffirst >= 0)
            {
                indexoffirst += word.Length;
                int indexofseccond = loadtext.IndexOf("</p>", indexoffirst);
                if (indexofseccond >= 0)
                {
                    fixedtext = loadtext.Substring(indexoffirst, indexofseccond - indexoffirst);
                }
                else
                {
                    fixedtext = loadtext.Substring(indexoffirst);
                }
                fixedtext = fixedtext.Replace("â€™", "'");
                fixedtext = fixedtext.Replace("&#8217;", "'");
                fixedtext = fixedtext.Replace("Â", "");
                fixedtext = RemoveBetween(fixedtext, '<', '>');
                fixedtext = fixedtext.Substring(fixedtext.IndexOf(">") + 1);
                fixedtext = fixedtext.Substring(fixedtext.IndexOf(":") + 2);
            }


            File.WriteAllText("TextFiles/keyword.txt", fixedtext);

            string result = File.ReadAllText("TextFiles/keyword.txt");

            await Context.Channel.SendMessageAsync("**" + word + ": **" + result);


        }

        [Command("keyword")]
        public async Task keyword(string word, string word2)
        {

            WebClient client = new WebClient();
            string downloadString = client.DownloadString("https://wizkids.com/dicemasters/keywords/");
            File.WriteAllText("TextFiles/keywordscrape.txt", downloadString);
            string fixedtext = "";
            string loadtext = File.ReadAllText("TextFiles/keywordscrape.txt");
            word = Upper(word);
            word2 = Upper(word2);
            string combined = word + " " + word2;
            int indexoffirst = loadtext.IndexOf(word + " " + word2);
            if (indexoffirst >= 0)
            {
                indexoffirst += combined.Length;
                int indexofseccond = loadtext.IndexOf("</p>", indexoffirst);
                if (indexofseccond >= 0)
                {
                    fixedtext = loadtext.Substring(indexoffirst, indexofseccond - indexoffirst);
                }
                else
                {
                    fixedtext = loadtext.Substring(indexoffirst);
                }
                fixedtext = fixedtext.Replace("â€™", "'");
                fixedtext = fixedtext.Replace("&#8217;", "'");
                fixedtext = fixedtext.Replace("Â", "");
                fixedtext = RemoveBetween(fixedtext, '<', '>');
                fixedtext = fixedtext.Substring(fixedtext.IndexOf(">") + 1);
                fixedtext = fixedtext.Substring(fixedtext.IndexOf(":") + 2);
            }


            File.WriteAllText("TextFiles/keyword.txt", fixedtext);

            string result = File.ReadAllText("TextFiles/keyword.txt");

            await Context.Channel.SendMessageAsync("**" + combined + ": **" + result);


        }

        [Command("keyword")]
        public async Task keyword(string word, string word2, string word3)
        {

            WebClient client = new WebClient();
            string downloadString = client.DownloadString("https://wizkids.com/dicemasters/keywords/");
            File.WriteAllText("TextFiles/keywordscrape.txt", downloadString);
            string fixedtext = "";
            string loadtext = File.ReadAllText("TextFiles/keywordscrape.txt");
            word = Upper(word);
            word2 = word2.ToLower();
            word3 = Upper(word3);
            string combined = word + " " + word2 + " " + word3;
            int indexoffirst = loadtext.IndexOf(combined);
            if (indexoffirst >= 0)
            {
                indexoffirst += combined.Length;
                int indexofseccond = loadtext.IndexOf("</p>", indexoffirst);
                if (indexofseccond >= 0)
                {
                    fixedtext = loadtext.Substring(indexoffirst, indexofseccond - indexoffirst);
                }
                else
                {
                    fixedtext = loadtext.Substring(indexoffirst);
                }
                fixedtext = fixedtext.Replace("â€™", "'");
                fixedtext = fixedtext.Replace("&#8217;", "'");
                fixedtext = fixedtext.Replace("Â", "");
                fixedtext = RemoveBetween(fixedtext, '<', '>');
                fixedtext = fixedtext.Substring(fixedtext.IndexOf(">") + 1);
                fixedtext = fixedtext.Substring(fixedtext.IndexOf(":") + 2);
            }


            File.WriteAllText("TextFiles/keyword.txt", fixedtext);

            string result = File.ReadAllText("TextFiles/keyword.txt");

            await Context.Channel.SendMessageAsync("**" + combined + ": **" + result);


        }

        [Command("fullpic")]
        public async Task DisplaySet(string value)
        {
            string sheet = value;
            sheet = sheet.ToUpper();

            if (sheet == "TMT")
                sheet = "THOR";

            if (!sets.Contains(sheet.ToUpper()))
            {
                await Error("set", sheet);
            }
            else
            {
               
                string sheetref = sheet + "!";
                GoogleTest.Main(sheetref);
                var test = GoogleTest._values;

                foreach(var row in test)
                {
                    CCode = (string)row[0];
                    CName = (string)row[1];
                    CSub = (string)row[2];
                    CCost = (string)row[3];
                    CEnergy = (string)row[4];
                    CRarity = (string)row[5];
                    CAffiliation = (string)row[6];
                    CEffect1 = (string)row[7];
                    CEffect = CEffect1;
                    CStat = (string)row[8];
                    CImage = (string)row[9];
                    CFImage = (string)row[10];
                    await PMbuilder(2);
                }
            }
        }

        [Command("fulltext")]
        public async Task Displaytext(string value)
        {
            string sheet = value;
            sheet = sheet.ToUpper();

            if (sheet == "TMT")
                sheet = "THOR";

            if (!sets.Contains(sheet.ToUpper()))
            {
                await Error("set", sheet);
            }
            else
            {

                string sheetref = sheet + "!";
                GoogleTest.Main(sheetref);
                var test = GoogleTest._values;

                foreach (var row in test)
                {
                    CCode = (string)row[0];
                    CName = (string)row[1];
                    CSub = (string)row[2];
                    CCost = (string)row[3];
                    CEnergy = (string)row[4];
                    CRarity = (string)row[5];
                    CAffiliation = (string)row[6];
                    CEffect1 = (string)row[7];
                    CEffect = CEffect1;
                    CStat = (string)row[8];
                    CImage = (string)row[9];
                    CFImage = (string)row[10];
                    await PMbuilder(1);
                }
            }
        }

        [Command("sub")]
        public async Task FindSub(string sheet, string name)
        {
            bool found = false;
            sheet = sheet.ToUpper();

            if (!sets.Contains(sheet.ToUpper()))
            {
                await Error("set", sheet);
                found = true;
            }
            else
            {

                string sheetref = sheet + "!";
                GoogleTest.Main(sheetref);
                var test = GoogleTest._values;

                name = name.ToLower();
                int count = 0;
                foreach (var row in test)
                {
                    Tempname = (string)row[2];
                    Tempname = Tempname.ToLower();

                    //Check for named cell
                    if (Tempname == name)
                    {
                        count++;
                    }
                }

                foreach (var row in test)
                {
                    Tempname = (string)row[2];
                    Tempname = Tempname.ToLower();

                    //Check for named cell
                    if (Tempname == name)
                    {
                        //Assign the cells to varibales
                        CCode = (string)row[0];
                        CName = (string)row[1];
                        CSub = (string)row[2];
                        CCost = (string)row[3];
                        CEnergy = (string)row[4];
                        CRarity = (string)row[5];
                        CAffiliation = (string)row[6];
                        CEffect = (string)row[7];
                        CStat = (string)row[8];
                        CImage = (string)row[9];
                        CFImage = (string)row[10];

                        found = true;
                        if (count > 1)
                        {
                            await contextbuilder(1);
                        }
                        else
                        {
                            await contextbuilder(2);
                        }
                    }
                }
            }
            if (found == false)
            {
                await Error("set", sheet);
            }
        }

        [Command("super")]
        public async Task DisplaySupers(string set)
        {
            bool found = false;
            string sheet = set;
            sheet = sheet.ToUpper();

            if (sheet == "TMT")
                sheet = "THOR";

            if (!sets.Contains(sheet.ToUpper()))
            {
                await Error("set", sheet);
                found = true;
            }
            else
            {              
                string sheetref = sheet + "!";
                GoogleTest.Main(sheetref);
                var test = GoogleTest._values;

                foreach (var row in test)
                {
                    //Check for named cell
                    if ((string)row[5] == "Super")
                    {

                        //Assign the cells to varibales
                        CCode = (string)row[0];
                        CName = (string)row[1];
                        CSub = (string)row[2];
                        CCost = (string)row[3];
                        CEnergy = (string)row[4];
                        CRarity = (string)row[5];
                        CAffiliation = (string)row[6];
                        CEffect1 = (string)row[7];
                        CEffect = CEffect1;
                        CStat = (string)row[8];
                        CImage = (string)row[9];
                        CFImage = (string)row[10];
                        found = true;

                        await contextbuilder(1);
                    }
                }
                                
            }

            if (found == false)
            {
                await Context.Channel.SendFileAsync("hall.png");
                await Context.Channel.SendMessageAsync("Oh dear," + Context.User.Mention + "\n\n I don't appear to have any super rares in that set.");
            }
        }


        private string randomNumber(string sheet)
        {

            Random number = new Random(Guid.NewGuid().GetHashCode());
            int tempaid;
            int temp;
            string tempstring;

            if (sheet == "AOU") //146
            {
                tempaid = number.Next(1, 4);
                if (tempaid == 1)
                {
                    temp = number.Next(1, 75);
                }
                else if (tempaid == 2)
                {
                    temp = number.Next(75, 107);
                }
                else if (tempaid == 3)
                {
                    temp = number.Next(107, 147);
                }
                else
                    temp = number.Next(1, 147);


                if (temp < 10)
                    tempstring = "00" + temp.ToString();
                else if ((temp < 100) && (temp >= 10))
                    tempstring = "0" + temp.ToString();
                else
                    tempstring = temp.ToString();
            }
            else if ((sheet == "ASM") || (sheet == "CW") || (sheet == "FUS") || (sheet == "WF") || (sheet == "WOL")) //142
            {
                tempaid = number.Next(1, 4);
                if (tempaid == 1)
                {
                    temp = number.Next(1, 75);
                }
                else if (tempaid == 2)
                {
                    temp = number.Next(75, 107);
                }
                else if (tempaid == 3)
                {
                    temp = number.Next(107, 143);
                }
                else
                    temp = number.Next(1, 143);

                if (temp < 10)
                    tempstring = "00" + temp.ToString();
                else if ((temp < 100) && (temp >= 10))
                    tempstring = "0" + temp.ToString();
                else
                    tempstring = temp.ToString();
            }
            else if (sheet == "YGO") //120
            {
                tempaid = number.Next(1, 4);
                if (tempaid == 1)
                {
                    temp = number.Next(1, 41);
                }
                else if (tempaid == 2)
                {
                    temp = number.Next(41, 71);
                }
                else if (tempaid == 3)
                {
                    temp = number.Next(71, 121);
                }
                else
                    temp = number.Next(1, 121);

                if (temp < 10)
                    tempstring = "00" + temp.ToString();
                else if ((temp < 100) && (temp >= 10))
                    tempstring = "0" + temp.ToString();
                else
                    tempstring = temp.ToString();
            }
            else if (sheet == "UXM") //120
            {
                tempaid = number.Next(1, 4);
                if (tempaid == 1)
                {
                    temp = number.Next(1, 41);
                }
                else if (tempaid == 2)
                {
                    temp = number.Next(41, 71);
                }
                else if (tempaid == 3)
                {
                    temp = number.Next(71, 127);
                }
                else
                    temp = number.Next(1, 127);

                if (temp < 10)
                    tempstring = "00" + temp.ToString();
                else if ((temp < 100) && (temp >= 10))
                    tempstring = "0" + temp.ToString();
                else
                    tempstring = temp.ToString();
            }
            else if ((sheet == "BAT") || (sheet == "DP") || (sheet == "GAF") || (sheet == "GOTG")) //124
            {
                tempaid = number.Next(1, 4);
                if (tempaid == 1)
                {
                    temp = number.Next(1, 41);
                }
                else if (tempaid == 2)
                {
                    temp = number.Next(41, 81);
                }
                else if (tempaid == 3)
                {
                    temp = number.Next(81, 125);
                }
                else
                    temp = number.Next(1, 125);

                if (temp < 10)
                    tempstring = "00" + temp.ToString();
                else if ((temp < 100) && (temp >= 10))
                    tempstring = "0" + temp.ToString();
                else
                    tempstring = temp.ToString();
            }
            else if (sheet == "AVX") //134
            {
                tempaid = number.Next(1, 4);
                if (tempaid == 1)
                {
                    temp = number.Next(1, 65);
                }
                else if (tempaid == 2)
                {
                    temp = number.Next(65, 99);
                }
                else if (tempaid == 3)
                {
                    temp = number.Next(99, 133);
                }
                else
                    temp = number.Next(1, 133);

                if (temp < 10)
                    tempstring = "00" + temp.ToString();
                else if ((temp < 100) && (temp >= 10))
                    tempstring = "0" + temp.ToString();
                else
                    tempstring = temp.ToString();
            }
            else if (sheet == "BFF" || (sheet == "DP")) //138
            {
                tempaid = number.Next(1, 4);
                if (tempaid == 1)
                {
                    temp = number.Next(1, 65);
                }
                else if (tempaid == 2)
                {
                    temp = number.Next(65, 99);
                }
                else if (tempaid == 3)
                {
                    temp = number.Next(99, 139);
                }
                else
                    temp = number.Next(1, 139);

                if (temp < 10)
                    tempstring = "00" + temp.ToString();
                else if ((temp < 100) && (temp >= 10))
                    tempstring = "0" + temp.ToString();
                else
                    tempstring = temp.ToString();
            }
            else if ((sheet == "SMC") || (sheet == "DEF") || (sheet == "DRS") || (sheet == "SMC")) //24
            {
                temp = number.Next(1, 25);
                if (temp < 10)
                    tempstring = "0" + temp.ToString();
                else
                    tempstring = temp.ToString();
            }
            else if ((sheet == "SWW") || (sheet == "IMW")) //34
            {
                temp = number.Next(1, 35);
                if (temp < 10)
                    tempstring = "0" + temp.ToString();
                else
                    tempstring = temp.ToString();
            }
            else
                tempstring = "";

            return tempstring;
                
        }

        private async Task<string> randomSheet(int number)
        {
            await Task.Delay(1);
            return sets[number];
        }

        private async Task contextbuilder(int type)
        {
            if (CName == "Imprisoned")
            {
                await Context.Channel.SendMessageAsync("... ... Just why would you want to know about THIS card?! Ugh, you humans disgust me.");
                System.Threading.Thread.Sleep(2000);
            
            }


            if (CRarity == "Common")
            {
                rarity = new Color(165, 162, 159);
            }
            else if (CRarity == "Uncommon")
            {
                rarity = new Color(56, 181, 0);
            }
            else if (CRarity == "Rare")
            {
                rarity = new Color(255, 255, 93);
            }
            else if (CRarity == "Super")
            {
                rarity = new Color(255, 0, 0);
            }
            else if (CRarity == "Promo")
            {
                rarity = new Color(100, 211, 249);
            }
            else if (CRarity == "Chase")
            {
                rarity = new Color(218, 40, 176);
            }
            else
            {
                rarity = new Color(0, 0, 0);
            }

            EmbedBuilder MyEmbedBuilder = new EmbedBuilder();
            MyEmbedBuilder.WithColor(rarity);
            MyEmbedBuilder.WithTitle(CName);
            MyEmbedBuilder.WithDescription(CSub);
            if (type == 2) MyEmbedBuilder.WithImageUrl(CImage);

            EmbedFooterBuilder MyFooterBuilder = new EmbedFooterBuilder();
            MyFooterBuilder.WithText(CStat + " -- " + CCode);
            MyFooterBuilder.WithIconUrl(CFImage);
            MyEmbedBuilder.WithFooter(MyFooterBuilder);

            EmbedFieldBuilder MyEmbedField = new EmbedFieldBuilder();
            MyEmbedField.WithIsInline(true);
            MyEmbedField.WithName("__Cost__");
            CEnergy = EffectEmojiInsert(CEnergy);
            MyEmbedField.WithValue(CCost + " " + CEnergy);
            MyEmbedBuilder.AddField(MyEmbedField);

            EmbedFieldBuilder MyEmbedField2 = new EmbedFieldBuilder();
            MyEmbedField2.WithIsInline(true);
            MyEmbedField2.WithName("__Affiliation__");
            CAffiliation = AffiliationReplacer(CAffiliation);
            MyEmbedField2.WithValue(CAffiliation);
            MyEmbedBuilder.AddField(MyEmbedField2);

            if (CEffect.Contains("Global:"))
            {
                global = true;
                CGlobal = CEffect.Substring(CEffect.IndexOf("Global:") + 7);
                CEffect = CEffect.Substring(0, CEffect.IndexOf("Global:"));
            }

            if (global == true)
            {
                EmbedFieldBuilder MyEmbedField3 = new EmbedFieldBuilder();
                MyEmbedField3.WithName("__Effect__");
                CEffect = AffiliationReplacer(CEffect);
                MyEmbedField3.WithValue(CEffect);
                MyEmbedBuilder.AddField(MyEmbedField3);

                EmbedFieldBuilder MyEmbedField4 = new EmbedFieldBuilder();
                MyEmbedField4.WithName("__Global__");
                CGlobal = AffiliationReplacer(CGlobal);
                MyEmbedField4.WithValue(CGlobal);
                MyEmbedBuilder.AddField(MyEmbedField4);

                global = false;
            }
            else
            {
                EmbedFieldBuilder MyEmbedField3 = new EmbedFieldBuilder();
                MyEmbedField3.WithName("__Effect__");
                CEffect = AffiliationReplacer(CEffect);
                MyEmbedField3.WithValue(CEffect);
                MyEmbedBuilder.AddField(MyEmbedField3);
            }

            await Context.Channel.SendMessageAsync("", false, MyEmbedBuilder.Build());
        }
        private async Task globalprint(string name)
        {

           
            EmbedBuilder MyEmbedBuilder = new EmbedBuilder();
            MyEmbedBuilder.WithColor(0xff9115);
            MyEmbedBuilder.WithTitle(name + "'s Global Abilities:" );

            EmbedFieldBuilder MyEmbedField4 = new EmbedFieldBuilder();
            MyEmbedField4.WithName("__Global__");
            CGlobal = AffiliationReplacer(CGlobal);
            MyEmbedField4.WithValue(CGlobal);
            MyEmbedBuilder.AddField(MyEmbedField4);


            await Context.Channel.SendMessageAsync("", false, MyEmbedBuilder.Build());
        }

        private async Task PMbuilder(int type)
        {

            CEffect = CEffect.Replace("\n", Environment.NewLine);


            if (CRarity == "Common")
            {
                rarity = new Color(165, 162, 159);
            }
            else if (CRarity == "Uncommon")
            {
                rarity = new Color(56, 181, 0);
            }
            else if (CRarity == "Rare")
            {
                rarity = new Color(255, 255, 93);
            }
            else if (CRarity == "Super")
            {
                rarity = new Color(255, 0, 0);
            }
            else if (CRarity == "Promo")
            {
                rarity = new Color(100, 211, 249);
            }
            else if (CRarity == "Chase")
            {
                rarity = new Color(218, 40, 176);
            }
            else
            {
                rarity = new Color(0, 0, 0);
            }

            EmbedBuilder MyEmbedBuilder = new EmbedBuilder();
            MyEmbedBuilder.WithColor(rarity);
            MyEmbedBuilder.WithTitle(CName);
            MyEmbedBuilder.WithDescription(CSub);
            if(type == 2) MyEmbedBuilder.WithImageUrl(CImage);

            EmbedFooterBuilder MyFooterBuilder = new EmbedFooterBuilder();
            MyFooterBuilder.WithText(CStat + " -- " + CCode);
            MyFooterBuilder.WithIconUrl(CFImage);
            MyEmbedBuilder.WithFooter(MyFooterBuilder);

            EmbedFieldBuilder MyEmbedField = new EmbedFieldBuilder();
            MyEmbedField.WithIsInline(true);
            MyEmbedField.WithName("__Cost__");
            CEnergy = EffectEmojiInsert(CEnergy);
            MyEmbedField.WithValue(CCost + " " + CEnergy);
            MyEmbedBuilder.AddField(MyEmbedField);

            EmbedFieldBuilder MyEmbedField2 = new EmbedFieldBuilder();
            MyEmbedField2.WithIsInline(true);
            MyEmbedField2.WithName("__Affiliation__");
            CAffiliation = AffiliationReplacer(CAffiliation);
            MyEmbedField2.WithValue(CAffiliation);
            MyEmbedBuilder.AddField(MyEmbedField2);

            if (CEffect.Contains("Global:"))
            {
                global = true;
                CGlobal = CEffect.Substring(CEffect.IndexOf("Global:") + 7);
                CEffect = CEffect.Substring(0, CEffect.IndexOf("Global:"));
            }

            if (global == true)
            {
                EmbedFieldBuilder MyEmbedField3 = new EmbedFieldBuilder();
                MyEmbedField3.WithName("__Effect__");
                CEffect = AffiliationReplacer(CEffect);
                MyEmbedField3.WithValue(CEffect);
                MyEmbedBuilder.AddField(MyEmbedField3);

                EmbedFieldBuilder MyEmbedField4 = new EmbedFieldBuilder();
                MyEmbedField4.WithName("__Global__");
                CEffect = AffiliationReplacer(CGlobal);
                MyEmbedField4.WithValue(CGlobal);
                MyEmbedBuilder.AddField(MyEmbedField4);

                global = false;
            }
            else
            {
                EmbedFieldBuilder MyEmbedField3 = new EmbedFieldBuilder();
                MyEmbedField3.WithName("__Effect__");
                CEffect = AffiliationReplacer(CEffect);
                MyEmbedField3.WithValue(CEffect);
                MyEmbedBuilder.AddField(MyEmbedField3);
            }

            await Context.User.SendMessageAsync("", false, MyEmbedBuilder.Build());
        }

        public async Task Error(string type, string erronious)
        {
            var activeuser = Context.User;
            string errorType = "";
            string errorMessage = "";
            string normalError = "";
            //Color rarity = new Color(218, 40, 176);

            if (type == "set")
            {
                Dictionary<string, int> resultset = new Dictionary<string, int>();
                foreach (string stringtoTest in sets)
                {
                    resultset.Add(stringtoTest, LevenshteinDistance.Compute(erronious, stringtoTest));
                }
               
                int minimumModifications = resultset.Min(c => c.Value);


                string s = "Could you have been looking for the following?  : \n";

                foreach (KeyValuePair<string, int> pair in resultset)
                {
                    if (pair.Value == minimumModifications)
                    {
                        int index = Array.IndexOf(sets, pair.Key);
                        string nameOfSet = setname[index];

                        s += pair.Key + " | " + nameOfSet + "\n";

                    }
                }

                errorType = "Unknown Set";
                errorMessage = "My, my, my, " + Context.User.Username + ". I do not have a record of that set in my collection. \n Please type \">sets\" to have" +
                    " a list of sets and their codes to you via private message. Alternatively you can type \">setshere\" to have the list printed out in the current channel. \n\n" + s;


                normalError = "the set";
            }
            else if (type == "card")
            {


                errorType = "Unknown Card/Character";
                string s = "";
                for (int i = 0; i < valid.Count; i++)
                {
                    if (i == valid.Count - 1)
                    {
                        s += valid[i];
                    }
                    else
                    {
                        s += valid[i] + ", ";
                    }
                }

                Dictionary<string, int> resultset = new Dictionary<string, int>();
                foreach (string stringtoTest in valid)
                {
                    resultset.Add(stringtoTest, LevenshteinDistance.Compute(erronious, stringtoTest));
                }

                int minimumModifications = resultset.Min(c => c.Value);


                s += "\n\nCould you have been looking for the following? : \n";

                foreach (KeyValuePair<string, int> pair in resultset)
                {
                    if (pair.Value == minimumModifications)
                    {
                        int intex = valid.IndexOf(pair.Key);
                       
                        s += pair.Key + "\n";

                    }

                }

                errorMessage = ("My, my, my, " + Context.User.Username + "\n\n I don't appear to have that card recoreded. \n\n Here are the valid cards in the given set :\n " + s + "\n\nIf you copy and paste, make sure to include any quotation marks.");
                normalError = "the card name";

            }
            else if (type == "rarity")
            {
                errorType = "Rarity";
                errorMessage = "At least there was an attempt" + Context.User.Username + "... Here is how to do the rarities. \n\nFor Common cards type : c or Common. \nFor Uncommon cards type : uc, u or Uncommon. \nFor Rare cards type : r or Rare" +
                    "\nFor Super Rare cards type: s, sr, Super or 'Super Rare'. \nFor promo cards type: p or Promo. \nAnd finally, for Chase cards type: ch or Chase.";
                normalError = "the rarity";
            }
            else if (type == "global")
            {
                errorType = "Global";
                errorMessage = "*sigh*.  " + Context.User.Username + " You do know that that character does not have a global, right?";
                normalError = "the globals on that character";
            }
            else if (type == "dice")
            {
                errorType = "teambuildergif";
                errorMessage = "*hmmm*.  " + Context.User.Username + " How about trying one of the following 4 options. Dice or die for the dice quantites, or nodice or nodie for images without quantites?";
                normalError = "the quantiy graphic variable";
            }
            else
            {
                errorType = "???";
                errorMessage = "???";
            }

            EmbedBuilder EmbeddedError = new EmbedBuilder();
            EmbeddedError.WithColor(218, 40, 176);
            EmbeddedError.WithTitle("__ERROR!__");
            EmbeddedError.WithDescription(errorType);
            EmbeddedError.WithImageUrl("https://i.imgur.com/Ov6CJML.jpg");

            EmbedFieldBuilder ErrorMessageField = new EmbedFieldBuilder();
            ErrorMessageField.WithIsInline(true);
            ErrorMessageField.WithName("A Message from Brainiac");
            ErrorMessageField.WithValue(errorMessage);
            EmbeddedError.AddField(ErrorMessageField);

            await Context.Channel.SendMessageAsync("There is an error with  " + normalError + ". I have PM'ed you more details.");
            await activeuser.SendMessageAsync("", false, EmbeddedError.Build());
         
        }

        public static string RemoveBetween(string s, char begin, char end)
        {
            Regex regex = new Regex(string.Format("\\{0}.*?\\{1}", begin, end));
            return regex.Replace(s, string.Empty);
        }

        public static string Upper(string s)
        {
            s.ToLower();
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            // Return char and concat substring.
            return char.ToUpper(s[0]) + s.Substring(1);
        }

        public static string AffiliationReplacer(string s)
        {

            for (int i = 0; i < Affiliations.Length; i++)
            {
                if(s.Contains(Affiliations[i]))
                {
                    s = s.Replace(Affiliations[i], AffiliationEmoji[i]);
                }
            }

            for (int i = 0; i < keywords.Length; i++)
            {
                if(s.Contains(keywords[i]))
                {
                    if (i == 4)
                    {
                        s = s.Replace("Anti-Breath Weapon", "**Anti-Breath Weapo**");
                    }
                    else
                    {
                        s = s.Replace(keywords[i], "**" + keywords[i] + "**");
                    }
                    
                }

            }

            s = s.Replace("**Anti-Breath Weapo**", "**Anti-Breath Weapon**");

            if (s.Contains("Avenger") && (!s.Contains("<:Avengers:336237385547513856>")))
            {
                s = s.Replace("Avenger", "<:Avengers:336237385547513856>");
            }

            if (s.Contains("Villain")|| s.Contains("Villains") || s.Contains("villain") || s.Contains("villains"))
            {
                s = s.Replace("villain", "<:villain:336232473862340614>");
                s = s.Replace("Villains", "<:villain:336232473862340614>");
                s = s.Replace("villains", "<:villain:336232473862340614>");
                s = s.Replace("Villain", "<:villain:336232473862340614>");
            }

            if (s.Contains("Fist"))
            {
                if(CName == "Iron Fist")
                {

                }
                else
                {
                    s = s.Replace("Fist", "<:Fist:366516545284866048>");
                }
            }

            if (s.Contains("Mask"))
            {
               s = s.Replace("Mask", "<:Mask:366516573466394624>");
            }

            if (s.Contains("Bolt"))
            {
                if ((CName == "Black Bolt") || (CName == "King Black Bolt"))
                {

                }
                else
                {
                    s = s.Replace("Bolt", "<:Bolt:366516620522160128>");
                }
            }

            if (s.Contains("Shield"))
            {
                if ((CName == "Vibranium Shield"))
                {

                }
                else
                {
                    s = s.Replace("Shield", "<:Shield:366516603027980288>.");
                }
            }
            return s;
        }

        public static string EffectEmojiInsert(string s)
        {
            if(s == "Fist")
            {
                s = "<:Fist:366516545284866048>";
            }
            else if (s == "Shield")
            {
                s = "<:Shield:366516603027980288>";
            }
            else if (s == "Mask")
            {
                s = "<:Mask:366516573466394624>";
            }
            else if (s == "Bolt")
            {
                s = "<:Bolt:366516620522160128>";
            }
            return s;
        }


    }

    static class LevenshteinDistance
    {
        /// <summary>
        /// Compute the distance between two strings.
        /// </summary>
        public static int Compute(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            // Step 1
            if (n == 0)
            {
                return m;
            }

            if (m == 0)
            {
                return n;
            }

            // Step 2
            for (int i = 0; i <= n; d[i, 0] = i++)
            {
            }

            for (int j = 0; j <= m; d[0, j] = j++)
            {
            }

            // Step 3
            for (int i = 1; i <= n; i++)
            {
                //Step 4
                for (int j = 1; j <= m; j++)
                {
                    // Step 5
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    // Step 6
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            // Step 7
            return d[n, m];
        }
    }
}
