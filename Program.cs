
using Newtonsoft.Json.Linq;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Lab_2_Ilchenko
{
    class Program
    {
        private static readonly string BotToken = "";

        private static readonly TelegramBotClient BotClient = new TelegramBotClient(BotToken);
        private static readonly HttpClient HttpClient = new HttpClient();


        static async Task Main(string[] args)
        {
            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { }
            };

            try
            {
                var me = await BotClient.GetMeAsync();
                Console.WriteLine($"Bot id: {me.Id}. Bot name: {me.FirstName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return;
            }

            BotClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken
            );

            Console.WriteLine("Bot started, press any key to exit.");
            Console.ReadLine();

            cts.Cancel();
        }

        private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message && update.Message?.Text != null)
            {
                await HandleMessageReceived(botClient, update.Message);
            }
        }

        private static async Task HandleMessageReceived(ITelegramBotClient botClient, Message message)
        {
            if (message.Text == "/start")
            {
                await SendMainMenu(botClient, message.Chat.Id);
            }
            else if (message.Text == "/help")
            {
                await SendHelpMenu(botClient, message.Chat.Id);
            }
            else // Assume the user has entered a word to look up in the dictionary
            {
                var word = message.Text;
                var definition = await GetWordDefinition(word);
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: definition,
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown
                );
            }
        }

        private static async Task SendMainMenu(ITelegramBotClient botClient, long chatId)
        {
            var mainKeyboard = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "/start"},
                new KeyboardButton[] {"/help" },
            })
            {
                ResizeKeyboard = true
            };

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Welcome to the Dictionary Bot! 📚\n\n" +
                      "Here's how you can use me:\n" +
                      "- Just type a word and I'll give you its definition, meaning, and phonetics.\n" +
                      "- Use /start to start the bot.\n" +
                      "- Use /help to get this instruction message.",
                replyMarkup: mainKeyboard
            );
        }
        private static async Task SendHelpMenu(ITelegramBotClient botClient, long chatId)
        {
            var helpText = "Here's how you can use me:\n" +
                           "- Just type a word and I'll give you its definition, meaning, and phonetics.\n" +
                           "- Use /start to start the bot.\n" +
                           "- Use /help to get this instruction message.";

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: helpText
            );
        }


        private static async Task<string> GetWordDefinition(string word)
        {
            string apiUrl = $"https://api.dictionaryapi.dev/api/v2/entries/en/{word}";
            try
            {
                var response = await HttpClient.GetStringAsync(apiUrl);
                var data = JArray.Parse(response);

                var output = new StringBuilder();
                output.AppendLine($"🔤 Word: **{word}**");

                foreach (var item in data)
                {
                    if (item["phonetics"].HasValues)
                    {
                        output.AppendLine($"🔊 Phonetics: **{item["phonetics"][0]["text"]}**");
                    }
                    if (item["origin"] != null)
                    {
                        output.AppendLine($"📜 Origin: **{item["origin"]}**");
                    }
                    if (item["meanings"].HasValues && item["meanings"][0]["definitions"].HasValues)
                    {
                        output.AppendLine($"📖 Part of Speech: **{item["meanings"][0]["partOfSpeech"]}**");
                        output.AppendLine($"📚 Definition: **{item["meanings"][0]["definitions"][0]["definition"]}**");
                        if (item["meanings"][0]["definitions"][0]["example"] != null)
                        {
                            output.AppendLine($"📝 Example: **{item["meanings"][0]["definitions"][0]["example"]}**");
                        }
                    }
                    break; // Only process the first item
                }

                return output.ToString();
            }
            catch (Exception ex)
            {
                // Handle exception
                return $"An error occurred: {ex.Message}";
            }
        }


        private static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Error: {exception.Message}");
            return Task.CompletedTask;
        }
    }
}
