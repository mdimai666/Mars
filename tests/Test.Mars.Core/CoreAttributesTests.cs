using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Test.Mars.Core;

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
    public void TestEmailAddressThatAllowsBlanks()
    {
        var user1valid = new TestUser("Dima", "user@example.com");
        var user2valid = new TestUser("Aina", "");
        var user3valid = new TestUser("Alex", null!);
        var user91notValid = new TestUser("Vasya", "xxxx");
        var user92notValid = new TestUser("Zen", "1");

        var validator = new EmailAddressThatAllowsBlanks();


        //Check alhoritm
        Assert.True(validator.IsValid(user1valid.Email));
        Assert.True(validator.IsValid(user2valid.Email));
        Assert.True(validator.IsValid(user3valid.Email));
        Assert.False(validator.IsValid(user91notValid.Email));
        Assert.False(validator.IsValid(user92notValid.Email));

        //Check attribute functionality
        ValidationContext vx1 = new ValidationContext(user1valid);
        ValidationContext vx2 = new ValidationContext(user2valid);
        ValidationContext vx3 = new ValidationContext(user3valid);
        ValidationContext vx4 = new ValidationContext(user91notValid);
        ValidationContext vx5 = new ValidationContext(user92notValid);

        Assert.True(Validator.TryValidateObject(user1valid, vx1, null, true));
        Assert.True(Validator.TryValidateObject(user2valid, vx2, null, true));
        Assert.True(Validator.TryValidateObject(user3valid, vx3, null, true));
        Assert.False(Validator.TryValidateObject(user91notValid, vx4, null, true));
        Assert.False(Validator.TryValidateObject(user92notValid, vx5, null, true));
    }

    class TestPostClass
    {
        [SlugString]
        public required string Slug { get; set; } = default!;
    }

    [Fact]
    public void SlugStringValidator()
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
