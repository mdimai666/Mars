using EditorJsBlazored.Blocks;

namespace Test.EditorJsBlazored.Blocks;

public class BlockEmbedTests
{
    [Theory]
    //youtube
    [InlineData(["youtube", "https://www.youtube.com/watch?v=wZZ7oFKsKzY&t=120", "https://www.youtube.com/embed/wZZ7oFKsKzY?start=120"])]
    [InlineData(["youtube", "https://www.youtube.com/embed/_q51LZ2HpbE?list=PLLy6qvPKpdlV3OAw00EuZMoYPz4pYuwuN", "https://www.youtube.com/embed/_q51LZ2HpbE?list=PLLy6qvPKpdlV3OAw00EuZMoYPz4pYuwuN"])]
    [InlineData(["youtube", "https://www.youtube.com/watch?time_continue=173&v=Nd9LbCWpHp8", "https://www.youtube.com/embed/Nd9LbCWpHp8?start=173"])]
    [InlineData(["youtube", "https://www.youtube.com/watch?v=efBBjIK3b8I&list=LL&t=1337", "https://www.youtube.com/embed/efBBjIK3b8I?start=1337"])]
    [InlineData(["youtube", "https://www.youtube.com/watch?v=yQUeAin7fII&list=RDMMnMXCzscqi_M", "https://www.youtube.com/embed/yQUeAin7fII?"])]
    [InlineData(["youtube", "https://www.youtube.com/watch?v=3kw2sttGXMI&list=FLgc4xqIMDoiP4KOTFS21TJA", "https://www.youtube.com/embed/3kw2sttGXMI?"])]

    //vimeo
    [InlineData(["vimeo", "https://vimeo.com/289836809", "https://player.vimeo.com/video/289836809?title=0&byline=0"])]
    [InlineData(["vimeo", "https://www.vimeo.com/280712228", "https://player.vimeo.com/video/280712228?title=0&byline=0"])]
    [InlineData(["vimeo", "https://player.vimeo.com/video/504749530", "https://player.vimeo.com/video/504749530?title=0&byline=0"])]

    //coub
    [InlineData(["coub", "https://coub.com/view/1efrxs", "https://coub.com/embed/1efrxs"])]
    [InlineData(["coub", "https://coub.com/view/1c6nrr", "https://coub.com/embed/1c6nrr"])]

    //imgur
    [InlineData(["imgur", "https://imgur.com/gallery/OHbkxgr", "http://imgur.com/OHbkxgr/embed"])]
    [InlineData(["imgur", "https://imgur.com/gallery/TqIWG12", "http://imgur.com/TqIWG12/embed"])]

    //gfycat
    [InlineData(["gfycat", "https://gfycat.com/EsteemedMarvelousHagfish", "https://gfycat.com/ifr/EsteemedMarvelousHagfish"])]
    [InlineData(["gfycat", "https://gfycat.com/OddCornyLeech", "https://gfycat.com/ifr/OddCornyLeech"])]

    //twitch-channel
    [InlineData(["twitch-channel", "https://www.twitch.tv/ninja", "https://player.twitch.tv/?channel=ninja"])]
    [InlineData(["twitch-channel", "https://www.twitch.tv/gohamedia", "https://player.twitch.tv/?channel=gohamedia"])]

    //twitch-video
    [InlineData(["twitch-video", "https://www.twitch.tv/videos/315468440", "https://player.twitch.tv/?video=v315468440"])]
    [InlineData(["twitch-video", "https://www.twitch.tv/videos/314691366", "https://player.twitch.tv/?video=v314691366"])]

    //yandex-music-album
    [InlineData(["yandex-music-album", "https://music.yandex.ru/album/5643859", "https://music.yandex.ru/iframe/#album/5643859/"])]
    [InlineData(["yandex-music-album", "https://music.yandex.ru/album/5393158", "https://music.yandex.ru/iframe/#album/5393158/"])]

    //yandex-music-track
    [InlineData(["yandex-music-track", "https://music.yandex.ru/album/5643859/track/42662275", "https://music.yandex.ru/iframe/#track/5643859/42662275/"])]
    [InlineData(["yandex-music-track", "https://music.yandex.ru/album/5393158/track/41249158", "https://music.yandex.ru/iframe/#track/5393158/41249158/"])]

    //yandex-music-playlist
    [InlineData(["yandex-music-playlist", "https://music.yandex.ru/users/yamusic-personal/playlists/25098905", "https://music.yandex.ru/iframe/#playlist/yamusic-personal/25098905/show/cover/description/"])]
    [InlineData(["yandex-music-playlist", "https://music.yandex.ru/users/yamusic-personal/playlists/27924603", "https://music.yandex.ru/iframe/#playlist/yamusic-personal/27924603/show/cover/description/"])]

    //codepen
    [InlineData(["codepen", "https://codepen.io/Rikkokiri/pen/RYBrwG", "https://codepen.io/Rikkokiri/embed/RYBrwG?height=300&theme-id=0&default-tab=css,result&embed-version=2"])]
    [InlineData(["codepen", "https://codepen.io/geoffgraham/pen/bxEVEN", "https://codepen.io/geoffgraham/embed/bxEVEN?height=300&theme-id=0&default-tab=css,result&embed-version=2"])]

    //twitter
    [InlineData(["twitter", "https://twitter.com/codex_team/status/1202295536826630145", "https://platform.twitter.com/embed/Tweet.html?id=1202295536826630145"])]
    [InlineData(["twitter", "https://twitter.com/codex_team/status/1202295536826630145?s=20&t=wrY8ei5GBjbbmNonrEm2kQ", "https://platform.twitter.com/embed/Tweet.html?id=1202295536826630145"])]
    [InlineData(["twitter", "https://x.com/codex_team/status/1202295536826630145", "https://platform.twitter.com/embed/Tweet.html?id=1202295536826630145"])]

    //instagram
    [InlineData(["instagram", "https://www.instagram.com/p/B--iRCFHVxI/", "https://www.instagram.com/p/B--iRCFHVxI/embed"])]
    [InlineData(["instagram", "https://www.instagram.com/p/CfQzzGNphD8/?utm_source=ig_web_copy_link", "https://www.instagram.com/p/CfQzzGNphD8/embed"])]
    [InlineData(["instagram", "https://www.instagram.com/p/C4_Lsf1NBra/?img_index=1", "https://www.instagram.com/p/C4_Lsf1NBra/embed"])]
    [InlineData(["instagram", "https://www.instagram.com/p/C5ZZUWPydSY/?utm_source=ig_web_copy_link", "https://www.instagram.com/p/C5ZZUWPydSY/embed"])]
    [InlineData(["instagram", "https://www.instagram.com/reel/C19IuqJx6wm/", "https://www.instagram.com/p/C19IuqJx6wm/embed"])]
    [InlineData(["instagram", "https://www.instagram.com/reel/C19IuqJx6wm/?utm_source=ig_web_copy_link", "https://www.instagram.com/p/C19IuqJx6wm/embed"])]

    //aparat
    [InlineData(["aparat", "https://www.aparat.com/v/tDZe5", "https://www.aparat.com/video/video/embed/videohash/tDZe5/vt/frame"])]
    //pinterest
    [InlineData(["pinterest", "https://tr.pinterest.com/pin/409757266103637553/", "https://assets.pinterest.com/ext/embed.html?id=409757266103637553"])]

    //facebook
    [InlineData(["facebook", "https://www.facebook.com/genclikforeverresmi/videos/944647522284479", "https://www.facebook.com/plugins/post.php?href=https://www.facebook.com/genclikforeverresmi/videos/944647522284479&width=500"])]
    [InlineData(["facebook", "https://www.facebook.com/0devco/posts/497515624410920", "https://www.facebook.com/plugins/post.php?href=https://www.facebook.com/0devco/posts/497515624410920&width=500"])]

    //github
    [InlineData(["github", "https://gist.github.com/userharis/091b56505c804276e1f91925976f11db", @"data:text/html;charset=utf-8,<head><base target=""_blank"" /></head><body><script src=""https://gist.github.com/userharis/091b56505c804276e1f91925976f11db.js"" ></script></body>"])]
    [InlineData(["github", "https://gist.github.com/userharis/a8c2977094d4716c43e35e6c20b7d306", @"data:text/html;charset=utf-8,<head><base target=""_blank"" /></head><body><script src=""https://gist.github.com/userharis/a8c2977094d4716c43e35e6c20b7d306.js"" ></script></body>"])]

    //miro
    [InlineData(["miro", "https://miro.com/app/board/10J_kw57KxQ=/", "https://miro.com/app/live-embed/10J_kw57KxQ="])]

    public void GetHtml_Parse_Success(string service, string source, string expect)
    {
        //Arrange
        var block = new BlockEmbed { Service = service, Embed = source };

        //Act
        var (html, embedUrl, id) = BlockEmbed.Parse(block);

        //Assert
        Assert.Contains(expect, embedUrl);
    }
}
