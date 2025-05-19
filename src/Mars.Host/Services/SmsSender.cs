using System.Text.Json;
using Mars.Host.Shared.Services;
using Mars.Shared.Common;
using Mars.Shared.Contracts.Sms;

namespace Mars.Host.Services;

internal class SmsSender : ISmsSender
{

    // smsc.ru настройки
    const string SMSC_LOGIN = ""; // логин клиента
    const string SMSC_PASSWORD = ""; // пароль
    const bool SMSC_POST = true; // использовать метод POST
    const bool SMSC_HTTPS = true; // использовать HTTPS протокол
    const string SMSC_CHARSET = "utf-8"; // кодировка сообщения (windows-1251 или koi8-r), по умолчанию используется utf-8
    const bool SMSC_DEBUG = false; // флаг отладки

    // Константы для отправки SMS по SMTP
    const string SMTP_FROM = "api@smsc.ru"; // e-mail адрес отправителя
    const string SMTP_SERVER = ""; // адрес smtp сервера
    const string SMTP_LOGIN = ""; // логин для smtp сервера
    const string SMTP_PASSWORD = ""; // пароль для smtp сервера

    // sms.ru настройки
    const string SMSRU_API_ID = "";


    public async Task<UserActionResult> Send(SendSmsModelRequest form)
    {
        var valiidate = form.EnsureValidate(form);
        if (valiidate != null) return valiidate;

        using HttpClient client = new HttpClient();
        client.BaseAddress = new Uri("https://sms.ru");


        var result = await HttpPost(client, form);

        string message = codes.GetValueOrDefault(result.status_code);

        if (result.Ok) message += " Баланс=" + result.balance.ToString("0.00");

        return new UserActionResult
        {
            Ok = result.Ok,
            Message = message,
        };

    }

    async Task<ResponseResult> HttpPost(HttpClient client, SendSmsModelRequest form)
    {
        var msg = new SmsApiMessageData
        {
            api_id = SMSRU_API_ID,
            to = form.Phone.Replace("+", ""),
            text = form.Message
        };

        string json = JsonSerializer.Serialize(msg);

        //StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
        //var response = await client.PostAsync($"/sms/send?json=1&translit=0&test=1&api_id={SMSRU_API_ID}", content);
        string url = $"/sms/send?json=1&translit=0&api_id={SMSRU_API_ID}&to[{msg.to}]={msg.text}";
#if DEBUG
        url += "&test=1";
#endif
        var response = await client.GetAsync(url);

        response.EnsureSuccessStatusCode();

        string body = await response.Content.ReadAsStringAsync();

        ResponseResult result = JsonSerializer.Deserialize<ResponseResult>(body)!;

        return result;
    }

    class SmsApiMessageData
    {
        public string api_id { get; set; }
        public string to { get; set; }
        public string text { get; set; }
    }


    public class ResponseResult
    {
        public string status { get; set; }
        public int status_code { get; set; }
        public Dictionary<string, AbonentStatus> sms { get; set; }
        public decimal balance { get; set; }

        public bool Ok => status == "OK";
    }

    public class AbonentStatus
    {
        public string status { get; set; }
        public int status_code { get; set; }
        public string status_text { get; set; }

        public bool Ok => status == "OK";

    }


    //https://sms.ru/api/send
    static readonly IReadOnlyDictionary<int, string> codes = new Dictionary<int, string>
    {
        [-1] = "Сообщение не найдено",
        [100] = "Запрос выполнен или сообщение находится в нашей очереди",//Успешный запрос
        [101] = "Сообщение передается оператору",
        [102] = "Сообщение отправлено (в пути)",
        [103] = "Сообщение доставлено",
        [104] = "Не может быть доставлено: время жизни истекло",
        [105] = "Не может быть доставлено: удалено оператором",
        [106] = "Не может быть доставлено: сбой в телефоне",
        [107] = "Не может быть доставлено: неизвестная причина",
        [108] = "Не может быть доставлено: отклонено",
        [110] = "Сообщение прочитано (для Viber, временно не работает)",
        [150] = "Не может быть доставлено: не найден маршрут на данный номер",
        [200] = "Неправильный api_id",
        [201] = "Не хватает средств на лицевом счету",
        [202] = "Неправильно указан номер телефона получателя, либо на него нет маршрута",
        [203] = "Нет текста сообщения",
        [204] = "Имя отправителя не согласовано с администрацией",
        [205] = "Сообщение слишком длинное (превышает 8 СМС)",
        [206] = "Будет превышен или уже превышен дневной лимит на отправку сообщений",
        [207] = "На этот номер нет маршрута для доставки сообщений",
        [208] = "Параметр time указан неправильно",
        [209] = "Вы добавили этот номер (или один из номеров) в стоп-лист",
        [215] = "Этот номер находится в стоп-листе SMS.RU (от получателя поступала жалоба на спам)",
        [210] = "Используется GET, где необходимо использовать POST",
        [211] = "Метод не найден",
        [212] = "Текст сообщения необходимо передать в кодировке UTF-8 (вы передали в другой кодировке)",
        [213] = "Указано более 1000 номеров в списке получателей",
        [214] = "Номер находится зарубежом (включена настройка \"Отправлять только на номера РФ\")",
        [220] = "Сервис временно недоступен, попробуйте чуть позже",
        [230] = "Превышен общий лимит количества сообщений на этот номер в день",
        [231] = "Превышен лимит одинаковых сообщений на этот номер в минуту",
        [232] = "Превышен лимит одинаковых сообщений на этот номер в день",
        [233] = "Превышен лимит отправки повторных сообщений с кодом на этот номер за короткий промежуток времени (\"защита от мошенников\", можно отключить в разделе \"Настройки\")",
        [300] = "Неправильный token (возможно истек срок действия, либо ваш IP изменился)",
        [301] = "Неправильный api_id, либо логин/пароль",
        [302] = "Пользователь авторизован, но аккаунт не подтвержден (пользователь не ввел код, присланный в регистрационной смс)",
        [303] = "Код подтверждения неверен",
        [304] = "Отправлено слишком много кодов подтверждения. Пожалуйста, повторите запрос позднее",
        [305] = "Слишком много неверных вводов кода, повторите попытку позднее",
        [500] = "Ошибка на сервере. Повторите запрос.",
        [901] = "Callback: URL неверный (не начинается на http://)",
        [902] = "Callback: Обработчик не найден (возможно был удален ранее)",
    };

    public async Task<UserActionResult> SendTestSms(SendSmsModelRequest form)
    {
        try
        {
            return await Send(form);
        }
        catch (Exception ex)
        {
            return new UserActionResult
            {
                Ok = false,
                Message = ex.Message
            };
        }
    }
}
