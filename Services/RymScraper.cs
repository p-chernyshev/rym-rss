using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Io.Cookie;
namespace RymRss.Services;

public class RymScraper
{
    private async Task FetchAlbums()
    {
        var cookies = new[]
        {
            "is_logged_in=1; Expires=Tue, 01-Jan-2030 01:00:00 GMT; Path=/; HttpOnly;",
            "username=jiux; Expires=Tue, 01-Jan-2030 01:00:00 GMT; Path=/; secure; HttpOnly;",
        };

        var cookiesFileHandler = new LocalFileHandler("cookies.txt");
        var cookieProvider = new AdvancedCookieProvider(cookiesFileHandler);
        foreach (var cookie in cookies)
        {
            cookieProvider.AddCookie(WebCookie.FromString(cookie + "; Domain=rateyourmusic.com"));
        }

        var config = Configuration.Default
            .WithCookies(cookieProvider)
            .WithDefaultLoader();
        var address = "https://rateyourmusic.com/~jiux";
        var context = BrowsingContext.New(config);
        var document = await context.OpenAsync(address);
        var artists = document.QuerySelectorAll(".artist");
        var albums = document.QuerySelectorAll(".album");
        var parentContainer = artists.First().ParentElement;
        var dates = parentContainer.QuerySelectorAll("div > b");

        foreach (var album in albums)
        {
            var artist = artists.Last(artist => artist.CompareDocumentPosition(album).HasFlag(DocumentPositions.Following));
            var date = dates.Last(date => date.CompareDocumentPosition(album).HasFlag(DocumentPositions.Following));
            Console.WriteLine($"{date.TextContent} {artist.TextContent} {album.TextContent}");
        }
    }
    
}
