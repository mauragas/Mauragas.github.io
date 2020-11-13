using System;
using System.Collections.Generic;
using System.IO;
using Application.Shared.Models;

namespace ArticleData.Generator
{
  public class Generator : IGenerator
  {
    public List<ArticleFileInfo> GetArticles(string path)
    {
      if (!Directory.Exists(path))
        path = Path.GetDirectoryName(path);

      var articleFiles = GetArticles(path, "*.md");
      _ = articleFiles.Remove(articleFiles.Find(a => a.FileName.StartsWith("README")));
      var gitClient = new GitClient(path);
      foreach (var file in articleFiles)
        gitClient.GetFileInfo(file);
      return articleFiles;
    }

    private static List<ArticleFileInfo> GetArticles(string path, string fileExtension)
    {
      var articles = new List<ArticleFileInfo>();
      var directory = new DirectoryInfo(path);
      foreach (var file in directory.GetFiles(fileExtension, SearchOption.AllDirectories))
      {
        var content = file.OpenText().ReadToEnd();
        articles.Add(new ArticleFileInfo
        {
          FileName = file.Name,
          Path = file.FullName.Replace($"{path}/", string.Empty).Replace($"/{file.Name}", string.Empty),
          Description = GetDescription(content),
          Title = GetTitle(content),
          PictureUrl = GetPictureUrl(content),
          Content = string.Empty

        });
      }
      return articles;
    }

    private static string GetTitle(string content)
    {
      using (var reader = new StringReader(content))
      {
        var line = reader.ReadLine()?.Trim();
        while (line != null)
        {
          if (!line.StartsWith('#'))
          {
            line = reader.ReadLine()?.Trim();
            continue;
          }
          return line.Replace("#", string.Empty).Trim();
        }
      }
      return string.Empty;
    }

    private static string GetDescription(string content)
    {
      using (var reader = new StringReader(content))
      {
        var line = reader.ReadLine()?.Trim();
        while (line != null)
        {
          if (string.IsNullOrWhiteSpace(line) ||
            line.StartsWith('#') ||
            line.StartsWith('!') ||
            line.StartsWith('['))
          {
            line = reader.ReadLine()?.Trim();
            continue;
          }
          return line;
        }
      }
      return string.Empty;
    }

    private static string GetPictureUrl(string content)
    {
      using (var reader = new StringReader(content))
      {
        var line = reader.ReadLine()?.Trim();
        while (line != null)
        {
          if (line.StartsWith("![") || line.StartsWith("[!["))
          {
            var url = GetUrl(line);
            if (!string.IsNullOrWhiteSpace(url))
              return url;
          }
          line = reader.ReadLine();
        }
      }
      return string.Empty;
    }

    private static string GetUrl(string line)
    {
      var startIndex = line.IndexOf('(');
      var length = line.IndexOf(')') - startIndex - 1;
      var url = line.Substring(startIndex + 1, length);

      if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
        return url;

      return string.Empty;
    }
  }
}