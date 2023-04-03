using AngleSharp;
using AngleSharp.Dom;

var config = Configuration.Default.WithDefaultLoader();
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
