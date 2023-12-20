using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RymRss.Migrations
{
    /// <inheritdoc />
    public partial class AddArtistManyToMany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Albums_new",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Href = table.Column<string>(type: "TEXT", nullable: false),
                    ReleaseDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Albums", x => x.Id);
                });

            // TODO Add option in installer?
            // migrationBuilder.Sql(
            //     @"
            //         INSERT INTO Albums_new (Id, Title, ReleaseDate, DateCreated, DateUpdated, Href)
            //         SELECT AlbumId, Title, ReleaseDate, DateCreated, DateUpdated, AlbumHref
            //         FROM Albums
            //         GROUP BY Albums.AlbumId;
            //     ");

            migrationBuilder.CreateTable(
                name: "Artists",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Href = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Artists", x => x.Id);
                });

            // migrationBuilder.Sql(
            //     @"
            //         INSERT INTO Artists (Id, Name, Href)
            //         SELECT ArtistId, Artist, ArtistHref
            //         FROM Albums
            //         GROUP BY ArtistId;
            //     ");

            migrationBuilder.CreateTable(
                name: "AlbumArtist",
                columns: table => new
                {
                    AlbumsId = table.Column<string>(type: "TEXT", nullable: false),
                    ArtistsId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlbumArtist", x => new { x.AlbumsId, x.ArtistsId });
                    table.ForeignKey(
                        name: "FK_AlbumArtist_Albums_AlbumsId",
                        column: x => x.AlbumsId,
                        principalTable: "Albums_new",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AlbumArtist_Artists_ArtistsId",
                        column: x => x.ArtistsId,
                        principalTable: "Artists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlbumArtist_ArtistsId",
                table: "AlbumArtist",
                column: "ArtistsId");

            // migrationBuilder.Sql(
            //     @"
            //         INSERT INTO AlbumArtist (AlbumsId, ArtistsId)
            //         SELECT AlbumId, ArtistId
            //         FROM Albums
            //         GROUP BY Albums.AlbumId;
            //     ");

            migrationBuilder.DropTable(
                name: "Albums");

            migrationBuilder.RenameTable(
                name: "Albums_new",
                newName: "Albums");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Albums_new",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AlbumId = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    AlbumHref = table.Column<string>(type: "TEXT", nullable: false),
                    Artist = table.Column<string>(type: "TEXT", nullable: false),
                    ArtistHref = table.Column<string>(type: "TEXT", nullable: false),
                    ArtistId = table.Column<string>(type: "TEXT", nullable: false),
                    ReleaseDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Albums", x => x.Id);
                });

            // migrationBuilder.Sql(
            //     @"
            //         INSERT INTO Albums_new (AlbumId, Title, AlbumHref, DateCreated, DateUpdated, ReleaseDate, ArtistId, Artist, ArtistHref)
            //         SELECT Albums.Id, Albums.Title, Albums.Href, Albums.DateCreated, Albums.DateUpdated, Albums.ReleaseDate, Artists.Id, Artists.Name, Artists.Href
            //         FROM Albums
            //             INNER JOIN AlbumArtist ON Albums.Id = AlbumArtist.AlbumsId
            //             INNER JOIN Artists ON AlbumArtist.ArtistsId = Artists.Id
            //         GROUP BY Albums.Id;
            //     ");


            migrationBuilder.DropTable(
                name: "AlbumArtist");

            migrationBuilder.DropTable(
                name: "Artists");

            migrationBuilder.DropTable(
                name: "Albums");

            migrationBuilder.RenameTable(
                name: "Albums_new",
                newName: "Albums");
        }
    }
}
