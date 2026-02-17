using FluentAssertions;
using Mars.HttpSmartAuthFlow.Strategies;

namespace Mars.HttpSmartAuthFlow.Integration.Tests;

public class ParseFormTests
{
    private const string wordPressLoginPageUrl = "http://localhost:10100/wp-login.php";
    private const string wordPressLoginFormHtml = """
        <form name="loginform" id="loginform" action="http://localhost:10100/wp-login.php" method="post">
        	<p>
        		<label for="user_login">Имя пользователя или email</label>
        		<input type="text" name="log" id="user_login" class="input" value="" size="20" autocapitalize="off" autocomplete="username" required="required" />
        	</p>

        	<div class="user-pass-wrap">
        		<label for="user_pass">Пароль</label>
        		<div class="wp-pwd">
        			<input type="password" name="pwd" id="user_pass" class="input password-input" value="" size="20" autocomplete="current-password" spellcheck="false" required="required" />
        			<button type="button" class="button button-secondary wp-hide-pw hide-if-no-js" data-toggle="0" aria-label="Показать пароль">
        				<span class="dashicons dashicons-visibility" aria-hidden="true"></span>
        			</button>
        		</div>
        	</div>
        	<p class="forgetmenot"><input name="rememberme" type="checkbox" id="rememberme" value="forever"  /> <label for="rememberme">Запомнить меня</label></p>
        	<p class="submit">
        		<input type="submit" name="wp-submit" id="wp-submit" class="button button-primary button-large" value="Войти" />
        		<input type="hidden" name="redirect_to" value="http://localhost:10100/wp-admin/" />
        		<input type="hidden" name="testcookie" value="1" />
        	</p>
        </form>
        """;

    [Fact]
    public void ParseForm_ValidData_Success()
    {
        //Arrange
        _ = nameof(CookieFormStrategy.ParseLoginForm);

        //Act
        var formInfo = new CookieFormStrategy(new()).ParseLoginForm(wordPressLoginFormHtml, wordPressLoginPageUrl);

        //Assert
        formInfo.Should().NotBeNull();
        formInfo.ActionUrl.Should().Be(wordPressLoginPageUrl);
        formInfo.UsernameField.Should().Be("log");
        formInfo.PasswordField.Should().Be("pwd");
        formInfo.HiddenFields.Count.Should().Be(2);
    }
}
