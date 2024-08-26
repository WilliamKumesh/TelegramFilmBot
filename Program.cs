using System;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotFirst;
using WebSiteParser;

namespace Program
{
    class Program()
    {
        private static bool start = false;
        private static bool fileAdd = false;
        private static bool waitingCommandInput = false;
        private static bool waitingMoreInfo = false;
        private static bool checkViewed = false;
        private static bool extraInfo = false;
        private static string command = "";
        private static List<string> result = new List<string>();
         
        private static readonly string BotToken = $"7463587360:AAGglB_KwNSnlx2NA6cmUTE89WZuf8YkFIs";
        private static TelegramBotClient _client;
        private static ReceiverOptions _receiverOptions;

        static async Task Main(string[] args) 
        {
            _client = new TelegramBotClient(BotToken);
            _receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[]
    {
                UpdateType.Message,
                UpdateType.CallbackQuery
            },
                ThrowPendingUpdates = true,
            };

            using var cts = new CancellationTokenSource();

            _client.StartReceiving(UpdateHandler, ErrorHandler, _receiverOptions, cts.Token);

            var me = await _client.GetMeAsync();

            await Task.Delay(-1); 
        }

        private static async Task SendMainMenuAsync(long chatId)
        {
            var replyKeyboard = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { CommandStrings.randomFilm, CommandStrings.filmWithRateMoreThen },
                new KeyboardButton[] { CommandStrings.suchGenreFilm, CommandStrings.filmOlderThen },
                new KeyboardButton[] { CommandStrings.addNewFilm, CommandStrings.addNewFile },
                new KeyboardButton[] { CommandStrings.changeWatchedParam }
            })
            {
                ResizeKeyboard = true
            };

            await _client.SendTextMessageAsync(chatId, BotInfoStrings.enterOperation, replyMarkup: replyKeyboard);
        }

        private static async Task SendWatchedMenu(long chatId)
        {
            var replyKeyboard = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { WatchedMenuStrings.viewd, WatchedMenuStrings.wanted, WatchedMenuStrings.back },
            })
            {
                ResizeKeyboard = true
            };

            await _client.SendTextMessageAsync(chatId, BotInfoStrings.enterCategory, replyMarkup: replyKeyboard);
        }

        private static async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                switch (update.Type)
                {
                    case UpdateType.Message:
                        {
                            var message = update.Message;

                            var user = message.From;

                            var chat = message.Chat;

                            DataBaseEditor.MakeConnectionString(user.Id.ToString());

                            if (!DataBaseEditor.CheckUserInSystem(user.Id.ToString()))
                            {
                                DataBaseEditor.CreateNewDataBase(user.Id.ToString());
                                DataBaseEditor.CreateTablesInDataBase();
                            }

                            switch (message.Type)
                            {
                                case MessageType.Text:
                                    {
                                        if ((message.Text == CommandStrings.start || message.Text == CommandStrings.continueStr) && !start || message.Text == WatchedMenuStrings.back)
                                        {
                                            ClearUserInput();
                                            start = true;
                                            await SendMainMenuAsync(chat.Id);
                                            return;
                                        }

                                        if (!waitingCommandInput)
                                        {
                                            waitingCommandInput = true;
                                            command += message.Text;
                                            await SendWatchedMenu(chat.Id);
                                            return;
                                        }

                                        if(waitingCommandInput && !waitingMoreInfo)
                                        {
                                            if (message.Text == WatchedMenuStrings.viewd)
                                            {
                                                checkViewed = true;
                                            }

                                            switch (command)
                                            {
                                                case CommandStrings.randomFilm:
                                                    var filmName = DataBaseEditor.GetRandomFilm(checkViewed);

                                                    var film = DataBaseEditor.GetInfoByName(filmName);

                                                    var genres = MakeOneStringFromList(film.Genres);

                                                    var toWrite = MakeOneFullSentence(film.Name, film.Year, film.Rate, genres, film.Description);

                                                    await botClient.SendTextMessageAsync(chat.Id, toWrite);

                                                    try { var trailer = await FilmInfoParser.GetTrailerLink(film.Url); await botClient.SendTextMessageAsync(chat.Id, trailer); } catch (Exception e) { }

                                                    await botClient.SendTextMessageAsync(chat.Id, BotInfoStrings.toContinue);
                                                    ClearUserInput();
                                                    return;

                                                case CommandStrings.filmWithRateMoreThen:
                                                    await botClient.SendTextMessageAsync(chat.Id, BotInfoStrings.enterRate);
                                                    break;

                                                case CommandStrings.suchGenreFilm:
                                                    await botClient.SendTextMessageAsync(chat.Id, BotInfoStrings.enterGenre);
                                                    break;

                                                case CommandStrings.filmOlderThen:
                                                    await botClient.SendTextMessageAsync(chat.Id, BotInfoStrings.enterYear);
                                                    break;

                                                case CommandStrings.addNewFilm:
                                                    await botClient.SendTextMessageAsync(chat.Id, BotInfoStrings.enterName);
                                                    break;

                                                case CommandStrings.addNewFile:
                                                    await botClient.SendTextMessageAsync(chat.Id, BotInfoStrings.enterFile);
                                                    fileAdd = true;
                                                    break;

                                                case CommandStrings.changeWatchedParam:
                                                    await botClient.SendTextMessageAsync(chat.Id, BotInfoStrings.enterName);
                                                    break;

                                                default:
                                                    ClearUserInput();
                                                    await botClient.SendTextMessageAsync(chat.Id, BotInfoStrings.tryAgain);
                                                    await botClient.SendTextMessageAsync(chat.Id, BotInfoStrings.toContinue);
                                                    return;
                                            }
                                            waitingMoreInfo = true;
                                            return;
                                        }

                                        if (waitingMoreInfo && !extraInfo)
                                        {                                                                                 
                                            switch (command)
                                            {
                                                case CommandStrings.filmWithRateMoreThen:
                                                    double.TryParse(message.Text, out double rate);
                                                    result = DataBaseEditor.GetFilmsAboveRating(rate, checkViewed);
                                                    break;

                                                case CommandStrings.suchGenreFilm:
                                                    result = DataBaseEditor.GetFilmsByGenre(message.Text, checkViewed);
                                                    break;

                                                case CommandStrings.filmOlderThen:
                                                    int.TryParse(message.Text, out int year);
                                                    result = DataBaseEditor.GetFilmsOlderThan(year, checkViewed);
                                                    break;

                                                case CommandStrings.addNewFilm:
                                                    var info = await MakeFilmInfo(message.Text);
                                                    var genres = MakeOneStringFromList(info.Genres);
                                                    DataBaseEditor.SaveFilmToDatabase(info, checkViewed);
                                                    var toWrite = MakeOneFullSentence(info.Name, info.Year, info.Rate, genres, info.Description);
                                                    await botClient.SendTextMessageAsync(chat.Id, toWrite);
                                                    ClearUserInput();
                                                    await botClient.SendTextMessageAsync(chat.Id, BotInfoStrings.toContinue);
                                                    return;

                                                case CommandStrings.changeWatchedParam:
                                                    if(DataBaseEditor.MakeWatchedFilm(message.Text, checkViewed))
                                                    {
                                                        await botClient.SendTextMessageAsync(chat.Id, BotInfoStrings.updated);
                                                    }
                                                    else
                                                    {
                                                        await botClient.SendTextMessageAsync(chat.Id, BotInfoStrings.cantUpdate);
                                                    }
                                                    ClearUserInput();
                                                    await botClient.SendTextMessageAsync(chat.Id, BotInfoStrings.toContinue);
                                                    return;

                                                default:
                                                    ClearUserInput();
                                                    await botClient.SendTextMessageAsync(chat.Id, BotInfoStrings.tryAgain);
                                                    await botClient.SendTextMessageAsync(chat.Id, BotInfoStrings.toContinue);
                                                    return;
                                            }

                                            if (result.Count != 0)
                                            {
                                                await botClient.SendTextMessageAsync(chat.Id, MakeOneFullSentence(result.ToArray()));
                                                await botClient.SendTextMessageAsync(chat.Id, BotInfoStrings.enterNum);
                                                extraInfo = true;
                                            }

                                            return;
                                        }

                                        if(extraInfo)
                                        {
                                            int.TryParse(message.Text, out int filmNum);

                                            if(filmNum > result.Count || filmNum <= 0)
                                            {
                                                await botClient.SendTextMessageAsync(chat.Id, BotInfoStrings.noSuchNum);
                                                return;
                                            }
                                            
                                            var film = await MakeFilmInfo(result[filmNum - 1]);
                                            var genres = MakeOneStringFromList(film.Genres);
                                            var toWrite = MakeOneFullSentence(film.Name, film.Year, film.Rate, genres, film.Description);

                                            await botClient.SendTextMessageAsync(chat.Id, toWrite);

                                            var trailer = await FilmInfoParser.GetTrailerLink(film.Url);
                                            await botClient.SendTextMessageAsync(chat.Id, trailer);

                                            ClearUserInput();
                                            await botClient.SendTextMessageAsync(chat.Id, BotInfoStrings.toContinue);
                                            return;
                                        }

                                        return;
                                    }

                                case MessageType.Document:
                                    {
                                        if (fileAdd)
                                        {
                                            var fileId = message.Document.FileId;

                                            var file = await botClient.GetFileAsync(fileId);

                                            using (var fileStream = new MemoryStream())
                                            {
                                                await botClient.DownloadFileAsync(file.FilePath, fileStream);
                                                fileStream.Position = 0;
                                                int count = 0;
                                                using (var reader = new StreamReader(fileStream))
                                                {
                                                    string line;
                                                    List<string> filmsNotAdded = new List<string>();

                                                    while ((line = await reader.ReadLineAsync()) != null)
                                                    {
                                                        try
                                                        {
                                                            var info = await MakeFilmInfo(line);
                                                            DataBaseEditor.SaveFilmToDatabase(info);
                                                            count++;
                                                        }
                                                        catch
                                                        {
                                                            filmsNotAdded.Add(line);
                                                        }
                                                    }

                                                    if (filmsNotAdded.Count != 0)
                                                    {
                                                        var sentence = MakeOneStringFromList(filmsNotAdded);
                                                        await botClient.SendTextMessageAsync(chat.Id, BotInfoStrings.cantFind + sentence);
                                                    }
                                                    await botClient.SendTextMessageAsync(chat.Id, BotInfoStrings.added + count.ToString());
                                                    ClearUserInput();

                                                }
                                            }
                                            return;
                                        }

                                        return;
                                    }
                            }
                            return;
                        }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        
        private static void ClearUserInput()
        {
            extraInfo = false;
            start = false;
            waitingCommandInput = false;
            waitingMoreInfo = false;
            checkViewed = false;
            fileAdd = false;
            command = "";
        }

        private static Task ErrorHandler(ITelegramBotClient botClient, Exception error, CancellationToken cancellationToken)
        {
            var ErrorMessage = error switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => error.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }

        private static string MakeOneFullSentence(params string[] strings)
        {
            var fullString = "";
            int pos = 1;
            foreach (string s in strings) 
            {
                fullString += pos.ToString();
                fullString += ") ";
                pos++;
                fullString += s;
                fullString += '\n';
            }

            return fullString;
        }

        private static string MakeOneStringFromList(List<string> strings)
        {
            var fullString = "";
            foreach (string s in strings)
            {
                fullString += s;
                fullString += ", ";
            }

            var result = fullString.Remove(fullString.Length - 2);

            return result;
        }

        private static async Task<FilmInfo> MakeFilmInfo(string message)
        { 
            var splitted = message.Split(" ");

            if (splitted.Length <= 1)
            {
                return await FilmInfoParser.GetFilmInfo(message);
            }
            var yearInfo = splitted[splitted.Length - 1];

            var complete = int.TryParse(yearInfo, out int year);

            if(!complete)
            {
                return await FilmInfoParser.GetFilmInfo(message);
            }

            return await FilmInfoParser.GetFilmInfo(message, year);
        }
    }
}