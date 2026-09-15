using FluentAssertions;
using Mars.Datasource.Abstractions.Models;

namespace Mars.Datasource.Integration.Tests;

public class SqlSafetyTests
{
    [Theory]
    [InlineData("DROP TABLE posts")]
    [InlineData("  truncate table posts;")]
    [InlineData("-- заметка\nALTER TABLE posts ADD COLUMN x int")]
    [InlineData("/* заметка */ DELETE FROM posts")]
    [InlineData("DELETE FROM posts")]
    [InlineData("update posts\nset title = 'x'")]
    [InlineData("revoke all on posts from wp")]
    public void IsDestructive_DangerousStatement_ReturnsTrue(string sql)
    {
        SqlSafety.IsDestructive(sql).Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("-- DELETE FROM posts")]
    [InlineData("SELECT * FROM posts")]
    [InlineData("WITH x AS (SELECT 1) SELECT * FROM x")]
    [InlineData("UPDATE posts SET title = 'x' WHERE id = 1")]
    [InlineData("DELETE FROM posts WHERE id = 1")]
    [InlineData("INSERT INTO posts (title) VALUES ('a')")]
    public void IsDestructive_SafeStatement_ReturnsFalse(string sql)
    {
        SqlSafety.IsDestructive(sql).Should().BeFalse();
    }

    [Theory]
    [InlineData("select 1", "SELECT")]
    [InlineData("  -- c\n\ndelete from t", "DELETE")]
    [InlineData("", "")]
    public void FirstWord_Statement_ReturnsFirstKeyword(string sql, string expected)
    {
        SqlSafety.FirstWord(sql).Should().Be(expected);
    }
}
