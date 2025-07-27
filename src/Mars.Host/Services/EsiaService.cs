using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Mars.Host.Data.Contexts;
using Mars.Host.Data.Entities;
using Mars.Host.Shared.Dto.Auth;

namespace Mars.Host.Services;

public class EsiaService
{
    string JWT_SECRET = "";
    const string ESIA_PASSW = "";

    readonly ITokenService _tokenService;
    private readonly AccountsService _accountsService;
    private readonly MarsDbContext ef;

    public EsiaService(ITokenService tokenService, AccountsService accountsService, MarsDbContext marsDbContext)
    {
        _tokenService = tokenService;
        _accountsService = accountsService;
        ef = marsDbContext;
    }

    UserEntity ConvertUser(EsiaUserInfo userData)
    {
        var info = userData;

        var user = new UserEntity
        {
            UserName = userData.email,
            Email = userData.email,
            EmailConfirmed = true,
            LockoutEnabled = true,
        };

        if (DateTime.TryParse(info.birthDate, out DateTime _birthDate))
        {
            user.BirthDate = _birthDate;
        }

        user.FirstName = info.firstName ?? "";
        user.LastName = info.lastName ?? "";
        user.MiddleName = info.middleName ?? "";
        user.PhoneNumber = Regex.Replace(info.mobile ?? "", "[^0-9+]", "");

        return user;
    }

    public AuthResultDto EsiaLogin(string data, CancellationToken cancellationToken)
    {
        try
        {
            throw new NotImplementedException();
            // Была старая реализация. Которая была нестандартной. Надо переделать на новый.

            EsiaUserInfo userInfo = _tokenService.JwtDecode<EsiaUserInfo>(data, JWT_SECRET, verify: true);

            if (userInfo != null)
            {
                var existUser = ef.Users.FirstOrDefault(s => s.Email.ToLower() == userInfo.email.ToLower());

                if (existUser != null)
                {
                    return _accountsService.LoginForce(existUser.Id).Result;
                }
                else
                {
                    //var registrationData = new UserForRegistrationDto
                    //{
                    //    Email = userInfo.email,
                    //    Password = ESIA_PASSW,
                    //    ConfirmPassword = ESIA_PASSW,
                    //};

                    var user = ConvertUser(userInfo);

                    var result = _accountsService.RegisterUser(user, ESIA_PASSW, cancellationToken).ConfigureAwait(false).GetAwaiter().GetResult();

                    if (result.IsSuccessfulRegistration)
                    {
                        existUser = ef.Users.FirstOrDefault(s => s.Email.ToLower() == userInfo.email.ToLower());
                        return _accountsService.LoginForce(existUser.Id).Result;
                    }
                    else
                    {
                        return AuthResultDto.ErrorResponse(string.Join(";\n", result.Errors));
                    }

                    //_loginUser(userInfo.GetAccountLoginInfo(pass));
                    //string redirectUrl = Url.Action("Index", "Home");
                    //return Redirect(redirectUrl);

                }
            }
            else
            {
                return AuthResultDto.ErrorResponse("cannot parse token");
            }

        }
        catch (Exception ex)
        {
            return AuthResultDto.ErrorResponse("Необработанная ошибка f1c654d0-a265-4916-8738-7b663c43a60a. " + ex.Message);
        }

        //return Redirect(Url.Action("Register", "Home") + "?data=" + data);
    }

    //public ActionResult Register(string data)
    //{
    //    if (!AllowSelfRegistration)
    //        return HttpNotFound();

    //    var userRegistrationInfo = TypeAccessor.CreateInstance<UserRegistrationInfo>();
    //    userRegistrationInfo.RegisterType = SelfRegisterType.Individual;

    //    if (!string.IsNullOrEmpty(data))
    //    {
    //        EsiaUserInfo userInfo = JwtDecode<EsiaUserInfo>(data, JWT_SECRET);

    //        if (userInfo != null)
    //        {
    //            userRegistrationInfo.FirstName = userInfo.firstName;
    //            userRegistrationInfo.LastName = userInfo.lastName;
    //            userRegistrationInfo.Phone = userInfo.mobile;
    //            userRegistrationInfo.EMail = userInfo.email;
    //            userRegistrationInfo.LoginName = userInfo.Username;

    //            ViewData.Add("ESIA_AUTH_COMPLETE", true);
    //            //string password = Guid.NewGuid().ToString();
    //            string password = ESIA_PASSW;
    //            //string password = "TheiRei918(_!dkPasl01";
    //            userRegistrationInfo.NewPassword = password;
    //            userRegistrationInfo.ConfirmPassword = password;
    //            userRegistrationInfo.RegisterType = SelfRegisterType.Individual;
    //        }
    //    }

    //    GetRegions();
    //    GetMunicipalities(userRegistrationInfo.RegID);
    //    return View(userRegistrationInfo);
    //}

}

// Не оригинальная реализация Esia
public class EsiaUserInfo
{

    [JsonPropertyName("firstName")]
    public string firstName { get; set; } = default!;
    [JsonPropertyName("lastName")]
    public string lastName { get; set; } = default!;
    [JsonPropertyName("middleName")]
    public string middleName { get; set; } = default!;
    [JsonPropertyName("birthDate")]
    public string birthDate { get; set; } = default!;
    [JsonPropertyName("mobile")]
    public string mobile { get; set; } = default!;
    [JsonPropertyName("email")]
    public string email { get; set; } = default!;
    [JsonPropertyName("trusted")]
    public bool trusted { get; set; } = default!;
    [JsonPropertyName("verifying")]
    public bool verifying { get; set; } = default!;

    //[JsonIgnore]
    //public string Username => new string(mobile?.Where(s => char.IsDigit(s) || s == '+').ToArray()) {get;set;}
}
