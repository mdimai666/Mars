using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Core.Tests;

public class CoreAttributesTests
{

    class TestUser
    {
        public string Name { get; set; } = "";
        public int Age { get; set; } = 18;

        [EmailAddressThatAllowsBlanks]
        public string Email { get; set; } = "";

        public TestUser(string name, string email)
        {
            Name = name;
            Email = email;
        }
    }

    [Fact]
    public void IsValid_BlankValidAndInvalidEmails_PassesBlankAndValid()
    {
        var validator = new EmailAddressThatAllowsBlanks();
        string[] validEmails = ["user@example.com", "", null!];
        string[] invalidEmails = ["xxxx", "1"];

        foreach (var email in validEmails)
        {
            Assert.True(validator.IsValid(email));
        }

        foreach (var email in invalidEmails)
        {
            Assert.False(validator.IsValid(email));
        }
    }

    [Fact]
    public void TryValidateObject_UsersWithEmailAttribute_PassesBlankAndValid()
    {
        TestUser[] valid =
        [
            new("Dima", "user@example.com"),
            new("Aina", ""),
            new("Alex", null!),
        ];
        TestUser[] invalid =
        [
            new("Vasya", "xxxx"),
            new("Zen", "1"),
        ];

        foreach (var user in valid)
        {
            Assert.True(Validator.TryValidateObject(user, new ValidationContext(user), null, true));
        }

        foreach (var user in invalid)
        {
            Assert.False(Validator.TryValidateObject(user, new ValidationContext(user), null, true));
        }
    }

    class TestPostClass
    {
        [SlugString]
        public required string Slug { get; set; } = default!;
    }

    [Fact]
    public void Validate_Slug_EmptyNullOrLeadingDot_Fails()
    {
        TestPostClass[] validPosts = [
            new(){ Slug = "slug1" },
        ];
        TestPostClass[] notValidPosts = [
            new(){ Slug = "" },
            new(){ Slug = null! },
            new(){ Slug = ".aaaa" },
        ];

        foreach (var post in validPosts)
        {
            ValidationContext vx = new(post);
            Assert.True(Validator.TryValidateObject(post, vx, null, true));
        }

        foreach (var post in notValidPosts)
        {
            ValidationContext vx = new(post);
            Assert.False(Validator.TryValidateObject(post, vx, null, true));
        }

    }
}
