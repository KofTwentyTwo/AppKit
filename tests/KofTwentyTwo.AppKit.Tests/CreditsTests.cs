/*
 * Copyright (c) 2026 James Maes (KofTwentyTwo)
 * SPDX-License-Identifier: MIT
 */

namespace KofTwentyTwo.AppKit.Tests;

/// <summary>Tests for the splash and About credit text.</summary>
public class CreditsTests
{
   /// <summary>ContactLine: joins what is set.</summary>
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



   /// <summary>ContactLine: empty when nothing is set.</summary>
   [Fact]
   public void ContactLine_EmptyWhenNothingIsSet()
   {
      Assert.Equal("", Credits.ContactLine(new AppInfo { Id = "x", DisplayName = "X" }));
      Assert.Throws<ArgumentNullException>(() => Credits.ContactLine(null!));
   }



   /// <summary>A missing email does not introduce a leading separator before repository/website credits.</summary>
   [Fact]
   public void ContactLine_NoEmail_OmitsEmptyCredit()
   {
      var app = new AppInfo { Id = "sample", DisplayName = "Sample", RepositoryUrl = new Uri("https://github.com/owner/repo") };
      Assert.Equal("github.com/owner", Credits.ContactLine(app));
   }



   /// <summary>The owner profile is host/owner for any repository URL, or just the host when the path is empty.</summary>
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
