namespace TelegramBotFirst
{
    public static class CommandStrings
    {
        public const string randomFilm = "Случайный фильм";
        public const string changeWatchedParam = "Изменить статус на просмотренный из желаемых";
        public const string filmWithRateMoreThen = "Фильм с рейтингом больше чем";
        public const string suchGenreFilm = "Фильм определённого жанра";
        public const string filmOlderThen = "Фильм старше чем";
        public const string addNewFilm = "Добавить новый фильм";
        public const string addNewFile = "Добавить новый файл с фильмами";
        public const string start = "/start";
        public const string continueStr = "/Continue";
    }

    public static class WatchedMenuStrings
    {
        public const string viewd = "Просмотренный";
        public const string wanted = "Желаемый";
        public const string back = "Назад";
    }

    public static class BotInfoStrings
    {
        public const string updated = "Данные успешно обновлены";
        public const string cantUpdate = "Не удалось обновить данные, попробуйте снова";
        public const string enterOperation = "Выберите действие:";
        public const string enterCategory = "Введите из какой категории выбрать фильм:";
        public const string toContinue = "Для продолжения /Continue";
        public const string enterRate = "Введите рейтинг";
        public const string enterGenre = "Введите жанр";
        public const string enterYear = "Введите год";
        public const string enterName = "Введите название";
        public const string enterFile = "Пришлите файл";
        public const string tryAgain = "Такого варианта нет, попробуйте сначала";
        public const string enterNum = "Введите номер из списка или нажмите Назад";
        public const string noSuchNum = "Вы ввели неверный номер, попробуйте ещё раз";
        public const string cantFind = "Не удалось найти: ";
        public const string added = "Всего фильмов доабвлено: ";
    }
}
