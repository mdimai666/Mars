namespace Mars.SSO.Dto;

internal class GithubUserInfoResponse
{
    public string login { get; set; } = default!;
    public int id { get; set; }
    public string node_id { get; set; } = default!;
    public string avatar_url { get; set; } = default!;
    public string gravatar_id { get; set; } = default!;
    public string url { get; set; } = default!;
    public string html_url { get; set; } = default!;
    public string followers_url { get; set; } = default!;
    public string following_url { get; set; } = default!;
    public string gists_url { get; set; } = default!;
    public string starred_url { get; set; } = default!;
    public string subscriptions_url { get; set; } = default!;
    public string organizations_url { get; set; } = default!;
    public string repos_url { get; set; } = default!;
    public string events_url { get; set; } = default!;
    public string received_events_url { get; set; } = default!;
    public string type { get; set; } = default!;
    public string user_view_type { get; set; } = default!;
    public bool site_admin { get; set; }
    public string name { get; set; } = default!;
    public string? company { get; set; } = default!;
    public string? blog { get; set; } = default!;
    public string? location { get; set; }
    public string? email { get; set; } = default!;
    public bool hireable { get; set; }
    public string bio { get; set; } = default!;
    public string? twitter_username { get; set; }
    public string notification_email { get; set; } = default!;
    public int public_repos { get; set; }
    public int public_gists { get; set; }
    public int followers { get; set; }
    public int following { get; set; }
    public DateTime created_at { get; set; }
    public DateTime updated_at { get; set; }
    public int private_gists { get; set; }
    public int total_private_repos { get; set; }
    public int owned_private_repos { get; set; }
    public int disk_usage { get; set; }
    public int collaborators { get; set; }
    public bool two_factor_authentication { get; set; }
    public Plan plan { get; set; } = default!;
}

public class Plan
{
    public string name { get; set; } = default!;
    public int space { get; set; }
    public int collaborators { get; set; }
    public int private_repos { get; set; }
}
