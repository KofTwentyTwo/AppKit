namespace KofTwentyTwo.AppKit;

/// <summary>Credit and link text for the splash and About screens, shared by every UI package.</summary>
public static class Credits
{
   /// <summary>Separator between items on one credit line (en spaces around a middle dot).</summary>
   public const string Separator = " · ";



   /// <summary>
   /// The contact line: author email, the repository owner's profile (e.g.
   /// "github.com/KofTwentyTwo"), and the website host, skipping whatever is unset.
   /// </summary>
   public static string ContactLine(AppInfo app)
   {
      ArgumentNullException.ThrowIfNull(app);
      var parts = new List<string>(3);
      if(app.AuthorEmail.Length > 0)
      {
         parts.Add(app.AuthorEmail);
      }
      if(OwnerProfile(app.RepositoryUrl) is string owner)
      {
         parts.Add(owner);
      }
      if(app.Website is not null)
      {
         parts.Add(app.Website.Host);
      }
      return string.Join(Separator, parts);
   }



   /// <summary>"host/owner" for a repository URL such as https://github.com/owner/repo; null when absent.</summary>
   public static string? OwnerProfile(Uri? repositoryUrl)
   {
      if(repositoryUrl is null)
      {
         return null;
      }

      string owner = repositoryUrl.AbsolutePath.Trim('/').Split('/')[0];
      return owner.Length == 0 ? repositoryUrl.Host : $"{repositoryUrl.Host}/{owner}";
   }
}
