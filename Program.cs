using System;
using System.IO;
using System.Data.SQLite;

using Telegram.Bot;
using Telegram.Bot.Args;
using System.Collections.Generic;
using Telegram.Bot.Types.ReplyMarkups;
using System.Globalization;
using Telegram.Bot.Types.InputFiles;
using System.Threading;

namespace IPZ_bot
{
    class Program
    {
        static ITelegramBotClient botClient;
        static SQLiteConnection con;
        const string startStickerId = "CAACAgIAAxkBAAIHZl9YuiQy8kcDSyjOsIo48ow-D1bQAAICAQACkAABUCDZF3t-23uX2BsE";

        static void Main(string[] args)
        {
            string cs = @$"URI=file:{Directory.GetCurrentDirectory()}\testDB.db;";
            con = new SQLiteConnection(cs);
            con.Open();

            botClient = new TelegramBotClient("921117196:AAGIf_Dm4L_ko0hf-q5VrhNFZGoL2cdFOH4");
            var me = botClient.GetMeAsync().Result;
            botClient.OnMessage += BotClient_OnMessage;
            botClient.StartReceiving();

            Thread.Sleep(-1);
            botClient.StopReceiving();
        }

        static async void BotClient_OnMessage(object sender, MessageEventArgs e)
        {            
            if (e.Message.Text != null)
            {
                string answer = null;
                IReplyMarkup replyMarkup = null;
                switch (e.Message.Text.ToLower())
                {
                    case "/start":
                        replyMarkup = GetKeyboard();
                        break;
                    case "варианты":
                        answer = GetGroupList();
                        break;
                    case "расписание":
                        answer = GetSchedule(IsWeekEven());
                        break;
                    case "на след. неделю":
                        answer = GetSchedule(!IsWeekEven());
                        break;
                    default:
                        return;
                }

                if (answer != null)
                {
                    await botClient.SendTextMessageAsync(
                        chatId: e.Message.Chat,
                        text: answer,
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
                    );
                }
                else
                {
                    var startSticker = new InputOnlineFile(startStickerId);
                    await botClient.SendStickerAsync(
                        chatId: e.Message.Chat,
                        sticker: startSticker,
                        replyMarkup: replyMarkup
                    );
                }
            }
        }

        private static bool IsWeekEven()
        {
            CultureInfo myCI = new CultureInfo("uk-UA");
            Calendar myCal = myCI.Calendar;
            // Gets the DTFI properties required by GetWeekOfYear.
            CalendarWeekRule myCWR = myCI.DateTimeFormat.CalendarWeekRule;
            DayOfWeek myFirstDOW = myCI.DateTimeFormat.FirstDayOfWeek;
            
            var startingPoint = 36;
            var currentWeekNum = myCal.GetWeekOfYear(DateTime.Now, myCWR, myFirstDOW);
            return (currentWeekNum - startingPoint) % 2 == 0;
        }

        static string GetSchedule(bool isWeekEven)
        {
            string stm = $"SELECT * FROM {(isWeekEven ? "GetEvenSchedule" : "GetOddSchedule")}";
            using var cmd = new SQLiteCommand(stm, con);
            using SQLiteDataReader rdr = cmd.ExecuteReader();

            string answer = "";
            string week = "";
            while (rdr.Read())
            {
                if (rdr["weekday"].ToString() != week)
                {
                    answer += $"\n<b>{rdr["weekday"]}</b>\n";
                    week = $"{rdr["weekday"]}";
                }
                answer += $"{rdr["number"]}. {rdr["subject"]} ({rdr["type"]}) {rdr["classroom"]}\n";
            }
            return answer;
        }

        static string GetGroupList()
        {
            string stm = "SELECT * FROM student";
            using var cmd = new SQLiteCommand(stm, con);
            using SQLiteDataReader rdr = cmd.ExecuteReader();

            string answer = "";
            int i = 0;
            int columnNum = 0;
            foreach (System.Data.Common.DbDataRecord item in rdr)
            {
                answer += $"{++i}. {item.GetValue(columnNum)}\n";
            }
            return answer;
        }

        static ReplyKeyboardMarkup GetKeyboard()
        {
            
            var button1 = new KeyboardButton()
            {
                Text = "Расписание"
            };
            var button2 = new KeyboardButton()
            {
                Text = "Варианты"
            };
            var firstRow = new List<KeyboardButton>() { button1, button2 };
            var button3 = new KeyboardButton()
            {
                Text = "На след. неделю"
            };
            var secondRow = new List<KeyboardButton>() { button3 };
            var keyboard = new ReplyKeyboardMarkup(new List<List<KeyboardButton>>() { firstRow, secondRow }, resizeKeyboard: true);

            return keyboard;
        }
    }
}
