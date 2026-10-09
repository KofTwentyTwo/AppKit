namespace KofTwentyTwo.AppKit.Tests;

public class CreditsTests
{
   [Fact]
   public void ContactLine_JoinsWhatIsSet()
   {
      var app = new AppInfo
      {
         Id = "gclo",
         DisplayName = "gclo",
         AuthorEmail = "james@kof22.com",
         RepositoryUrl = new Uri("https://github.com/KofTwentyTwo/gclo"),
         Website = new Uri("https://kof22.com/"),
      };
      Assert.Equal("james@kof22.com · github.com/KofTwentyTwo · kof22.com", Credits.ContactLine(app));
   }



   [Fact]
   public void ContactLine_EmptyWhenNothingIsSet()
   {
      Assert.Equal("", Credits.ContactLine(new AppInfo { Id = "x", DisplayName = "X" }));
      Assert.Throws<ArgumentNullException>(() => Credits.ContactLine(null!));
   }



   [Theory]
   [InlineData(null, null)]
   [InlineData("https://github.com/", "github.com")]
   [InlineData("https://github.com/owner", "github.com/owner")]
   [InlineData("https://github.com/owner/repo/", "github.com/owner")]
   public void OwnerProfile(string? url, string? expected)
   {
      Assert.Equal(expected, Credits.OwnerProfile(url is null ? null : new Uri(url)));
   }
}
