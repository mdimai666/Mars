using Mars.Core.Exceptions;
using Mars.Core.Extensions;
using Mars.Core.Utils;
using Mars.Host.Shared.Dto.Users;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Mars.Shared.Common;
using Mars.Shared.Contracts.ActionHistorys;
using Mars.Shared.Resources;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace Mars.Host.Services;

internal class NotifyService : INotifyService
{
    private readonly IMarsEmailSender _emailSender;
    private readonly ISmsSender _smsSender;
    private readonly IUserRepository _userRepository;
    private readonly IOptionService _optionService;

    private readonly IStringLocalizer<AppRes> L;
    private readonly IActionHistoryService _actionHistoryService;

    public NotifyService(IConfiguration configuration,
        IServiceProvider serviceProvider,
        IFileStorage fileStorage,
        IOptionService optionService,
        IEmailSender emailSender,
        ISmsSender smsSender,
        IUserRepository userRepository,
        IStringLocalizer<AppRes> stringLocalizer,
        IActionHistoryService actionHistoryService) 
    {
        _optionService = optionService;
        _emailSender = (emailSender as EmailSender)!;
        _smsSender = smsSender;
        _userRepository = userRepository;
        L = stringLocalizer;
        _actionHistoryService = actionHistoryService;
    }

    private string GetHtml(string Title, string Text, string Logo = null)
    {
        var wwwRoot = _optionService.FileHostingInfo().wwwRoot.LocalPath;
        string template = Path.Join(wwwRoot, @"mail_templates/template2.html");

        string html = File.ReadAllText(template);

        html = html.Replace("{{Title}}", Title);
        html = html.Replace("{{Logo}}", Logo ?? GetLogo());
        html = html.Replace("{{Text}}", Text);

        return html;
    }

    private string GetLogo()
    {
        return _optionService.SysOption.SiteUrl + "/img/logo.png";
    }

    public async Task<UserActionResult> SendNotifyTest(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetDetail(userId, cancellationToken) ?? throw new NotFoundException("user not found");

        string html = GetHtml("Test", "<div><b>Html</b>Text</div>");

        string email = user.Email;

        try
        {
            await _emailSender.SendEmailForce(email, _optionService.MailSettings.FromName, "TestNotify", html, true);

        }
        catch (Exception ex)
        {
            return new UserActionResult
            {
                Message = ex.Message
            };
        }
        return new UserActionResult
        {
            Ok = true,
            Message = "sent"
        };
    }


    public async Task<UserActionResult> SendNotify(string title, string body, Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetDetail(userId, cancellationToken) ?? throw new NotFoundException("user not found");
        return await SendNotify(title, body, user, cancellationToken);
    }

    async Task<UserActionResult> SendNotify(string title, string body, UserDetail user, CancellationToken cancellationToken)
    {

        string html = GetHtml(title, body);

        string email = user.Email;

        //string phone = user.PhoneNumber;
        //if (phone.Count() == 11 && phone.StartsWith("8")) phone = "+7" + phone.Right(10);

        bool validEmail = EmailUtil.IsEmail(email);

        //if (phone.IsPhone())
        //{
        //    _ = smsSender.Send(new SendSmsModel
        //    {
        //        Message = title,
        //        Phone = phone,
        //    });
        //}

        if (!validEmail)
        {
            _ = _actionHistoryService.Add(email, $"Неправильная почта - {email}", ActionHistoryLevel.Info, "email");

            return new UserActionResult
            {
                Message = "Неправильная почта - " + email

            };
        }
        else
        {
            try
            {
                await _emailSender.SendEmailForce(email, _optionService.MailSettings.FromName, title.Left(250), html, true);

            }
            catch (Exception ex)
            {
                _ = _actionHistoryService.Add(ex.Message, $"SendNotify- {email}", ActionHistoryLevel.Info, "email");
                return new UserActionResult
                {
                    Message = ex.Message
                };
            }
        }
        _ = _actionHistoryService.Add($"Отправлено {email}", $"SendNotify- {email}", ActionHistoryLevel.Info, "email");
        return new UserActionResult
        {
            Ok = true,
            Message = "Отправлено"
        };
    }

    public async Task<UserActionResult> SendNotify_Invation(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetDetail(userId, cancellationToken) ?? throw new NotFoundException("user not found");

        string title = $"Для вас создан Аккаунт на сайте.";

        var dict = OpenPasswords();

        bool foundPass = dict.ContainsKey(user.Email);

        if (!foundPass) return new UserActionResult
        {
            Message = "Для пользователя не найден пароль, уведомите его в частном порядке."
        };

        string getPass = dict[user.Email];

        var body = new Dictionary<string, string>()
        {
            //["ID"] = $"{zayavka.Id}",
            //["Новый статус"] = $"{status}",
            //["Комментарий"] = comment,
            ["Url"] = $"{_optionService.SysOption.SiteUrl}",
            ["Логин"] = $"{user.Email}",
            ["Пароль"] = getPass
        };

        //var recipients = new List<User> { user };
        //foreach (var recipient in recipients)
        //{
        //    _ = SendNotify(title, DictAsHtml(body), recipient);
        //}

        var res = await SendNotify(title, DictAsHtml(body), user, cancellationToken);

        if (res.Ok == false)
        {
            return res;
        }

        return new UserActionResult
        {
            Ok = true,
            Message = $"Приглашение успешно отправлено - {user.Email}"
        };
    }

    public string DictAsHtml(Dictionary<string, string> dict)
    {
        string html = dict.Select(s => @$"
            <div>
                <b>{s.Key} : </b> {s.Value}
            </div>
            ").JoinStr("\n");
        return html;
    }

    public static Dictionary<string, string> OpenPasswords()
    {
        return new Dictionary<string, string>
        {
            ["email"] = "pass",

        };
    }

}

