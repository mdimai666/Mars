namespace Mars.Host.Shared.Interfaces;

public interface IMtoStatusProp
{
    //public PostStatusDto PostStatus { get; set; }
}

public interface IMtoLikes
{
    public bool IsLiked { get; set; }
    public int LikesCount { get; set; }
    //public ICollection<PostLike> Likes { get; set; }

}
