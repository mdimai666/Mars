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

namespace Mars.Host.Services;

internal class NotifyService : INotifyService
{
    private readonly IMarsEmailSender _emailSender;
    private readonly IUserRepository _userRepository;
    private readonly IOptionService _optionService;

    private readonly IActionHistoryService _actionHistoryService;

    public NotifyService(IConfiguration configuration,
        IServiceProvider serviceProvider,
        IFileStorage fileStorage,
        IOptionService optionService,
        IEmailSender emailSender,
        IUserRepository userRepository,
        IActionHistoryService actionHistoryService)
    {
        _optionService = optionService;
        _emailSender = (emailSender as EmailSender)!;
        _userRepository = userRepository;
        _actionHistoryService = actionHistoryService;
    }

    private string GetHtml(string title, string text, string? logo = null)
    {
        string templateFile = Path.Join("Res", "mail_templates", "template2.html");

        string html = File.ReadAllText(templateFile);

        html = html.Replace("{{Title}}", title);
        html = html.Replace("{{Logo}}", logo ?? GetLogo());
        html = html.Replace("{{Text}}", text);

        return html;
    }

    private string GetLogo()
    {
        return _optionService.SysOption.SiteUrl + "/img/logo.png";
    }

    public async Task<UserActionResult> SendNotifyTest(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetDetail(userId, cancellationToken) ?? throw new NotFoundException("user not found");
        if (string.IsNullOrEmpty(user.Email)) return new UserActionResult { Message = AppRes.The_user_did_not_provide_an_email };

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

        if (string.IsNullOrEmpty(user.Email)) return new UserActionResult { Message = AppRes.The_user_did_not_provide_an_email };
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
        if (string.IsNullOrEmpty(user.Email)) throw new UserActionException(AppRes.The_user_does_not_have_email);
        //TODO: Сделать сброс и отправку пароля

        string title = $"Для вас создан Аккаунт на сайте.";

        var body = new Dictionary<string, string>()
        {
            //["ID"] = $"{zayavka.Id}",
            //["Новый статус"] = $"{status}",
            //["Комментарий"] = comment,
            ["Url"] = $"{_optionService.SysOption.SiteUrl}",
            ["Логин"] = $"{user.Email}",
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

}

